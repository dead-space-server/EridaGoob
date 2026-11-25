// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Raised on a goal entity to get its progress from 0 to 1.
/// Each condition lowers <see cref="Progress"/> to its own value, so the goal is complete only when all are.
/// Null after raising means the goal has no conditions.
/// </summary>
[ByRefEvent]
public record struct StationGoalGetProgressEvent(EntityUid Station, float? Progress = null)
{
    public void Add(float progress)
    {
        Progress = Math.Min(Progress ?? 1f, Math.Clamp(progress, 0f, 1f));
    }
}

/// <summary>
/// Raised on a goal entity after it is assigned to <see cref="Station"/>.
/// </summary>
[ByRefEvent]
public readonly record struct StationGoalAssignedEvent(EntityUid Station);
