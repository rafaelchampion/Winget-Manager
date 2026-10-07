using System.Text.RegularExpressions;
using WingetManager.Domain.Entities;

namespace WingetManager.Infrastructure.Winget;

public static partial class WingetCliParser
{
    private static readonly Regex ProgressPercentRegex = new(@"(?:^|\s|\()(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

    public static double? ExtractProgressPercentage(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        var match = ProgressPercentRegex.Match(line);
        if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var percent))
        {
            return percent;
        }

        return null;
    }

    /// &lt;summary&gt;
    /// Finds the first index of any of the given candidates in the header line (case-insensitive).
    /// Returns -1 if none found.
    /// &lt;/summary&gt;
    private static int FindColumnIndex(string headerLine, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            int idx = headerLine.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) return idx;
        }
        return -1;
    }

    // Winget footer lines that signal the end of the data table (all locales we support)
    private static readonly string[] TableFooterPatterns =
    [
        "upgrades available",   // en
        "upgrade available",    // en (singular)
        "atualizações disponíveis", // pt-BR
        "atualização disponível",   // pt-BR (singular)
        "actualizaciones disponibles", // es
    ];

    private static bool IsFooterLine(string line)
    {
        foreach (var pattern in TableFooterPatterns)
        {
            if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static IReadOnlyList<Package> ParseUpgradesTable(string cliOutput)
    {
        var result = new List<Package>();
        if (string.IsNullOrWhiteSpace(cliOutput)) return result;

        var lines = cliOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int separatorIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("---") || lines[i].Contains("------"))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex <= 0 || separatorIndex >= lines.Length)
        {
            return result;
        }

        var headerLine = lines[separatorIndex - 1];
        var sepLine = lines[separatorIndex];

        // Find column start positions from header — supports English + Portuguese + Spanish
        int idIndex = FindColumnIndex(headerLine, "Id", "ID");
        int versionIndex = FindColumnIndex(headerLine, "Version", "Versão", "Versión", "Versao");
        int availableIndex = FindColumnIndex(headerLine, "Available", "Disponível", "Disponible", "Disponivel");
        int sourceIndex = FindColumnIndex(headerLine, "Source", "Origem", "Origen", "Fonte");

        if (idIndex < 0 || versionIndex < 0) return result;

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (IsFooterLine(line)) break;
            if (line.StartsWith('<') || line.StartsWith('-')) continue;

            if (line.Length <= idIndex) continue;

            string name = line[..Math.Min(idIndex, line.Length)].Trim();

            string id;
            if (versionIndex > 0 && line.Length > versionIndex)
            {
                id = line[idIndex..versionIndex].Trim();
            }
            else
            {
                id = line[idIndex..].Trim();
            }

            string version = "";
            if (versionIndex > 0 && line.Length > versionIndex)
            {
                int endVersion = availableIndex > 0 && line.Length > availableIndex ? availableIndex : line.Length;
                version = line[versionIndex..endVersion].Trim();
            }

            string? available = null;
            if (availableIndex > 0 && line.Length > availableIndex)
            {
                int endAvailable = sourceIndex > 0 && line.Length > sourceIndex ? sourceIndex : line.Length;
                available = line[availableIndex..endAvailable].Trim();
            }

            string? source = null;
            if (sourceIndex > 0 && line.Length > sourceIndex)
            {
                source = line[sourceIndex..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                result.Add(new Package
                {
                    Id = id,
                    Name = string.IsNullOrWhiteSpace(name) ? id : name,
                    InstalledVersion = string.IsNullOrWhiteSpace(version) ? "Unknown" : version,
                    AvailableVersion = available,
                    Source = source
                });
            }
        }

        return result;
    }

    public static IReadOnlyList<Package> ParseSearchTable(string cliOutput)
    {
        var result = new List<Package>();
        if (string.IsNullOrWhiteSpace(cliOutput)) return result;

        var lines = cliOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int separatorIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("---") || lines[i].Contains("------"))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex <= 0 || separatorIndex >= lines.Length)
        {
            return result;
        }

        var headerLine = lines[separatorIndex - 1];

        int idIndex = FindColumnIndex(headerLine, "Id", "ID");
        int versionIndex = FindColumnIndex(headerLine, "Version", "Versão", "Versión", "Versao");
        int matchIndex = FindColumnIndex(headerLine, "Match", "Correspondência", "Coincidencia");
        int sourceIndex = FindColumnIndex(headerLine, "Source", "Origem", "Origen", "Fonte");

        if (idIndex < 0) return result;

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith('<') || line.StartsWith('-')) continue;

            if (line.Length <= idIndex) continue;

            string name = line[..Math.Min(idIndex, line.Length)].Trim();

            string id;
            if (versionIndex > 0 && line.Length > versionIndex)
            {
                id = line[idIndex..versionIndex].Trim();
            }
            else
            {
                id = line[idIndex..].Trim();
            }

            string? version = null;
            if (versionIndex > 0 && line.Length > versionIndex)
            {
                int endVersion = matchIndex > 0 && line.Length > matchIndex 
                    ? matchIndex 
                    : (sourceIndex > 0 && line.Length > sourceIndex ? sourceIndex : line.Length);
                version = line[versionIndex..endVersion].Trim();
            }

            string? source = null;
            if (sourceIndex > 0 && line.Length > sourceIndex)
            {
                source = line[sourceIndex..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                result.Add(new Package
                {
                    Id = id,
                    Name = string.IsNullOrWhiteSpace(name) ? id : name,
                    InstalledVersion = "Not Installed",
                    AvailableVersion = version,
                    Source = source
                });
            }
        }

        return result;
    }
}
