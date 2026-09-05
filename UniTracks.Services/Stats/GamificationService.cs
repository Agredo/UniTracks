using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniTracks.Data.Repository;
using UniTracks.Models.Achievement;
using UniTracks.Models.Trip;

namespace UniTracks.Services.Stats;

public class GamificationService : IGamificationService
{
    private const int XpPerLevel = 100;

    private readonly IRepository repository;

    public GamificationService(IRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GamificationStats> ComputeAsync()
    {
        var trips = (await repository.GetAllAsync<Trip>()).ToList();

        double totalDistanceKm = trips.Sum(t => t.Distance ?? 0) / 1000.0;
        int totalTrips = trips.Count;

        int xp = (int)Math.Floor(totalDistanceKm) + totalTrips * 2;
        int level = xp / XpPerLevel + 1;
        double levelProgressFraction = (xp % XpPerLevel) / (double)XpPerLevel;

        var activeDays = trips
            .Select(t => t.StartTime.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        int bestStreak = ComputeBestStreak(activeDays);
        int currentStreak = ComputeCurrentStreak(activeDays);

        double longestTripKm = trips.Select(t => t.Distance ?? 0).DefaultIfEmpty(0).Max() / 1000.0;
        double maxAltitude = trips.Select(t => t.MaxAltitude ?? 0).DefaultIfEmpty(0).Max();

        var achievements = new List<Achievement>
        {
            Make("first-trip", "Erster Trip", "Dein erster aufgezeichneter Trip.", "🎉", totalTrips, 1),
            Make("ten-trips", "10 Trips", "Zehn aufgezeichnete Trips.", "🏅", totalTrips, 10),
            Make("twentyfive-trips", "25 Trips", "Fünfundzwanzig aufgezeichnete Trips.", "🏆", totalTrips, 25),
            Make("ten-km", "10 km gesamt", "Insgesamt 10 Kilometer zurückgelegt.", "🚀", totalDistanceKm, 10),
            Make("fifty-km", "50 km gesamt", "Insgesamt 50 Kilometer zurückgelegt.", "🥇", totalDistanceKm, 50),
            Make("hundred-km", "100 km gesamt", "Insgesamt 100 Kilometer zurückgelegt.", "💯", totalDistanceKm, 100),
            Make("long-trip", "Langer Trip", "Ein einzelner Trip über 10 km.", "🥾", longestTripKm, 10),
            Make("marathon", "Marathon-Bereit", "Ein einzelner Trip über 42,2 km.", "🏃", longestTripKm, 42.195),
            Make("summit", "Gipfelstürmer", "Über 1000 m Höhe erreicht.", "⛰️", maxAltitude, 1000),
            Make("streak-3", "3-Tage-Streak", "Drei Tage in Folge aktiv.", "🔥", bestStreak, 3),
            Make("streak-7", "7-Tage-Streak", "Sieben Tage in Folge aktiv.", "🔥", bestStreak, 7),
            Make("streak-30", "30-Tage-Streak", "Dreißig Tage in Folge aktiv.", "👑", bestStreak, 30),
        };

        return new GamificationStats
        {
            TotalTrips = totalTrips,
            TotalDistanceKm = Math.Round(totalDistanceKm, 1),
            Xp = xp,
            Level = level,
            LevelProgressFraction = levelProgressFraction,
            BestStreakDays = bestStreak,
            CurrentStreakDays = currentStreak,
            Achievements = achievements,
        };
    }

    private static Achievement Make(string id, string title, string description, string icon, double progress, double target) =>
        new()
        {
            Id = id,
            Title = title,
            Description = description,
            Icon = icon,
            Progress = progress,
            Target = target,
            IsUnlocked = target > 0 && progress >= target,
        };

    private static int ComputeBestStreak(List<DateTime> days)
    {
        if (days.Count == 0)
        {
            return 0;
        }

        int best = 1;
        int current = 1;
        for (int i = 1; i < days.Count; i++)
        {
            if ((days[i] - days[i - 1]).TotalDays == 1)
            {
                current++;
                best = Math.Max(best, current);
            }
            else
            {
                current = 1;
            }
        }

        return best;
    }

    private static int ComputeCurrentStreak(List<DateTime> days)
    {
        if (days.Count == 0)
        {
            return 0;
        }

        // An active streak counts backwards from the most recent activity day.
        // It is only "current" if it touches today or yesterday.
        if (days[^1] < DateTime.Today.AddDays(-1))
        {
            return 0;
        }

        var active = days.ToHashSet();
        int streak = 0;
        var cursor = days[^1];
        while (active.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
