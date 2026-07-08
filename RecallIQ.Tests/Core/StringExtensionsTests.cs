using FluentAssertions;
using RecallIQ.Core.Extensions;
using Xunit;

namespace RecallIQ.Tests.Core;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Hello World", 5, "Hello")]
    [InlineData("Hi", 10, "Hi")]
    [InlineData("", 5, "")]
    public void Truncate_ReturnsExpected(string input, int maxLen, string expected)
    {
        input.Truncate(maxLen).Should().Be(expected);
    }

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1023L, "1023 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void FormatFileSize_ReturnsExpected(long bytes, string expected)
    {
        StringExtensions.FormatFileSize(bytes).Should().Be(expected);
    }

    [Fact]
    public void ComputeSha256_ReturnsDeterministicHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content for hashing");
            var hash1 = tempFile.ComputeSha256();
            var hash2 = tempFile.ComputeSha256();

            hash1.Should().NotBeNullOrEmpty();
            hash1.Should().Be(hash2);
            hash1.Should().HaveLength(64);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
