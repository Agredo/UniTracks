namespace UniTracks.Services.Comparison;

/// <summary>A latitude/longitude pair, so the comparison pipeline does not have to carry GPS models around.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>Axis-aligned latitude/longitude box of a track.</summary>
public readonly record struct GeoBounds(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude)
{
    public double CenterLatitude => (MinLatitude + MaxLatitude) / 2;

    public double CenterLongitude => (MinLongitude + MaxLongitude) / 2;
}
