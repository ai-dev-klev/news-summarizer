namespace NewsSummarizer.Api;

public static class LocalEnvLoader
{
    public static void Load()
    {
        var envPath = FindEnvFile();

        if (envPath is null)
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');

            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            // Real process env has priority over local .env.
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindEnvFile()
    {
        var candidates = new List<string?>
        {
            Path.Combine(Environment.CurrentDirectory, ".env"),
            Path.Combine(Environment.CurrentDirectory, "src", "NewsSummarizer.Api", ".env")
        };

        AddParentCandidates(candidates, Environment.CurrentDirectory);

        var baseDirectory = AppContext.BaseDirectory;
        AddParentCandidates(candidates, baseDirectory);

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
    }

    private static void AddParentCandidates(List<string?> candidates, string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            candidates.Add(Path.Combine(directory.FullName, ".env"));
            directory = directory.Parent;
        }
    }
}