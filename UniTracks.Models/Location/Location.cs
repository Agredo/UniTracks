using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Models.Location;

public record Location()
{
    public Guid ID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Accuracy { get; set; }
    public double Speed { get; set; }


    public double SpeedAccuracy { get; set; }
    public double Heading { get; set; }
    public double HeadingAccuracy { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
