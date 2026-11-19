// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Markings;
using Content.Shared.MagicMirror;
using Robust.Shared.Timing;

namespace Content.Client.MagicMirror;

public sealed partial class MagicMirrorWindow
{
    /// <summary>
    /// How long the colors and gradient must stay unchanged before they are sent.
    /// </summary>
    private const float AppearanceSendDelay = 0.3f;

    /// <summary>
    /// Raised with the colors and gradient of a slot once the user stops changing them.
    /// One message carries both, so a later change never cancels the server's do-after for the other.
    /// </summary>
    public Action<(MagicMirrorCategory category, int slot, Marking marking)>? OnAppearanceChanged;

    private (MagicMirrorCategory Category, int Slot, Marking Marking)? _pendingAppearance;
    private float _pendingAppearanceTime;

    /// <summary>
    /// Subscribes before the other handlers, so a pending change is sent before an action that replaces it.
    /// </summary>
    private void InitializeAppearanceQueue()
    {
        HairPicker.OnMarkingSelect += _ => FlushAppearance();
        HairPicker.OnSlotRemove += _ => FlushAppearance();
        HairPicker.OnSlotAdd += FlushAppearance;
        FacialHairPicker.OnMarkingSelect += _ => FlushAppearance();
        FacialHairPicker.OnSlotRemove += _ => FlushAppearance();
        FacialHairPicker.OnSlotAdd += FlushAppearance;
        OnClose += FlushAppearance;
    }

    /// <summary>
    /// Sliders fire on every mouse move, and every message restarts the do-after and shows a popup.
    /// </summary>
    private void QueueAppearance(MagicMirrorCategory category, (int slot, Marking marking) args)
    {
        if (_pendingAppearance is { } pending && (pending.Category != category || pending.Slot != args.slot))
            FlushAppearance();

        _pendingAppearance = (category, args.slot, new Marking(args.marking));
        _pendingAppearanceTime = 0f;
    }

    private void FlushAppearance()
    {
        if (_pendingAppearance is not { } pending)
            return;

        _pendingAppearance = null;
        OnAppearanceChanged?.Invoke((pending.Category, pending.Slot, pending.Marking));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_pendingAppearance == null)
            return;

        _pendingAppearanceTime += args.DeltaSeconds;
        if (_pendingAppearanceTime >= AppearanceSendDelay)
            FlushAppearance();
    }
}
