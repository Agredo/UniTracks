using System;
using System.Collections.Generic;

namespace UniTracks.Models.Achievement;

/// <summary>A single achievement/badge with progress toward an unlock goal.</summary>
public record Achievement
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "🏅";

    /// <summary>Current value toward the goal (e.g. total km, trip count, streak days).</summary>
    public double Progress { get; init; }

    /// <summary>Goal value. A target of 0 means a boolean state that ignores progress.</summary>
    public double Target { get; init; }

    public bool IsUnlocked { get; init; }

    /// <summary>Fraction 0..1 for progress bars; locked boolean badges show 0.</summary>
    public double ProgressFraction =>
        Target > 0 ? Math.Clamp(Progress / Target, 0, 1) : (IsUnlocked ? 1 : 0);

    public string ProgressText =>
        Target > 1 ? $"{Progress:0.#} / {Target:0.#}" : (IsUnlocked ? "Geschafft!" : "Noch offen");
}

/// <summary>Aggregated gamification state computed from the recorded trips.</summary>
public record GamificationStats
{
    public int TotalTrips { get; init; }
    public double TotalDistanceKm { get; init; }
    public int Xp { get; init; }
    public int Level { get; init; }
    public double LevelProgressFraction { get; init; }
    public int BestStreakDays { get; init; }
    public int CurrentStreakDays { get; init; }
    public IReadOnlyList<Achievement> Achievements { get; init; } = Array.Empty<Achievement>();

    public string LevelLabel => $"Level {Level}";
    public string XpLabel => $"{Xp} XP";
}
