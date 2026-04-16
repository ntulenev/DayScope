using FluentAssertions;

using DayScope.Themes;

namespace DayScope.Tests;

public sealed class ThemeManagerTests
{
    [Fact(DisplayName = "Initialization loads the persisted theme and applies the resolved OS theme once.")]
    [Trait("Category", "Unit")]
    public void InitializeShouldLoadThePersistedThemeAndApplyTheResolvedOsThemeOnce()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Os);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Forest);
        var resourceApplier = new RecordingThemeResourceApplier();
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);
        var themeChangedCalls = 0;
        themeManager.ThemeChanged += (_, _) => themeChangedCalls++;

        // Act
        themeManager.Initialize();
        themeManager.Initialize();

        // Assert
        themeManager.SelectedMode.Should().Be(AppThemeMode.Os);
        themeManager.IsDarkTheme.Should().BeTrue();
        preferenceStore.LoadCalls.Should().Be(1);
        themeDetector.DetectCalls.Should().Be(1);
        resourceApplier.AppliedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.Forest);
        themeChangedCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Setting the same theme mode after initialization is a no-op.")]
    [Trait("Category", "Unit")]
    public void SetThemeModeShouldDoNothingWhenTheThemeModeIsUnchanged()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Dark);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Forest);
        var resourceApplier = new RecordingThemeResourceApplier();
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);

        themeManager.Initialize();

        // Act
        themeManager.SetThemeMode(AppThemeMode.Dark);

        // Assert
        preferenceStore.SavedModes.Should().BeEmpty();
        resourceApplier.AppliedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.Dark);
    }

    [Fact(DisplayName = "Setting a new theme mode saves the selection and applies it immediately.")]
    [Trait("Category", "Unit")]
    public void SetThemeModeShouldSaveTheSelectionAndApplyItImmediately()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Os);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Forest);
        var resourceApplier = new RecordingThemeResourceApplier();
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);
        var themeChangedCalls = 0;
        themeManager.ThemeChanged += (_, _) => themeChangedCalls++;

        themeManager.Initialize();

        // Act
        themeManager.SetThemeMode(AppThemeMode.Light);

        // Assert
        themeManager.SelectedMode.Should().Be(AppThemeMode.Light);
        themeManager.IsDarkTheme.Should().BeFalse();
        preferenceStore.SavedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.Light);
        resourceApplier.AppliedModes.Should().ContainInOrder(AppThemeMode.Forest, AppThemeMode.Light);
        themeChangedCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Setting a theme mode initializes lazily when the manager has not been initialized yet.")]
    [Trait("Category", "Unit")]
    public void SetThemeModeShouldInitializeLazilyWhenTheManagerHasNotBeenInitializedYet()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Dark);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Forest);
        var resourceApplier = new RecordingThemeResourceApplier();
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);

        // Act
        themeManager.SetThemeMode(AppThemeMode.Code);

        // Assert
        preferenceStore.LoadCalls.Should().Be(1);
        preferenceStore.SavedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.Code);
        resourceApplier.AppliedModes.Should().ContainInOrder(AppThemeMode.Dark, AppThemeMode.Code);
        themeManager.SelectedMode.Should().Be(AppThemeMode.Code);
        themeManager.IsDarkTheme.Should().BeTrue();
    }

    [Fact(DisplayName = "Initialization does not raise ThemeChanged when resources are not applied.")]
    [Trait("Category", "Unit")]
    public void InitializeShouldNotRaiseThemeChangedWhenResourcesAreNotApplied()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Os);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Forest);
        var resourceApplier = new RecordingThemeResourceApplier(applyResult: false);
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);
        var themeChangedCalls = 0;
        themeManager.ThemeChanged += (_, _) => themeChangedCalls++;

        // Act
        themeManager.Initialize();

        // Assert
        themeChangedCalls.Should().Be(0);
        resourceApplier.AppliedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.Forest);
        themeManager.IsDarkTheme.Should().BeTrue();
    }

    [Fact(DisplayName = "Setting Deep sea applies the dedicated dark theme mode.")]
    [Trait("Category", "Unit")]
    public void SetThemeModeShouldApplyDeepSeaTheme()
    {
        // Arrange
        var preferenceStore = new RecordingThemePreferenceStore(AppThemeMode.Os);
        var themeDetector = new RecordingOsThemeDetector(AppThemeMode.Light);
        var resourceApplier = new RecordingThemeResourceApplier();
        using var themeManager = new ThemeManager(preferenceStore, themeDetector, resourceApplier);

        themeManager.Initialize();

        // Act
        themeManager.SetThemeMode(AppThemeMode.DeepSea);

        // Assert
        themeManager.SelectedMode.Should().Be(AppThemeMode.DeepSea);
        themeManager.IsDarkTheme.Should().BeTrue();
        preferenceStore.SavedModes.Should().ContainSingle()
            .Which.Should().Be(AppThemeMode.DeepSea);
        resourceApplier.AppliedModes.Should().ContainInOrder(AppThemeMode.Light, AppThemeMode.DeepSea);
    }

    private sealed class RecordingThemePreferenceStore(AppThemeMode loadedMode) : IThemePreferenceStore
    {
        public int LoadCalls { get; private set; }

        public List<AppThemeMode> SavedModes { get; } = [];

        public AppThemeMode LoadThemeMode()
        {
            LoadCalls++;
            return loadedMode;
        }

        public void SaveThemeMode(AppThemeMode themeMode) => SavedModes.Add(themeMode);
    }

    private sealed class RecordingOsThemeDetector(AppThemeMode detectedMode) : IOsThemeDetector
    {
        public int DetectCalls { get; private set; }

        public AppThemeMode DetectThemeMode()
        {
            DetectCalls++;
            return detectedMode;
        }
    }

    private sealed class RecordingThemeResourceApplier(bool applyResult = true) : IThemeResourceApplier
    {
        public List<AppThemeMode> AppliedModes { get; } = [];

        public bool ApplyTheme(AppThemeMode themeMode)
        {
            AppliedModes.Add(themeMode);
            return applyResult;
        }
    }
}
