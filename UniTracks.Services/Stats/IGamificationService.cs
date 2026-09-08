using System.Collections.Generic;
using System.Threading.Tasks;
using UniTracks.Models.Achievement;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Stats;

/// <summary>Computes gamification state (level, XP, streaks, achievements) from the recorded trips.</summary>
public interface IGamificationService
{
    /// <summary>Loads qualifying trips and computes gamification state.</summary>
    Task<GamificationStats> ComputeAsync();

    /// <summary>Computes gamification state over already-loaded qualifying trips (avoids a second trip scan).</summary>
    Task<GamificationStats> ComputeAsync(IEnumerable<Trip> trips);
}
