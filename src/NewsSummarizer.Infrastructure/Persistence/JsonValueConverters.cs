using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NewsSummarizer.Infrastructure.Persistence;

internal static class JsonValueConverters
{
    public static readonly ValueConverter<List<string>, string> StringListConverter = new(
        value => SerializeStringList(value),
        value => DeserializeStringList(value));

    public static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => AreEqual(left, right),
        value => GetHashCode(value),
        value => Snapshot(value));

    private static string SerializeStringList(List<string>? value)
    {
        return JsonSerializer.Serialize(value ?? []);
    }

    private static List<string> DeserializeStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(value) ?? [];
    }

    private static bool AreEqual(List<string>? left, List<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.SequenceEqual(right, StringComparer.Ordinal);
    }

    private static int GetHashCode(List<string>? value)
    {
        if (value is null)
        {
            return 0;
        }

        var hash = new HashCode();

        foreach (var item in value)
        {
            hash.Add(item, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static List<string> Snapshot(List<string>? value)
    {
        return value is null ? [] : value.ToList();
    }
}