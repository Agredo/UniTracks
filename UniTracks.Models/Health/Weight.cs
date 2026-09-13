using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Models.Health;

public record Weight
{
    [Key]
    public Guid ID { get; set; }
    public double WeightValue { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Owner of the entry. Explicit so the profile page can attach a new weight to the
    /// user without loading the whole user graph (the column already existed as a shadow property
    /// of the EF model, which LiteDB mirrors as a plain field).</summary>
    public Guid? UserID { get; set; }

    //public User.User User { get; set; }
}
