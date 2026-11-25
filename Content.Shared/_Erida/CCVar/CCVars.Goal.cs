using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Send station goal on round start or not.
    /// </summary>
    public static readonly CVarDef<bool> StationGoal =
        CVarDef.Create("game.station_goal", true, CVar.SERVERONLY);

    /// <summary>
    /// Seconds after round start before the station goal is sent.
    /// </summary>
    public static readonly CVarDef<float> StationGoalDelay =
        CVarDef.Create("game.station_goal_delay", 600f, CVar.SERVERONLY);
}
