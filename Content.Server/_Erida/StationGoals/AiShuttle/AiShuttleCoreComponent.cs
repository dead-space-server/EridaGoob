// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Erida.StationGoals.AiShuttle;

/// <summary>
/// Shuttle console the station AI eye can pilot the shuttle from, the AI also sees and interacts on the grid of an active core.
/// </summary>
[RegisterComponent, Access(typeof(AiShuttleCoreSystem))]
public sealed partial class AiShuttleCoreComponent : Component
{
    /// <summary>
    /// Station the core was delivered to, only its goal counts this core.
    /// </summary>
    [ViewVariables]
    public EntityUid? Station;

    /// <summary>
    /// Anchored and powered, its grid has <see cref="Content.Shared._Erida.Silicons.StationAi.AiRemoteGridComponent"/>.
    /// </summary>
    [ViewVariables]
    public bool Active;
}
