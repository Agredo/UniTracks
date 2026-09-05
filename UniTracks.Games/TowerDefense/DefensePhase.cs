namespace UniTracks.Games.TowerDefense;

/// <summary>Lifecycle phase of a defense run.</summary>
public enum DefensePhase
{
    /// <summary>Between waves — towers can be placed and the next wave can be started.</summary>
    Building,

    /// <summary>A wave is marching down the trail.</summary>
    WaveRunning,

    /// <summary>All lives lost — the run is over and the score can be saved.</summary>
    Lost,
}
