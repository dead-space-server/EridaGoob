// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Erida.UserInterface.Controls;

/// <summary>
/// Window drawn as a Erida panel with a title row and a close button. Children go into <see cref="Contents"/>.
/// </summary>
[Virtual]
public class EridaWindow : BaseWindow
{
    private const float HeaderHeight = 44f;
    private const float DragMargin = 7f;

    private readonly Label _title;

    /// <summary>
    /// Body of the window under the title row.
    /// </summary>
    public BoxContainer Contents { get; }

    public string? Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public EridaWindow()
    {
        MouseFilter = MouseFilterMode.Stop;

        var panel = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        panel.AddStyleClass(EridaStyleClass.Window);

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            MinHeight = HeaderHeight,
            Margin = new Thickness(18, 0, 8, 0),
        };

        _title = new Label
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            ClipText = true,
        };
        _title.AddStyleClass(EridaStyleClass.Title);

        var close = new ShapedButton
        {
            MinWidth = 28,
            VerticalAlignment = VAlignment.Center,
            Icon = EridaIcons.Get("close"),
        };
        close.AddStyleClass(EridaStyleClass.ButtonBar);
        close.OnPressed += _ => Close();

        header.AddChild(_title);
        header.AddChild(close);

        Contents = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Margin = new Thickness(12, 0, 12, 12),
            SeparationOverride = 8,
        };

        panel.AddChild(header);
        panel.AddChild(Contents);
        AddChild(panel);
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        var mode = DragMode.None;

        if (Resizable)
        {
            if (relativeMousePos.Y < DragMargin)
                mode = DragMode.Top;
            else if (relativeMousePos.Y > Size.Y - DragMargin)
                mode = DragMode.Bottom;

            if (relativeMousePos.X < DragMargin)
                mode |= DragMode.Left;
            else if (relativeMousePos.X > Size.X - DragMargin)
                mode |= DragMode.Right;
        }

        if (mode == DragMode.None && relativeMousePos.Y < HeaderHeight)
            mode = DragMode.Move;

        return mode;
    }

    protected static ShapedButton CreateButton(string text, string? icon, string styleClass)
    {
        var button = new ShapedButton
        {
            Text = Loc.GetString(text),
            HorizontalExpand = true,
            MinHeight = 34,
        };

        if (icon != null)
            button.Icon = EridaIcons.Get(icon);

        button.AddStyleClass(styleClass);
        return button;
    }
}
