using System;

namespace Play.Infrastructure.Common.Utilities;

public class EnvReader
{
    private readonly Dictionary<string, string> _envVariables;

    public EnvReader(string envFilePath = ".env")
    {
        _envVariables = new Dictionary<string, string>();
        // Resolve path relative to project root
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var solutionPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\.."));
        var fullPath = Path.Combine(solutionPath, envFilePath);
        Load(fullPath);
    }

    private void Load(string envFilePath)
    {
        if (!File.Exists(envFilePath))
            throw new FileNotFoundException($".env file not found at {envFilePath}");

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                _envVariables[key] = value;
            }
        }
    }

    public string GetString(string key)
    {
        if (_envVariables.TryGetValue(key, out var value))
            return value;
        throw new KeyNotFoundException($"Environment variable {key} not found in .env file");
    }

    public int GetInt(string key)
    {
        if (_envVariables.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;
        throw new KeyNotFoundException($"Environment variable {key} not found or is not an integer");
    }
}