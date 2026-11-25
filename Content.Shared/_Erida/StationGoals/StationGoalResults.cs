// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Erida.StationGoals;

/// <summary>
/// Station goal result for the round end summary.
/// </summary>
[Serializable, NetSerializable]
public struct StationGoalResult
{
    public required string Station;

    public required LocId Goal;

    /// <summary>
    /// Progress from 0 to 1, null for a goal without conditions.
    /// </summary>
    public required float? Progress;

    /// <summary>
    /// One markup line: goal and whether it is completed.
    /// </summary>
    /// <param name="withStation">Prefix the station name, needed when there are several stations.</param>
    public readonly string Format(bool withStation)
    {
        var key = Progress switch
        {
            null => "station-goal-round-end-manual",
            >= 1f => "station-goal-round-end-completed",
            _ => "station-goal-round-end-failed",
        };

        var result = Loc.GetString(key,
            ("goal", Loc.GetString(Goal)),
            ("percent", (int) MathF.Floor((Progress ?? 0f) * 100f)));

        return withStation
            ? Loc.GetString("station-goal-round-end-station", ("station", Station), ("result", result))
            : result;
    }
}

/// <summary>
/// Sent to all clients at round end, before the round end summary.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationGoalResultsEvent(List<StationGoalResult> results) : EntityEventArgs
{
    public List<StationGoalResult> Results { get; } = results;
}
