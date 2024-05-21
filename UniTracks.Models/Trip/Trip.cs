using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Models.Environment;

namespace UniTracks.Models.Trip;

public record Trip
{
    [Key]
    public Guid ID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public double? Distance { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxAltitude { get; set; }
    public double? MinAltitude { get; set; }
    public double? MaxAccuracy { get; set; }
    public double? MinAccuracy { get; set; }
    public double? MaxSpeedAccuracy { get; set; }
    public double? MinSpeedAccuracy { get; set; }
    public double? MaxHeading { get; set; }
    public double? MinHeading { get; set; }
    public double? MaxHeadingAccuracy { get; set; }
    public double? MinHeadingAccuracy { get; set; }
    public double? TotalTime { get; set; }
    public double? MovingTime { get; set; }
    public double? StoppedTime { get; set; }

    //public TripType TripType { get; set; }
    public List<Location.Location> Locations { get; set; }
    public List<Health.HeartRate>? HeartRates { get; set; }
    public List<Health.Weight>? Weights { get; set; }
    public List<Weather>? Weather { get; set; }
}
