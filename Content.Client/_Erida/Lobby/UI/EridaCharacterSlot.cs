// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Lobby;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.Lobby.UI;

/// <summary>
/// Character slot in <see cref="EridaCharacterSetup"/>: sprite, name and top job, with a two-step delete button.
/// </summary>
public sealed class EridaCharacterSlot : ShapedButton
{
    private readonly IEntityManager _entManager;
    private readonly ShapedButton _delete;
    private readonly EntityUid _previewDummy;

    public event Action? OnDeletePressed;

    public EridaCharacterSlot(
        IEntityManager entManager,
        IPrototypeManager prototypes,
        ButtonGroup group,
        HumanoidCharacterProfile profile,
        bool selected,
        Texture deleteIcon)
    {
        _entManager = entManager;
        AddStyleClass(EridaStyleClass.ButtonListItem);
        ToggleMode = true;
        Group = group;
        Pressed = selected;
        MinHeight = 64;

        Content.HorizontalAlignment = HAlignment.Stretch;
        Content.Margin = new Thickness(4, 0, 6, 0);
        Content.SeparationOverride = 8;

        _previewDummy = UserInterfaceManager.GetUIController<LobbyUIController>().LoadProfileEntity(profile, null, true);
        var view = new SpriteView
        {
            Scale = new Vector2(2, 2),
            SetSize = new Vector2(64, 64),
            OverrideDirection = Direction.South,
            VerticalAlignment = VAlignment.Center,
        };
        view.SetEntity(_previewDummy);

        var name = new Label
        {
            Text = profile.Name,
            ClipText = true,
        };
        name.AddStyleClass(EridaStyleClass.TextValue);

        var job = new Label
        {
            Text = GetJobName(prototypes, profile),
            ClipText = true,
        };
        job.AddStyleClass(EridaStyleClass.TextMuted);

        var text = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            Children = { name, job },
        };

        _delete = new ShapedButton
        {
            Icon = deleteIcon,
            MinWidth = 28,
            VerticalAlignment = VAlignment.Center,
            Visible = !selected,
            ToolTip = Loc.GetString("character-setup-gui-character-picker-button-delete-button"),
        };
        _delete.AddStyleClass(EridaStyleClass.ButtonGhost);
        _delete.OnPressed += _ => OnDeletePress();
        // The slot doesn't get MouseExited when the cursor leaves straight from the delete button.
        _delete.OnMouseExited += _ => ResetDelete();

        Content.AddChild(view);
        Content.AddChild(text);
        Content.AddChild(_delete);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _entManager.DeleteEntity(_previewDummy);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _entManager.DeleteEntity(_previewDummy);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        ResetDelete();
    }

    private void OnDeletePress()
    {
        if (_delete.HasStyleClass(StyleClass.Negative))
        {
            OnDeletePressed?.Invoke();
            return;
        }

        _delete.RemoveStyleClass(EridaStyleClass.ButtonGhost);
        _delete.AddStyleClass(EridaStyleClass.ButtonTonal);
        _delete.AddStyleClass(StyleClass.Negative);
        _delete.Text = Loc.GetString("character-setup-gui-character-picker-button-confirm-delete-button");
    }

    private void ResetDelete()
    {
        if (!_delete.HasStyleClass(StyleClass.Negative))
            return;

        _delete.RemoveStyleClass(StyleClass.Negative);
        _delete.RemoveStyleClass(EridaStyleClass.ButtonTonal);
        _delete.AddStyleClass(EridaStyleClass.ButtonGhost);
        _delete.Text = null;
    }

    private static string GetJobName(IPrototypeManager prototypes, HumanoidCharacterProfile profile)
    {
        var job = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;

        if (job != default && prototypes.TryIndex(job, out var proto))
            return proto.LocalizedName;

        return Loc.GetString("erida-character-setup-no-job");
    }
}
