using Maui.BindableProperty.Generator.Core;
using Microsoft.Maui.Platform;
using UniTracks.Models.Trip;

namespace UniTracks.Maui.Views.Controls;

public partial class TripCard : Microsoft.Maui.Controls.ContentView
{
	public TripCard()
	{
		InitializeComponent();

	}

	[AutoBindable (OnChanged = nameof(TripDateTimeChanged))]
	private DateTimeOffset tripDateTime;

    [AutoBindable(OnChanged = nameof(TripEndDateTimeChanged))]
    private DateTimeOffset tripEndDateTime;

    [AutoBindable(OnChanged = nameof(MaxSpeedChanged))]
    private double maxSpeed;

    [AutoBindable(OnChanged = nameof(MinSpeedChanged))]
    private double minSpeed;

    [AutoBindable(OnChanged = nameof(AverageChanged))]
    private double averageSpeed;

    [AutoBindable(OnChanged = nameof(DistanceChanged))]
    private double distance;

    [AutoBindable(OnChanged = nameof(DurationChanged))]
    private TimeSpan duration;

    private void TripDateTimeChanged(DateTimeOffset newDateTimeOffset)
    {
        TripDateLabel.Text = newDateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
        SetNameLabelText();
    }

    private void TripEndDateTimeChanged(DateTimeOffset newDateTimeOffset)
    {
        //TripEndDateLabel.Text = newDateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void MaxSpeedChanged(double newMaxSpeed)
    {
        MaxSpeedLabel.Text = $"↗️{Math.Round(newMaxSpeed, 1)}m/s";
    }

    private void MinSpeedChanged(double newMinSpeed)
    {
        MinSpeedLabel.Text = $"↘️{Math.Round(newMinSpeed, 1)}m/s";
    }

    private void AverageChanged(double newAverageSpeed)
    {
        AverageSpeedLabel.Text = $"∅{Math.Round(newAverageSpeed, 1)}m/s";
    }

    private void DistanceChanged(double newDistance)
    {
        TripDistanceLabel.Text = $"🏁{Math.Round(newDistance, 1)}m";
    }
    private void DurationChanged(TimeSpan newDuration)
    {
        TripDurationLabel.Text = $"⏱️{newDuration.ToFormattedString("mm:ss")}min";
    }

    private void SetNameLabelText()
    {
        if (TripDateTime.Hour >= 6 && TripDateTime.Hour < 12)
        {
            TripNameLabel.Text = "Morgen Trip";
        }
        else if (TripDateTime.Hour >= 12 && TripDateTime.Hour < 18)
        {
            TripNameLabel.Text = "Mittags Trip";
        }
        else if (TripDateTime.Hour >= 18 && TripDateTime.Hour < 21)
        {
            TripNameLabel.Text = "Nachmittags Trip";
        }
        else
        {
            TripNameLabel.Text = "Abend Trip";
        }
    }
}