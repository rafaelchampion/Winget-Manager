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

    public static string SanitizePackageId(string? packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId)) return string.Empty;
        // Keep valid package ID characters: letters, numbers, dot, dash, underscore, plus
        return Regex.Replace(packageId.Trim(), @"[^a-zA-Z0-9.\-_+]", "");
    }

    public static string SanitizeSearchQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;
        // Strip double quotes, backticks, newlines, pipe, and semicolons to prevent argument breakout
        return Regex.Replace(query.Trim(), @"[""`\r\n|;&$]", "");
    }

    public static (bool IsSuccess, string? ErrorMessage) InterpretExitCode(int exitCode, string stdout, string stderr)
    {
        // Check for recognized success codes
        if (exitCode == 0)
        {
            return (true, null);
        }

        if (exitCode == 3010 || exitCode == 1641)
        {
            return (true, "Installation completed. System reboot is required to finish setup.");
        }

        // Check for textual success confirmations even if winget had warnings
        if (stdout.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase) ||
            stdout.Contains("Instalação bem-sucedida", StringComparison.OrdinalIgnoreCase) ||
            stdout.Contains("Successfully updated", StringComparison.OrdinalIgnoreCase) ||
            stdout.Contains("Atualização bem-sucedida", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null);
        }

        // Known Win32 / MSI installer exit codes
        string? message = exitCode switch
        {
            1602 => "Installation was cancelled by the user.",
            1603 => "Fatal error during installation (1603). Administrator elevation or missing dependencies may be required.",
            1618 => "Another installation is already in progress. Please wait for it to complete.",
            1619 => "Installer package could not be opened.",
            1620 => "Invalid installation package.",
            1638 => "Another version of this product is already installed.",
            // Winget specific HRESULT error codes
            unchecked((int)0x8A150011) => "No applicable updates found for this package.",
            unchecked((int)0x8A150014) => "Package agreements were not accepted.",
            unchecked((int)0x8A150022) => "Package installation requires administrator elevation.",
            unchecked((int)0x8A15002B) => "Installation was cancelled by the user.",
            unchecked((int)0x8A15002C) => "Installer hash mismatch. Download may be corrupt or repository manifest outdated.",
            unchecked((int)0x8A150005) => "Failed to update package source repositories.",
            unchecked((int)0x8A150006) => "Package was not found in the configured sources.",
            _ => null
        };

        if (message != null)
        {
            return (false, message);
        }

        // Fallback: extract meaningful error snippet from stderr or stdout
        string details = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim();
        if (string.IsNullOrWhiteSpace(details))
        {
            return (false, $"Process exited with error code {exitCode} (0x{exitCode:X8}).");
        }

        // Get the last non-empty line of details for a concise message
        var lastLine = details.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return (false, $"Process exited with error code {exitCode}: {lastLine}");
    }
}

