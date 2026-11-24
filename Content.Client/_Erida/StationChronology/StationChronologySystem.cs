// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.CCVar;
using Content.Shared._Erida.StationChronology;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client._Erida.StationChronology;

public sealed class StationChronology : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IOverlayManager _overMan = default!;

    private StationCreditsOverlay _overlay = default!;
    private bool _enabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new StationCreditsOverlay();
        _overMan.AddOverlay(_overlay);

        SubscribeNetworkEvent<ShowStationCreditsEvent>(OnShowStationCreditsEvent);
        Subs.CVar(_cfg, ECCVars.RoundJoinAnimation, OnAnimationChanged, true);
    }

    private void OnAnimationChanged(int animation)
    {
        _enabled = animation == (int) RoundJoinAnimationType.StationCredits;

        if (_enabled)
            return;

        _overlay.Reset();
        _overlay.ResetDescription();
    }

    private void OnShowStationCreditsEvent(ShowStationCreditsEvent args)
    {
        if (!_enabled)
            return;

        _overlay.Reset();
        _overlay.ResetDescription();

        _overlay.Text = args.Text;
        _overlay.TextDescription = args.Description;

        if (string.IsNullOrEmpty(_overlay.Text))
            _overlay.CharInterval = TimeSpan.Zero;
        else
            _overlay.CharInterval = TimeSpan.FromSeconds(2f / _overlay.Text.Length);

        if (_overlay.TextDescription == "")
            _overlay.CharIntervalDescription = TimeSpan.Zero;
        else
            _overlay.CharIntervalDescription = TimeSpan.FromSeconds(2f / _overlay.TextDescription.Length);
    }
}
