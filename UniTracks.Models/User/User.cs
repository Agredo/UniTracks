using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniTracks.Models.Health;
using UniTracks.Models.Trip;

namespace UniTracks.Models.User;

public record User
{
    public Guid ID { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }

    //Health Data
    public double? Height { get; set; }
    public string? BloodGroup { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Medications { get; set; }
    public string? Allergies { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? EmergencyContactEmail { get; set; }

    public List<Trip.Trip>? Trips { get; set; }
    public List<Weight>? Weights { get; set; }

}
