// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface;
using Content.Client._Erida.UserInterface.Controls;
using Content.Shared._Erida.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.Lobby.UI;

/// <summary>
/// Job title in the profile editor that opens a list of the job's alternate titles.
/// </summary>
public sealed class EridaAltTitlePicker : ShapedButton
{
    private const float PopupMaxHeight = 300f;

    private readonly string _jobName;
    private readonly IReadOnlyList<JobAlternateTitlePrototype> _titles;
    private readonly Popup _popup;
    private readonly BoxContainer _rows;

    private JobAlternateTitlePrototype? _selected;
    private Gender _gender;

    public event Action<ProtoId<JobAlternateTitlePrototype>?>? OnSelected;

    public EridaAltTitlePicker(string jobName, IReadOnlyList<JobAlternateTitlePrototype> titles)
    {
        _jobName = jobName;
        _titles = titles;

        AddStyleClass(EridaStyleClass.ButtonChip);
        Icon = EridaIcons.Get("expand_more");
        Content.GetChild(0).SetPositionLast();
        Content.HorizontalAlignment = HAlignment.Stretch;
        Label.HorizontalExpand = true;
        Label.ClipText = true;

        _rows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(6),
            SeparationOverride = 2,
        };

        var panel = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        panel.AddStyleClass(EridaStyleClass.Popup);
        panel.AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            ReturnMeasure = true,
            MaxHeight = PopupMaxHeight,
            Children = { _rows },
        });

        _popup = new Popup { Children = { panel } };
        _popup.OnPopupHide += _popup.Orphan;

        OnPressed += _ => OpenPopup();
    }

    /// <summary>
    /// Selects the title without raising <see cref="OnSelected"/>, unknown ids fall back to the job name.
    /// </summary>
    public void Select(ProtoId<JobAlternateTitlePrototype>? id)
    {
        _selected = null;
        foreach (var title in _titles)
        {
            if (id is { } value && title.ID == value.Id)
                _selected = title;
        }

        UpdateText();
    }

    public void SetGender(Gender gender)
    {
        _gender = gender;
        UpdateText();
    }

    private void UpdateText()
    {
        Text = _selected?.LocalizedName(_gender) ?? _jobName;
    }

    private void OpenPopup()
    {
        if (Root == null)
            return;

        _rows.RemoveAllChildren();
        AddRow(null, _jobName);
        foreach (var title in _titles)
        {
            AddRow(title, title.LocalizedName(_gender));
        }

        Root.ModalRoot.AddChild(_popup);
        _popup.Measure(Vector2Helpers.Infinity);

        var position = GlobalPosition + new Vector2(0, Size.Y + 2);
        var size = new Vector2(MathF.Max(Width, _popup.DesiredSize.X), _popup.DesiredSize.Y);
        _popup.Open(UIBox2.FromDimensions(position, size));
    }

    private void AddRow(JobAlternateTitlePrototype? title, string text)
    {
        var row = new ShapedButton
        {
            Text = text,
            HorizontalExpand = true,
            IsActive = title == _selected,
        };
        row.AddStyleClass(EridaStyleClass.ButtonListItem);
        row.Content.HorizontalAlignment = HAlignment.Left;
        row.Content.Margin = new Thickness(8, 0);

        row.OnPressed += _ =>
        {
            _popup.Close();

            if (title == _selected)
                return;

            _selected = title;
            UpdateText();
            OnSelected?.Invoke(title?.ID);
        };

        _rows.AddChild(row);
    }
}
