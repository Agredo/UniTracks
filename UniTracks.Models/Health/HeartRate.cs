﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Models.User;

namespace UniTracks.Models.Health;

public record HeartRate
{
    [Key]
    public Guid ID { get; set; }
    public double Rate { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    //public User.User user { get; set; }
}
