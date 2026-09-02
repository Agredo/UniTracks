using CommunityToolkit.Maui;
using Microsoft.Maui.Platform;

namespace UniTracks.Maui.Views.Controls;

public partial class TripCard : Microsoft.Maui.Controls.ContentView
{
    public TripCard()
    {
        InitializeComponent();
    }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTripDateTimePropertyChanged))]
    public partial DateTimeOffset TripDateTime { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTripEndDateTimePropertyChanged))]
    public partial DateTimeOffset TripEndDateTime { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnMaxSpeedPropertyChanged))]
    public partial double MaxSpeed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnMinSpeedPropertyChanged))]
    public partial double MinSpeed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnAverageSpeedPropertyChanged))]
    public partial double AverageSpeed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnDistancePropertyChanged))]
    public partial double Distance { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnDurationPropertyChanged))]
    public partial TimeSpan Duration { get; set; }

    private static void OnTripDateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.TripDateTimeChanged((DateTimeOffset)newValue);
    }

    private static void OnTripEndDateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.TripEndDateTimeChanged((DateTimeOffset)newValue);
    }

    private static void OnMaxSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.MaxSpeedChanged((double)newValue);
    }

    private static void OnMinSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.MinSpeedChanged((double)newValue);
    }

    private static void OnAverageSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.AverageChanged((double)newValue);
    }

    private static void OnDistancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.DistanceChanged((double)newValue);
    }

    private static void OnDurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.DurationChanged((TimeSpan)newValue);
    }

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
        try
        {
            TripDurationLabel.Text = $"⏱️{newDuration.ToFormattedString("mm:ss")}min";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void SetNameLabelText()
    {
        if (TripDateTime.Hour >= 5 && TripDateTime.Hour < 11)
        {
            TripNameLabel.Text = "Morgen Trip";
        }
        else if (TripDateTime.Hour >= 11 && TripDateTime.Hour < 14)
        {
            TripNameLabel.Text = "Mittags Trip";
        }
        else if (TripDateTime.Hour >= 14 && TripDateTime.Hour < 18)
        {
            TripNameLabel.Text = "Nachmittags Trip";
        }
        else
        {
            TripNameLabel.Text = "Abend Trip";
        }
    }
}