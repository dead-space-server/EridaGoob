// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Movement.Components;

namespace Content.Shared._Erida.Silicons.StationAi;

/// <summary>
/// Reach of the station AI to grids with an active remote core, see <see cref="AiRemoteGridComponent"/>.
/// </summary>
public sealed class AiRemoteGridSystem : EntitySystem
{
    private EntityQuery<AiRemoteGridComponent> _remoteQuery;
    private EntityQuery<RelayInputMoverComponent> _relayQuery;

    public override void Initialize()
    {
        base.Initialize();

        _remoteQuery = GetEntityQuery<AiRemoteGridComponent>();
        _relayQuery = GetEntityQuery<RelayInputMoverComponent>();
    }

    /// <summary>
    /// Whether the AI whose core is on <paramref name="aiGrid"/> can reach <paramref name="grid"/>.
    /// </summary>
    public bool CanReach(EntityUid? aiGrid, EntityUid? grid)
    {
        return grid != null && (grid == aiGrid || _remoteQuery.HasComp(grid));
    }

    /// <summary>
    /// Remote grid the eye of <paramref name="ai"/> is on.
    /// </summary>
    public bool TryGetEyeRemoteGrid(EntityUid? ai, [NotNullWhen(true)] out EntityUid? grid)
    {
        grid = null;
        if (!_relayQuery.TryComp(ai, out var relay)
            || Transform(relay.RelayEntity).GridUid is not { } eyeGrid
            || !_remoteQuery.HasComp(eyeGrid))
            return false;

        grid = eyeGrid;
        return true;
    }

    /// <summary>
    /// Whether <paramref name="targetGrid"/> is the remote grid the eye of <paramref name="ai"/> is on.
    /// </summary>
    public bool IsEyeRemoteGrid(EntityUid ai, EntityUid? targetGrid)
    {
        return targetGrid != null && TryGetEyeRemoteGrid(ai, out var grid) && grid == targetGrid;
    }

    public void SetRemote(EntityUid grid, bool remote)
    {
        if (remote)
            EnsureComp<AiRemoteGridComponent>(grid);
        else
            RemComp<AiRemoteGridComponent>(grid);
    }
}
