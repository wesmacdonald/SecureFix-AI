namespace SecureFix.Api;

internal static class DotEnvConfiguration
{
    public static void Load()
    {
        var path = FindDotEnvFile();
        if (path is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = line[..separatorIndex].Trim();
            var value = TrimQuotes(line[(separatorIndex + 1)..].Trim());
            if (IsValidVariableName(name) && Environment.GetEnvironmentVariable(name) is null)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }

    private static string? FindDotEnvFile()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string TrimQuotes(string value)
    {
        return value.Length >= 2 && value[0] == value[^1] && (value[0] == '\'' || value[0] == '"')
            ? value[1..^1]
            : value;
    }

    private static bool IsValidVariableName(string name)
    {
        return name.Length > 0
            && (char.IsLetter(name[0]) || name[0] == '_')
            && name.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}