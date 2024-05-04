namespace UniTracks.Models.GPS;
public static class PositionValidator
{
    public static bool Assert(double latitude, double longitude)
    {
        if (latitude < -90.0 || latitude > 90.0)
        {
            return false;
        }

        if (longitude < -180.0 || longitude > 180.0)
        {
            return false;
        }

        return true;
    }
}
