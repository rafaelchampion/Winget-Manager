using FluentAssertions;
using WingetManager.Infrastructure.Winget;
using Xunit;

namespace WingetManager.Infrastructure.Tests;

public class WingetCliParserTests
{
    [Fact]
    public void ParseUpgradesTable_ShouldParseStandardWingetUpgradeOutput()
    {
        // Arrange
        var output = """
            Name                          Id                            Version       Available     Source
            ----------------------------------------------------------------------------------------------
            Git                           Git.Git                       2.44.0        2.45.0        winget
            Microsoft Visual Studio Code  Microsoft.VisualStudioCode    1.88.0        1.89.0        winget
            2 upgrades available.
            """;

        // Act
        var packages = WingetCliParser.ParseUpgradesTable(output);

        // Assert
        packages.Should().HaveCount(2);
        packages[0].Id.Should().Be("Git.Git");
        packages[0].Name.Should().Be("Git");
        packages[0].InstalledVersion.Should().Be("2.44.0");
        packages[0].AvailableVersion.Should().Be("2.45.0");
        packages[0].Source.Should().Be("winget");
        packages[0].HasUpdate.Should().BeTrue();

        packages[1].Id.Should().Be("Microsoft.VisualStudioCode");
        packages[1].Name.Should().Be("Microsoft Visual Studio Code");
        packages[1].InstalledVersion.Should().Be("1.88.0");
        packages[1].AvailableVersion.Should().Be("1.89.0");
        packages[1].Source.Should().Be("winget");
    }

    [Fact]
    public void ParseUpgradesTable_ShouldParsePortugueseWingetUpgradeOutput()
    {
        // Arrange
        var output = """
            Nome                          ID                            Versão        Disponível    Origem
            ----------------------------------------------------------------------------------------------
            Git                           Git.Git                       2.44.0        2.45.0        winget
            Microsoft Visual Studio Code  Microsoft.VisualStudioCode    1.88.0        1.89.0        winget
            2 atualizações disponíveis.
            """;

        // Act
        var packages = WingetCliParser.ParseUpgradesTable(output);

        // Assert
        packages.Should().HaveCount(2);
        packages[0].Id.Should().Be("Git.Git");
        packages[0].Name.Should().Be("Git");
        packages[0].InstalledVersion.Should().Be("2.44.0");
        packages[0].AvailableVersion.Should().Be("2.45.0");
        packages[0].Source.Should().Be("winget");
        packages[0].HasUpdate.Should().BeTrue();

        packages[1].Id.Should().Be("Microsoft.VisualStudioCode");
        packages[1].Name.Should().Be("Microsoft Visual Studio Code");
        packages[1].InstalledVersion.Should().Be("1.88.0");
        packages[1].AvailableVersion.Should().Be("1.89.0");
        packages[1].Source.Should().Be("winget");
    }

    [Fact]
    public void ParseUpgradesTable_WhenNoUpgrades_ShouldReturnEmptyList()
    {
        // Arrange
        var output = """
            No installed package found matching input criteria.
            """;

        // Act
        var packages = WingetCliParser.ParseUpgradesTable(output);

        // Assert
        packages.Should().BeEmpty();
    }

    [Fact]
    public void ParseSearchTable_ShouldParseSearchResults()
    {
        // Arrange
        var output = """
            Name                          Id                            Version       Match         Source
            ----------------------------------------------------------------------------------------------
            Docker Desktop                Docker.DockerDesktop          4.29.0                      winget
            """;

        // Act
        var packages = WingetCliParser.ParseSearchTable(output);

        // Assert
        packages.Should().HaveCount(1);
        packages[0].Id.Should().Be("Docker.DockerDesktop");
        packages[0].Name.Should().Be("Docker Desktop");
        packages[0].AvailableVersion.Should().Be("4.29.0");
    }

    [Theory]
    [InlineData("  12.5 MB / 50.0 MB (25%)", 25.0)]
    [InlineData("Downloading https://example.com/installer.exe (78%)", 78.0)]
    [InlineData("100%", 100.0)]
    public void ExtractProgressPercentage_ShouldExtractCorrectPercent(string line, double expected)
    {
        // Act
        var percent = WingetCliParser.ExtractProgressPercentage(line);

        // Assert
        percent.Should().Be(expected);
    }
}
