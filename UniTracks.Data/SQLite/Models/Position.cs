using NetTopologySuite.Geometries;

namespace UniTracks.Data.SQLite.Models;

public record Position
{
    public Guid ID { get; set; }
    Location Location { get; set; }
    UniTracks.Models.Location.Location DetailedLocation { get; set; }
}
