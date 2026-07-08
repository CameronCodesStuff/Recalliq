using FluentAssertions;
using RecallIQ.Storage;
using Xunit;

namespace RecallIQ.Tests.Storage;

public class VectorOperationsTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        float[] vec = [1f, 2f, 3f, 4f, 5f];
        var similarity = VectorOperations.CosineSimilarity(vec, vec);
        similarity.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        float[] a = [1f, 0f, 0f];
        float[] b = [0f, 1f, 0f];
        var similarity = VectorOperations.CosineSimilarity(a, b);
        similarity.Should().BeApproximately(0.0f, 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [-1f, -2f, -3f];
        var similarity = VectorOperations.CosineSimilarity(a, b);
        similarity.Should().BeApproximately(-1.0f, 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_DifferentLengths_Throws()
    {
        float[] a = [1f, 2f];
        float[] b = [1f, 2f, 3f];
        var act = () => VectorOperations.CosineSimilarity(a, b);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_ReturnsZero()
    {
        float[] a = [0f, 0f, 0f];
        float[] b = [1f, 2f, 3f];
        var similarity = VectorOperations.CosineSimilarity(a, b);
        similarity.Should().Be(0f);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrips()
    {
        float[] original = [0.1f, 0.2f, 0.3f, -0.5f, 1.0f];
        var bytes = VectorOperations.SerializeEmbedding(original);
        var restored = VectorOperations.DeserializeEmbedding(bytes);

        restored.Should().HaveCount(original.Length);
        for (int i = 0; i < original.Length; i++)
        {
            restored[i].Should().BeApproximately(original[i], 0.00001f);
        }
    }

    [Fact]
    public void SerializeEmbedding_ProducesCorrectByteCount()
    {
        float[] vec = new float[384];
        var bytes = VectorOperations.SerializeEmbedding(vec);
        bytes.Should().HaveCount(384 * sizeof(float));
    }

    [Fact]
    public void CosineSimilarity_LargeVectors_Succeeds()
    {
        var rng = new Random(42);
        var a = Enumerable.Range(0, 384).Select(_ => (float)rng.NextDouble()).ToArray();
        var b = Enumerable.Range(0, 384).Select(_ => (float)rng.NextDouble()).ToArray();
        var result = VectorOperations.CosineSimilarity(a, b);
        result.Should().BeInRange(-1.0f, 1.0f);
    }
}
