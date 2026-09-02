using CommunityToolkit.Maui;
using System.Globalization;

namespace UniTracks.Maui.Views.Controls;

public partial class TripCard : Microsoft.Maui.Controls.ContentView
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

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
        // Reserved for future use.
    }

    private static void OnMaxSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.MaxSpeedChanged((double)newValue);
    }

    private static void OnMinSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Reserved for future use.
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
        TripDateLabel.Text = newDateTimeOffset.ToString("ddd, dd. MMM · HH:mm", GermanCulture);
        SetNameLabelText();
    }

    private void MaxSpeedChanged(double newMaxSpeed)
    {
        MaxSpeedValueLabel.Text = ToKilometersPerHour(newMaxSpeed);
    }

    private void AverageChanged(double newAverageSpeed)
    {
        AverageSpeedValueLabel.Text = ToKilometersPerHour(newAverageSpeed);
    }

    private void DistanceChanged(double newDistance)
    {
        if (newDistance >= 1000)
        {
            DistanceValueLabel.Text = (newDistance / 1000).ToString("0.00", GermanCulture);
            DistanceUnitLabel.Text = "km";
        }
        else
        {
            DistanceValueLabel.Text = Math.Round(newDistance).ToString("0", GermanCulture);
            DistanceUnitLabel.Text = "m";
        }
    }

    private void DurationChanged(TimeSpan newDuration)
    {
        DurationValueLabel.Text = newDuration.TotalHours >= 1
            ? newDuration.ToString(@"h\:mm\:ss", GermanCulture)
            : newDuration.ToString(@"mm\:ss", GermanCulture);
    }

    private static string ToKilometersPerHour(double metersPerSecond)
    {
        return Math.Round(metersPerSecond * 3.6, 1).ToString("0.0", GermanCulture);
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
