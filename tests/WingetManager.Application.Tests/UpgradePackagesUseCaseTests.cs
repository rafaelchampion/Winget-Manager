using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WingetManager.Application.UseCases;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;
using Xunit;

namespace WingetManager.Application.Tests;

public class UpgradePackagesUseCaseTests
{
    private readonly IPackageRepository _packageRepo = Substitute.For<IPackageRepository>();
    private readonly IElevationService _elevationService = Substitute.For<IElevationService>();
    private readonly ISettingsRepository _settingsRepo = Substitute.For<ISettingsRepository>();
    private readonly WingetManager.Application.Services.IPackageCacheService _cacheService = Substitute.For<WingetManager.Application.Services.IPackageCacheService>();

    [Fact]
    public async Task ExecuteAsync_ShouldUpgradePackagesConcurrently_AndReturnAllResults()
    {
        // Arrange
        var settings = new AppSettings { MaxConcurrentUpgrades = 2 };
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);

        _packageRepo.UpgradePackageAsync("Package.A", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageUpgradeResult("Package.A", true, OldVersion: "1.0", NewVersion: "1.1"));
        _packageRepo.UpgradePackageAsync("Package.B", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageUpgradeResult("Package.B", true, OldVersion: "2.0", NewVersion: "2.1"));

        var progressReports = new List<UpgradeProgress>();
        var progress = new Progress<UpgradeProgress>(p => progressReports.Add(p));

        var useCase = new UpgradePackagesUseCase(
            _packageRepo,
            _elevationService,
            _settingsRepo,
            _cacheService,
            NullLogger<UpgradePackagesUseCase>.Instance);

        // Act
        var results = await useCase.ExecuteAsync(["Package.A", "Package.B"], progress);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.PackageId == "Package.A" && r.Success);
        results.Should().Contain(r => r.PackageId == "Package.B" && r.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPackageRequiresElevation_ShouldInvokeElevationService()
    {
        // Arrange
        var settings = new AppSettings { MaxConcurrentUpgrades = 2 };
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);

        _packageRepo.UpgradePackageAsync("Elevated.Package", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageUpgradeResult("Elevated.Package", false, ErrorMessage: "Access is denied (requires elevation)"));

        _elevationService.RunElevatedUpgradeAsync("Elevated.Package", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageUpgradeResult("Elevated.Package", true, OldVersion: "1.0", NewVersion: "2.0"));

        var useCase = new UpgradePackagesUseCase(
            _packageRepo,
            _elevationService,
            _settingsRepo,
            _cacheService,
            NullLogger<UpgradePackagesUseCase>.Instance);

        // Act
        var results = await useCase.ExecuteAsync(["Elevated.Package"]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        await _elevationService.Received(1).RunElevatedUpgradeAsync("Elevated.Package", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneTaskCancelled_OtherTasksShouldCompleteSuccessfully()
    {
        // Arrange
        var settings = new AppSettings { MaxConcurrentUpgrades = 2 };
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);

        using var ctsCancelled = new CancellationTokenSource();
        ctsCancelled.Cancel();

        _packageRepo.UpgradePackageAsync("Package.Cancelled", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<PackageUpgradeResult>(ctsCancelled.Token));

        _packageRepo.UpgradePackageAsync("Package.Success", Arg.Any<IProgress<UpgradeProgress>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PackageUpgradeResult("Package.Success", true, OldVersion: "1.0", NewVersion: "1.1")));

        var useCase = new UpgradePackagesUseCase(
            _packageRepo,
            _elevationService,
            _settingsRepo,
            _cacheService,
            NullLogger<UpgradePackagesUseCase>.Instance);

        var tasks = new List<UpgradePackageTask>
        {
            new("Package.Cancelled", ctsCancelled.Token),
            new("Package.Success", CancellationToken.None)
        };

        // Act
        var results = await useCase.ExecuteAsync(tasks);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.PackageId == "Package.Cancelled" && !r.Success && r.ErrorMessage == "Cancelled by user");
        results.Should().Contain(r => r.PackageId == "Package.Success" && r.Success);
    }
}
