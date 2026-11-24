// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

namespace Content.Shared._Erida.Actions;

/// <summary>
/// Raised on the performer before an action is used. Unlike <see cref="Content.Shared.Actions.Events.ActionAttemptEvent"/>,
/// which is raised on the action entity, this lets the user's own components veto every action.
/// </summary>
[ByRefEvent]
public record struct UserActionAttemptEvent(EntityUid Action, bool Cancelled = false);
