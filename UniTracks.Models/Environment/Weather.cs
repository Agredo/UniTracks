using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Models.Environment;

public record Weather
{
    public Guid ID { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public double CloudCover { get; set; }
    public Location.Location Location { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
