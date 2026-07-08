using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.AI;

public sealed class OnnxEmbeddingService : IEmbeddingService
{
    private readonly ILogger<OnnxEmbeddingService> _logger;
    private InferenceSession? _session;
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public int EmbeddingDimension { get; private set; } = 384;
    public bool IsInitialized => _session != null;

    public OnnxEmbeddingService(ILogger<OnnxEmbeddingService> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (_session != null) return Task.CompletedTask;

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                InterOpNumThreads = Environment.ProcessorCount,
                IntraOpNumThreads = Environment.ProcessorCount
            };
            options.AppendExecutionProvider_CPU();

            if (!File.Exists(modelPath))
            {
                _logger.LogWarning("ONNX model not found at {Path}, using fallback hashing embeddings", modelPath);
                return;
            }

            _session = new InferenceSession(modelPath, options);

            var outputMeta = _session.OutputMetadata;
            foreach (var output in outputMeta)
            {
                var dims = output.Value.Dimensions;
                if (dims.Length >= 2)
                {
                    EmbeddingDimension = dims[^1];
                    break;
                }
            }

            _logger.LogInformation("ONNX model loaded from {Path}, dimension={Dim}", modelPath, EmbeddingDimension);
        }, cancellationToken);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, cancellationToken);
        return results[0];
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (_session == null)
        {
            return texts.Select(t => GenerateHashEmbedding(t, EmbeddingDimension)).ToArray();
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                var results = new float[texts.Count][];
                for (int i = 0; i < texts.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    results[i] = RunInference(texts[i]);
                }
                return results;
            }, cancellationToken);
        }
        finally { _semaphore.Release(); }
    }

    private float[] RunInference(string text)
    {
        var tokens = SimpleTokenize(text);
        int seqLen = Math.Min(tokens.Length, 512);

        var inputIds = new long[seqLen];
        var attentionMask = new long[seqLen];
        var tokenTypeIds = new long[seqLen];

        for (int i = 0; i < seqLen; i++)
        {
            inputIds[i] = tokens[i];
            attentionMask[i] = 1;
            tokenTypeIds[i] = 0;
        }

        var inputIdsTensor = new DenseTensor<long>(inputIds, [1, seqLen]);
        var attentionTensor = new DenseTensor<long>(attentionMask, [1, seqLen]);
        var tokenTypeTensor = new DenseTensor<long>(tokenTypeIds, [1, seqLen]);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeTensor)
        };

        using var results = _session!.Run(inputs);
        var output = results.First().AsTensor<float>();

        var embedding = new float[EmbeddingDimension];
        int tokenCount = seqLen;
        for (int t = 0; t < tokenCount; t++)
        {
            for (int d = 0; d < EmbeddingDimension; d++)
            {
                embedding[d] += output[0, t, d];
            }
        }
        for (int d = 0; d < EmbeddingDimension; d++)
        {
            embedding[d] /= tokenCount;
        }

        NormalizeVector(embedding);
        return embedding;
    }

    private static long[] SimpleTokenize(string text)
    {
        var cleaned = text.ToLowerInvariant().Trim();
        var words = cleaned.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        var tokens = new List<long> { 101 };
        foreach (var word in words)
        {
            long hash = 0;
            foreach (char c in word)
            {
                hash = hash * 31 + c;
            }
            tokens.Add(Math.Abs(hash % 30000) + 1000);
        }
        tokens.Add(102);
        return tokens.ToArray();
    }

    private static float[] GenerateHashEmbedding(string text, int dimension)
    {
        var embedding = new float[dimension];
        var normalized = text.ToLowerInvariant().Trim();
        var words = normalized.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            int hash = word.GetHashCode();
            var rng = new Random(hash);
            for (int i = 0; i < dimension; i++)
            {
                embedding[i] += (float)(rng.NextDouble() * 2.0 - 1.0);
            }
        }

        NormalizeVector(embedding);
        return embedding;
    }

    private static void NormalizeVector(float[] vector)
    {
        float magnitude = 0f;
        for (int i = 0; i < vector.Length; i++)
            magnitude += vector[i] * vector[i];
        magnitude = MathF.Sqrt(magnitude);

        if (magnitude > 0f)
        {
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= magnitude;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session?.Dispose();
        _semaphore.Dispose();
    }
}
