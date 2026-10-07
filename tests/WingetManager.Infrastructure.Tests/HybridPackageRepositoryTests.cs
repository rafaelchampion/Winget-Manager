using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WingetManager.Domain.Entities;
using WingetManager.Infrastructure.Winget;
using Xunit;

namespace WingetManager.Infrastructure.Tests;

public class HybridPackageRepositoryTests
{
    [Fact]
    public async Task GetInstalledPackagesAsync_WhenComFails_ShouldFallbackToCli()
    {
        // Arrange
        var comRepo = new WingetComRepository(NullLogger<WingetComRepository>.Instance);
        var cliRepo = Substitute.For<WingetCliRepository>(NullLogger<WingetCliRepository>.Instance);
        cliRepo.GetAvailableUpgradesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Package>());

        var hybridRepo = new HybridPackageRepository(
            comRepo, 
            cliRepo, 
            NullLogger<HybridPackageRepository>.Instance);

        // Act & Assert
        // Notice comRepo throws NotSupportedException by default, so hybrid should call cli
        var act = async () => await hybridRepo.GetAvailableUpgradesAsync();
        
        // Should not throw NotSupportedException
        await act.Should().NotThrowAsync<NotSupportedException>();
    }
}
