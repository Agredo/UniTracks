namespace UniTracks.Models.GPS;

public record GPSInformatoion(Position Position, double PositionAccuracy, DateTimeOffset Timestamp, double Heading, double HeadingAccuracy, double Altitude, double Speed, double SpeedAccuracy)
{
}
