
namespace NewsSummarizer.Core.Services;

public static class VectorMath
{
    public static double CosineSimilarity(
        IReadOnlyList<float> first,
        IReadOnlyList<float> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Count != second.Count)
        {
            throw new ArgumentException("Vectors must have the same dimensions.");
        }

        if (first.Count == 0)
        {
            return 0;
        }

        double dot = 0;
        double firstNorm = 0;
        double secondNorm = 0;

        for (var index = 0; index < first.Count; index++)
        {
            var a = first[index];
            var b = second[index];

            dot += a * b;
            firstNorm += a * a;
            secondNorm += b * b;
        }

        if (firstNorm <= 0 || secondNorm <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(firstNorm) * Math.Sqrt(secondNorm));
    }
}
