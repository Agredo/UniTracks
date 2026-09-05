using System.Threading.Tasks;
using UniTracks.Models.Achievement;

namespace UniTracks.Services.Stats;

/// <summary>Computes gamification state (level, XP, streaks, achievements) from the recorded trips.</summary>
public interface IGamificationService
{
    Task<GamificationStats> ComputeAsync();
}
