// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.StationGoals;
using Content.Shared.GameTicking;

namespace Content.Client._Erida.StationGoals;

/// <summary>
/// Keeps station goal results of the last round for the round end summary.
/// </summary>
public sealed class StationGoalSummarySystem : EntitySystem
{
    public IReadOnlyList<StationGoalResult> Results => _results;

    private List<StationGoalResult> _results = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StationGoalResultsEvent>(OnResults);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnResults(StationGoalResultsEvent ev)
    {
        _results = ev.Results;
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _results = [];
    }
}
