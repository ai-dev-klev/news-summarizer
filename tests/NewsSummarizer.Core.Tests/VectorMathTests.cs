
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.Tests;

public sealed class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_ShouldReturnOne_ForSameDirection()
    {
        var result = VectorMath.CosineSimilarity([1, 2, 3], [1, 2, 3]);

        Assert.True(result > 0.999);
    }

    [Fact]
    public void CosineSimilarity_ShouldReturnZero_ForOrthogonalVectors()
    {
        var result = VectorMath.CosineSimilarity([1, 0], [0, 1]);

        Assert.Equal(0, result, precision: 6);
    }

    [Fact]
    public void CosineSimilarity_ShouldThrow_ForDifferentDimensions()
    {
        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity([1], [1, 2]));
    }
}
