using UniTracks.Games.TowerDefense;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Map-selection entry wrapping a <see cref="DefenseMap"/> with its current unlock state
/// (level + achievement gates), so locked maps can be greyed out with an explanation.
/// </summary>
public class DefenseMapItemViewModel
{
    public DefenseMapItemViewModel(DefenseMap map, DefenseProfile profile)
    {
        Map = map;

        bool levelOk = profile.Level >= map.RequiredLevel;
        bool achievementOk = map.RequiredAchievementIds.Length == 0
            || map.RequiredAchievementIds.Any(profile.UnlockedAchievementIds.Contains);

        IsUnlocked = levelOk && achievementOk;
        LockLabel =
            IsUnlocked ? string.Empty
            : !achievementOk ? $"🔒 Erfolg nötig: {string.Join(" oder ", map.RequiredAchievementIds.Select(AchievementName))}"
            : $"🔒 Ab Level {map.RequiredLevel}";
    }

    public DefenseMap Map { get; }

    /// <summary>Player may start a run on this map.</summary>
    public bool IsUnlocked { get; }

    public bool IsLocked => !IsUnlocked;

    /// <summary>Reason shown on locked maps ("" when unlocked).</summary>
    public string LockLabel { get; }

    /// <summary>Human-readable achievement name for gate messages.</summary>
    private static string AchievementName(string id) => id switch
    {
        "hundred-km" => "„100 km gesamt“",
        "fifty-km" => "„50 km gesamt“",
        "ten-km" => "„10 km gesamt“",
        "marathon" => "„Marathon-Bereit“",
        "summit" => "„Gipfelstürmer“",
        "streak-3" => "„3-Tage-Streak“",
        "streak-7" => "„7-Tage-Streak“",
        "streak-30" => "„30-Tage-Streak“",
        _ => $"„{id}“",
    };
}
