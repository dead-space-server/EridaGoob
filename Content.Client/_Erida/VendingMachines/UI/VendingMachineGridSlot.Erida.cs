// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using System.Text;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Labels.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client._Funkystation.VendingMachines.UI;

public sealed partial class VendingMachineGridSlot
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private const float NameStripHeight = 16f;
    private const float NameStripInset = 2f;
    private const float NamePadding = 3f;
    private const string Ellipsis = "…";

    private readonly PanelContainer _nameStrip = new()
    {
        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black.WithAlpha(0.7f) },
    };

    private readonly Label _nameLabel = new()
    {
        ClipText = true,
        Align = Label.AlignMode.Center,
        VAlign = Label.VAlignMode.Center,
        FontColorOverride = Color.White,
        Margin = new Thickness(NamePadding, 0),
    };

    private Font _nameFont = null!;

    /// <summary>
    /// Starting solution to show in the fill layer of the preview, and the preview it was applied to.
    /// </summary>
    private Solution? _contents;
    private EntityUid? _contentsShownOn;

    public event Action? Pressed;

    /// <summary>
    /// Name shown on the slot: the label of a labeled container, otherwise the item name.
    /// </summary>
    public string ShortName { get; private set; } = string.Empty;

    private void InitializeErida()
    {
        _nameFont = _resCache.NotoStack(size: 9);
        _nameLabel.FontOverride = _nameFont;

        // Inset by the highlight border thickness so the strip does not cover it.
        _nameStrip.SetSize = new Vector2(SlotSize - NameStripInset * 2, NameStripHeight);
        _nameStrip.AddChild(_nameLabel);
        SlotLayout.AddChild(_nameStrip);
        LayoutContainer.SetPosition(_nameStrip, new Vector2(NameStripInset, SlotSize - NameStripHeight - NameStripInset));

        OnKeyBindDown += OnSlotKeyBindDown;
    }

    private void SetName(EntityPrototype prototype)
    {
        ShortName = GetShortName(prototype);
        _nameLabel.Text = Truncate(ShortName, SlotSize - NameStripInset * 2 - NamePadding * 2);

        _contents = prototype.TryComp<SolutionContainerVisualsComponent>(out _, _componentFactory)
            ? GetContents(prototype)
            : null;
        _contentsShownOn = null;
    }

    private string GetShortName(EntityPrototype prototype)
    {
        if (prototype.TryComp<LabelComponent>(out var label, _componentFactory)
            && !string.IsNullOrWhiteSpace(label.CurrentLabel))
        {
            return _loc.TryGetString(label.CurrentLabel, out var localized) ? localized : label.CurrentLabel;
        }

        return prototype.Name;
    }

    /// <summary>
    /// Cuts the text to fit the width and ends it with an ellipsis, since the label can only clip.
    /// </summary>
    private string Truncate(string text, float maxWidth)
    {
        if (GetTextWidth(text) <= maxWidth)
            return text;

        var available = maxWidth - GetTextWidth(Ellipsis);
        var width = 0f;
        var builder = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            width += GetRuneWidth(rune);
            if (width > available)
                break;

            builder.Append(rune.ToString());
        }

        return builder.ToString().TrimEnd() + Ellipsis;
    }

    private float GetTextWidth(string text)
    {
        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            width += GetRuneWidth(rune);
        }

        return width;
    }

    private float GetRuneWidth(Rune rune)
    {
        return _nameFont.TryGetCharMetrics(rune, 1f, out var metrics) ? metrics.Advance : 0f;
    }

    /// <summary>
    /// The starting solution shown by the item sprite, or the first one if the sprite does not pick.
    /// </summary>
    private Solution? GetContents(EntityPrototype prototype)
    {
        if (!prototype.TryComp<SolutionContainerManagerComponent>(out var manager, _componentFactory)
            || manager.Solutions is not { Count: > 0 } solutions)
            return null;

        if (prototype.TryComp<SolutionContainerVisualsComponent>(out var visuals, _componentFactory)
            && visuals.SolutionName is { } name
            && solutions.TryGetValue(name, out var shown))
            return shown;

        return solutions.Values.First();
    }

    /// <summary>
    /// The preview entity is spawned on the client without its solutions, so its fill layer is set by hand.
    /// The view respawns the preview whenever it enters the tree, hence the check every frame.
    /// </summary>
    private void UpdateContents()
    {
        if (_contents == null
            || ItemSprite.Entity is not { } preview
            || preview.Owner == _contentsShownOn)
            return;

        _contentsShownOn = preview.Owner;

        var appearanceSystem = _entMan.System<SharedAppearanceSystem>();
        var appearance = _entMan.EnsureComponent<AppearanceComponent>(preview.Owner);
        appearanceSystem.SetData(preview.Owner, SolutionContainerVisuals.FillFraction, _contents.FillFraction, appearance);
        appearanceSystem.SetData(preview.Owner, SolutionContainerVisuals.Color, _contents.GetColor(_protoManager), appearance);
    }

    private void OnSlotKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        Pressed?.Invoke();
    }
}
