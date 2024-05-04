using System.Runtime.CompilerServices;
using System.Text;

namespace UniTracks.Models.GPS;

public partial record Position
{
    public double Longitude { get; }
    public double Latitude { get; }
    public bool Valid { get; }

    public Position(double longitude, double latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
        Valid = PositionValidator.Assert(Latitude, Longitude);
        //Distance = Distance.Between(this, other);
    }

    public double GetCompassBearingTo(Position to)
    {
        double num = ToRad(to.Longitude - Longitude);
        double x = Math.Log(Math.Tan(ToRad(to.Latitude) / 2.0 + Math.PI / 4.0) / Math.Tan(ToRad(Latitude) / 2.0 + Math.PI / 4.0));
        if (Math.Abs(num) > Math.PI)
        {
            num = ((num > 0.0) ? (0.0 - (Math.PI * 2.0 - num)) : (Math.PI * 2.0 + num));
        }

        return ToBearing(Math.Atan2(num, x));
    }

    public static double ToRad(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    public static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    public static double ToBearing(double radians)
    {
        return (ToDegrees(radians) + 360.0) % 360.0;
    }

    [CompilerGenerated]
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("Latitude = ");
        builder.Append(Latitude.ToString());
        builder.Append(", Longitude = ");
        builder.Append(Longitude.ToString());
        return true;
    }
}
