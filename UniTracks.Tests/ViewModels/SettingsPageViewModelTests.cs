using UniTracks.Tests.ViewModels.Fakes;
using UniTracks.ViewModels.Pages;

namespace UniTracks.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="SettingsPageViewModel"/>. The switch is the only way to draw the raw GPS
/// track, so it must survive the trip back to the map: the value is read on construction and every
/// flip is written straight through to the settings store.
/// </summary>
public sealed class SettingsPageViewModelTests
{
    [Fact]
    public void Constructor_ReadsTheStoredValue()
    {
        var settings = new FakeTrackSmoothingSettings { IsEnabled = false };

        var viewModel = new SettingsPageViewModel(settings);

        Assert.False(viewModel.TrackSmoothingEnabled);
        Assert.Contains("Rohdaten", viewModel.SmoothingStateText);
    }

    [Fact]
    public void IsSmoothingEnabledByDefault()
    {
        var viewModel = new SettingsPageViewModel(new FakeTrackSmoothingSettings());

        Assert.True(viewModel.TrackSmoothingEnabled);
        Assert.Equal("Glättung aktiv", viewModel.SmoothingStateText);
    }

    [Fact]
    public void EveryFlip_IsPersisted()
    {
        var settings = new FakeTrackSmoothingSettings();
        var viewModel = new SettingsPageViewModel(settings);

        viewModel.TrackSmoothingEnabled = false;
        Assert.False(settings.IsEnabled);

        viewModel.TrackSmoothingEnabled = true;
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void FlippingTheSwitch_UpdatesTheStateText()
    {
        var viewModel = new SettingsPageViewModel(new FakeTrackSmoothingSettings());

        viewModel.TrackSmoothingEnabled = false;

        Assert.Contains("Rohdaten", viewModel.SmoothingStateText);
    }
}
