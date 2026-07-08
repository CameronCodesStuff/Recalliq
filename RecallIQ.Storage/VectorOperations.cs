using System.Runtime.InteropServices;
using System.Numerics;

namespace RecallIQ.Storage;

public static class VectorOperations
{
    public static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must be the same length");

        var spanA = new ReadOnlySpan<float>(vectorA);
        var spanB = new ReadOnlySpan<float>(vectorB);

        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        int i = 0;
        int vectorSize = System.Numerics.Vector<float>.Count;
        int lastBlockIndex = vectorA.Length - (vectorA.Length % vectorSize);

        var dotVec = System.Numerics.Vector<float>.Zero;
        var normAVec = System.Numerics.Vector<float>.Zero;
        var normBVec = System.Numerics.Vector<float>.Zero;

        for (; i < lastBlockIndex; i += vectorSize)
        {
            var va = new System.Numerics.Vector<float>(spanA.Slice(i, vectorSize));
            var vb = new System.Numerics.Vector<float>(spanB.Slice(i, vectorSize));
            dotVec += va * vb;
            normAVec += va * va;
            normBVec += vb * vb;
        }

        dotProduct = System.Numerics.Vector.Dot(dotVec, System.Numerics.Vector<float>.One);
        normA = System.Numerics.Vector.Dot(normAVec, System.Numerics.Vector<float>.One);
        normB = System.Numerics.Vector.Dot(normBVec, System.Numerics.Vector<float>.One);

        for (; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        float denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denominator == 0f ? 0f : dotProduct / denominator;
    }

    public static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] DeserializeEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
