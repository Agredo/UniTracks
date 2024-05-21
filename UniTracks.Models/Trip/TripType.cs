using System.ComponentModel.DataAnnotations;

namespace UniTracks.Models.Trip;

public record TripType
{
    [Key]
    public Guid ID { get; init; }
    public string Name { get; init; }
    public string Identifier { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
}
