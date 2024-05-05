using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Models.Health;

public record Weight
{
    public Guid ID { get; set; }
    public double WeightValue { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public User.User User { get; set; }
}
