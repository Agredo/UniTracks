using CommunityToolkit.Maui;
using System.Globalization;
using UniTracks.Services.Comparison;
using UniTracks.Services.Text;
using UniTracks.Services.Trips;
using HeartRateModel = UniTracks.Models.Health.HeartRate;
using TripTypeModel = UniTracks.Models.Trip.TripType;
using WeatherModel = UniTracks.Models.Environment.Weather;

namespace UniTracks.Maui.Views.Controls;

public partial class TripCard : Microsoft.Maui.Controls.ContentView
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public TripCard()
    {
        InitializeComponent();
        ApplyTypeVisual(TripTypeVisuals.For(null));
        UpdateExtrasVisibility();
    }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTripDateTimePropertyChanged))]
    public partial DateTimeOffset TripDateTime { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnNamePropertyChanged))]
    public partial string? Name { get; set; }

    [BindableProperty]
    public partial DateTimeOffset TripEndDateTime { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnMaxSpeedPropertyChanged))]
    public partial double MaxSpeed { get; set; }

    [BindableProperty]
    public partial double MinSpeed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnAverageSpeedPropertyChanged))]
    public partial double AverageSpeed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnDistancePropertyChanged))]
    public partial double Distance { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnDurationPropertyChanged))]
    public partial TimeSpan Duration { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTripTypePropertyChanged))]
    public partial TripTypeModel? TripType { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnWeatherPropertyChanged))]
    public partial List<WeatherModel>? Weather { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeartRatesPropertyChanged))]
    public partial List<HeartRateModel>? HeartRates { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnAltitudePropertyChanged))]
    public partial double? MaxAltitude { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnAltitudePropertyChanged))]
    public partial double? MinAltitude { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnIsCompactPropertyChanged))]
    public partial bool IsCompact { get; set; }

    private static void OnTripDateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.TripDateLabel.Text = RelativeDateFormatter.Format((DateTimeOffset)newValue);
        tripCard.SetNameLabelText();
    }

    private static void OnNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((TripCard)bindable).SetNameLabelText();
    }

    private static void OnMaxSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.MaxSpeedValueLabel.Text = ToKilometersPerHour((double)newValue);
    }

    private static void OnAverageSpeedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.AverageSpeedValueLabel.Text = ToKilometersPerHour((double)newValue);
        tripCard.UpdateCompactStats();
    }

    private static void OnDistancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        var newDistance = (double)newValue;

        if (newDistance >= 1000)
        {
            tripCard.DistanceValueLabel.Text = (newDistance / 1000).ToString("0.00", GermanCulture);
            tripCard.DistanceUnitLabel.Text = "km";
        }
        else
        {
            tripCard.DistanceValueLabel.Text = Math.Round(newDistance).ToString("0", GermanCulture);
            tripCard.DistanceUnitLabel.Text = "m";
        }
    }

    private static void OnDurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        tripCard.DurationValueLabel.Text = FormatDuration((TimeSpan)newValue);
        tripCard.UpdateCompactStats();
    }

    private static void OnTripTypePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        var type = (TripTypeModel?)newValue;

        tripCard.ApplyTypeVisual(TripTypeVisuals.For(type));

        tripCard.TypeBadge.IsVisible = type is not null;
        tripCard.TypeBadgeLabel.Text = type?.Name.ToUpper(GermanCulture) ?? string.Empty;
    }

    private static void OnWeatherPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        var weather = (newValue as List<WeatherModel>)?.FirstOrDefault();

        if (weather is null)
        {
            tripCard.WeatherBlock.IsVisible = false;
        }
        else
        {
            tripCard.WeatherIconLabel.Text = weather.CloudCover switch
            {
                < 25 => "☀️",
                < 60 => "⛅",
                _ => "☁️",
            };
            tripCard.WeatherTempLabel.Text = $"{Math.Round(weather.Temperature)}°";
            tripCard.WeatherBlock.IsVisible = true;
        }

        tripCard.UpdateExtrasVisibility();
    }

    private static void OnHeartRatesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        var rates = newValue as List<HeartRateModel>;

        if (rates is null || rates.Count == 0)
        {
            tripCard.HeartRateBlock.IsVisible = false;
        }
        else
        {
            tripCard.HeartRateLabel.Text = $"{Math.Round(rates.Average(rate => rate.Rate))} bpm";
            tripCard.HeartRateBlock.IsVisible = true;
        }

        tripCard.UpdateExtrasVisibility();
    }

    private static void OnAltitudePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;

        if (tripCard.MaxAltitude is { } max && tripCard.MinAltitude is { } min && max - min >= 25)
        {
            tripCard.ElevationLabel.Text = $"{Math.Round(max - min)} m";
            tripCard.ElevationBlock.IsVisible = true;
        }
        else
        {
            tripCard.ElevationBlock.IsVisible = false;
        }

        tripCard.UpdateExtrasVisibility();
    }

    private static void OnIsCompactPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tripCard = (TripCard)bindable;
        var compact = (bool)newValue;

        tripCard.StatsGrid.IsVisible = !compact;
        tripCard.CompactStatsLabel.IsVisible = compact;
        tripCard.IconBadge.HeightRequest = tripCard.IconBadge.WidthRequest = compact ? 44 : 64;
        tripCard.TypeIconImage.HeightRequest = tripCard.TypeIconImage.WidthRequest = compact ? 24 : 32;

        tripCard.UpdateCompactStats();
        tripCard.UpdateExtrasVisibility();
    }

    private void ApplyTypeVisual(TripTypeVisual visual)
    {
        TypeIconImage.Source = visual.Icon;

        var accent = Microsoft.Maui.Graphics.Color.FromArgb(visual.Accent);
        var soft = Microsoft.Maui.Graphics.Color.FromArgb(visual.Soft);

        IconBadge.Background = soft;
        IconBadge.Stroke = accent.WithAlpha(0.45f);
        DistanceValueLabel.TextColor = accent;
        TypeBadge.Background = soft;
        TypeBadgeLabel.TextColor = accent;
    }

    private void UpdateExtrasVisibility()
    {
        var anyExtra = WeatherBlock.IsVisible || HeartRateBlock.IsVisible || ElevationBlock.IsVisible;
        ExtrasRow.IsVisible = anyExtra && !IsCompact;
    }

    private void UpdateCompactStats()
    {
        if (!IsCompact)
        {
            return;
        }

        CompactStatsLabel.Text = $"· {FormatDuration(Duration)} · {ToKilometersPerHour(AverageSpeed)} km/h";
    }

    private void SetNameLabelText()
    {
        TripNameLabel.Text = !string.IsNullOrWhiteSpace(Name)
            ? Name
            : TripDisplay.TimeOfDayName(TripDateTime);
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? duration.ToString(@"h\:mm\:ss", GermanCulture)
        : duration.ToString(@"mm\:ss", GermanCulture);

    private static string ToKilometersPerHour(double metersPerSecond)
    {
        return Math.Round(metersPerSecond * 3.6, 1).ToString("0.0", GermanCulture);
    }
}
