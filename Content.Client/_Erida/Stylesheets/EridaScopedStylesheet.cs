// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.UserInterface;
using Content.Client._Erida.UserInterface.Shapes;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Erida.Stylesheets;

/// <summary>
/// Interface stylesheet with vanilla buttons, fields, tabs, sliders and lists recolored from the Erida palette.
/// Set it as <see cref="Control.Stylesheet"/> of a subtree that hosts upstream controls, such as the profile editor,
/// so they match Erida without touching the rest of the game.
/// </summary>
public static class EridaScopedStylesheet
{
    private const string OptionTriangle = OptionButton.StyleClassOptionTriangle;
    private const float OptionTriangleSize = 16f;

    /// <summary>
    /// The paddings here leave about 24px for text in a spin box's 40px field, and its buttons already take about 60px.
    /// Spin boxes that aren't stretched stay at this width, and the field gets all of it past the buttons.
    /// </summary>
    private const float SpinBoxMinWidth = 120f;

    private static readonly string[] ButtonPseudos =
    [
        ContainerButton.StylePseudoClassNormal,
        ContainerButton.StylePseudoClassHover,
        ContainerButton.StylePseudoClassPressed,
        ContainerButton.StylePseudoClassDisabled,
    ];

    /// <summary>
    /// Vanilla button variants and whether their left and right ends stay rounded.
    /// </summary>
    private static readonly (string? StyleClass, bool Left, bool Right)[] ButtonVariants =
    [
        (null, true, true),
        (StyleClass.ButtonOpenLeft, false, true),
        (StyleClass.ButtonOpenRight, true, false),
        (StyleClass.ButtonOpenBoth, false, false),
        (StyleClass.ButtonSquare, false, false),
    ];

    /// <summary>
    /// Builds a new sheet on every call, use <see cref="EridaThemeManager.GetScopedStylesheet"/> for the cached one.
    /// </summary>
    public static Stylesheet Create(Stylesheet baseSheet, EridaPalette p)
    {
        var rules = new List<StyleRule>(baseSheet.Rules);
        AddButtonRules(rules, p);
        AddFieldRules(rules, p);
        AddContainerRules(rules, p);
        AddOptionPopupRules(rules, p);
        AddTraitsRules(rules, p);

        return new Stylesheet(rules);
    }

    private static void AddButtonRules(List<StyleRule> rules, EridaPalette p)
    {
        foreach (var (variant, left, right) in ButtonVariants)
        {
            var shape = ControlShape(left, right);

            foreach (var pseudo in ButtonPseudos)
            {
                var (fill, text) = ButtonColors(pseudo, p.SecondaryContainer, p.SecondaryContainerHover,
                    p.Primary, p.OnSecondaryContainer, p.OnPrimary);

                AddButtonState(rules, variant, pseudo, fill, text, shape);
            }
        }

        foreach (var pseudo in ButtonPseudos)
        {
            var (negativeFill, negativeText) = ButtonColors(pseudo, p.ErrorContainer, p.ErrorContainerHover,
                p.ErrorContainerActive, p.OnErrorContainer, p.OnErrorContainer);
            AddButtonState(rules, StyleClass.Negative, pseudo, negativeFill, negativeText, EridaShapes.Control);

            var (positiveFill, positiveText) = ButtonColors(pseudo, p.PrimaryContainer, p.PrimaryContainerHover,
                p.PrimaryContainerActive, p.OnPrimaryContainer, p.OnPrimaryContainer);
            AddButtonState(rules, StyleClass.Positive, pseudo, positiveFill, positiveText, EridaShapes.Control);
        }

        rules.Add(E<TextureRect>().Class(OptionTriangle)
            .Prop(TextureRect.StylePropertyTexture, EridaIcons.Get("expand_more"))
            .Prop(TextureRect.StylePropertyTextureSizeTarget, new Vector2(OptionTriangleSize, OptionTriangleSize))
            .Prop(TextureRect.StylePropertyTextureStretch, TextureRect.StretchMode.KeepAspectCentered));

        rules.Add(E<TextureButton>().PseudoNormal().Modulate(p.OnSurfaceVariant));
        rules.Add(E<TextureButton>().PseudoHovered().Modulate(p.OnSurface));
        rules.Add(E<TextureButton>().PseudoPressed().Modulate(p.Primary));
    }

    private static (Color Fill, Color Text) ButtonColors(
        string pseudo,
        Color idle,
        Color hover,
        Color pressed,
        Color text,
        Color pressedText)
    {
        return pseudo switch
        {
            ContainerButton.StylePseudoClassHover => (hover, text),
            ContainerButton.StylePseudoClassPressed => (pressed, pressedText),
            ContainerButton.StylePseudoClassDisabled => (idle.WithAlpha(idle.A * 0.38f), text.WithAlpha(0.38f)),
            _ => (idle, text),
        };
    }

    private static void AddButtonState(
        List<StyleRule> rules,
        string? variant,
        string pseudo,
        Color fill,
        Color text,
        ShapeStyle shape)
    {
        rules.Add(Button(variant, pseudo)
            .Box(Box(fill, shape, 12, 3))
            .Prop(Control.StylePropertyModulateSelf, Color.White));

        rules.Add(Button(variant, pseudo).ParentOf(E<Label>()).FontColor(text));
        rules.Add(Button(variant, pseudo).ParentOf(E()).ParentOf(E<Label>()).FontColor(text));
        rules.Add(Button(variant, pseudo).ParentOf(E()).ParentOf(E<TextureRect>().Class(OptionTriangle))
            .Prop(Control.StylePropertyModulateSelf, text));
    }

    private static MutableSelectorElement Button(string? variant, string pseudo)
    {
        return E<ContainerButton>()
            .Class(ContainerButton.StyleClassButton)
            .MaybeClass(variant)
            .Pseudo(pseudo);
    }

    private static void AddFieldRules(List<StyleRule> rules, EridaPalette p)
    {
        var lineEdit = Box(p.Layer2, EridaShapes.Control, 8, 3);
        lineEdit.BorderColor = p.OutlineVariant;
        lineEdit.BorderThickness = 1f;

        rules.Add(E<LineEdit>()
            .Prop(LineEdit.StylePropertyStyleBox, lineEdit)
            .Prop(LineEdit.StylePropertyCursorColor, p.Primary)
            .Prop(LineEdit.StylePropertySelectionColor, p.Primary.WithAlpha(0.35f))
            .FontColor(p.OnSurface));
        rules.Add(E<SpinBox>().MinWidth(SpinBoxMinWidth));
        rules.Add(E<LineEdit>().Class(LineEdit.StyleClassLineEditNotEditable).FontColor(p.OnSurfaceVariant));
        rules.Add(E<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder).FontColor(p.Outline));
        rules.Add(E<TextEdit>().Pseudo(TextEdit.StylePseudoClassPlaceholder).FontColor(p.Outline));

        var track = new Thickness(0, 5);
        var grabber = Box(p.Primary, ShapeStyle.Pill, 8, 8);

        rules.Add(E<Slider>()
            .Prop(Slider.StylePropertyBackground, new ShapedStyleBox
            {
                BackgroundColor = p.SurfaceContainerHighest,
                Shape = ShapeStyle.Pill,
                Inset = track,
            })
            .Prop(Slider.StylePropertyFill, new ShapedStyleBox
            {
                BackgroundColor = p.Primary,
                Shape = ShapeStyle.Pill,
                Inset = track,
            })
            .Prop(Slider.StylePropertyForeground, new StyleBoxEmpty())
            .Prop(Slider.StylePropertyGrabber, grabber));
    }

    private static void AddContainerRules(List<StyleRule> rules, EridaPalette p)
    {
        rules.Add(E<Label>().FontColor(p.OnSurface));

        rules.Add(E<TabContainer>()
            .Prop(TabContainer.StylePropertyPanelStyleBox, Box(p.Layer1, EridaShapes.Section, 10, 10))
            .Prop(TabContainer.StylePropertyTabStyleBox, Box(p.SecondaryContainer, ShapeStyle.Pill, 14, 4))
            .Prop(TabContainer.StylePropertyTabStyleBoxInactive, Box(Color.Transparent, ShapeStyle.Pill, 14, 4))
            .Prop(TabContainer.stylePropertyTabFontColor, p.OnSecondaryContainer)
            .Prop(TabContainer.StylePropertyTabFontColorInactive, p.OnLayer1Inactive));

        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaSection)
            .Panel(Box(p.Layer1, EridaShapes.Section, 10, 10)));

        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaHeader)
            .Panel(Box(p.Layer2, EridaShapes.Card, 10, 5)));

        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaCard)
            .Panel(Box(p.Layer2, EridaShapes.Card, 12, 10)));
        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaHeader).ParentOf(E<Label>())
            .FontColor(p.Primary));

        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaCompactCard)
            .Panel(Box(p.Layer2, EridaShapes.Card, 4, 4)));
        rules.Add(E<PanelContainer>().Class(EridaStyleClass.VanillaInset)
            .Panel(Box(p.Layer3, EridaShapes.Control, 6, 6)));
        rules.Add(E<Label>().Class(EridaStyleClass.VanillaHeading).FontColor(p.Primary));

        rules.Add(E<ItemList>()
            .Prop(ItemList.StylePropertyBackground, Box(p.Layer2, EridaShapes.Card, 4, 4))
            .Prop(ItemList.StylePropertyItemBackground, Box(Color.Transparent, EridaShapes.Control, 6, 2))
            .Prop(ItemList.StylePropertySelectedItemBackground, Box(p.SecondaryContainer, EridaShapes.Control, 6, 2))
            .Prop(ItemList.StylePropertyDisabledItemBackground, Box(Color.Transparent, EridaShapes.Control, 6, 2))
            .FontColor(p.OnSurface));

        AddScrollBarRules<VScrollBar>(rules, p, new Thickness(3, 2));
        AddScrollBarRules<HScrollBar>(rules, p, new Thickness(2, 3));
    }

    private static void AddOptionPopupRules(List<StyleRule> rules, EridaPalette p)
    {
        var popup = E<Popup>().Class(OptionButton.StyleClassPopup);
        var background = new ShapedStyleBox
        {
            BackgroundColor = p.SurfaceContainer,
            BorderColor = p.Layer0Border,
            BorderThickness = 1f,
            Shape = EridaShapes.Card,
        };

        rules.Add(popup.ParentOf(E<PanelContainer>().Class(OptionButton.StyleClassOptionsBackground))
            .Panel(background));

        (string Pseudo, Color Fill, Color Text)[] states =
        [
            (ContainerButton.StylePseudoClassNormal, Color.Transparent, p.OnSurface),
            (ContainerButton.StylePseudoClassHover, p.Layer2Hover, p.OnSurface),
            (ContainerButton.StylePseudoClassPressed, p.SecondaryContainer, p.OnSecondaryContainer),
            (ContainerButton.StylePseudoClassDisabled, Color.Transparent, p.OnSurface.WithAlpha(0.38f)),
        ];

        foreach (var (pseudo, fill, text) in states)
        {
            var item = popup
                .ParentOf(E<ScrollContainer>())
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(pseudo));

            rules.Add(item.Box(Box(fill, EridaShapes.Control, 10, 3))
                .Prop(Control.StylePropertyModulateSelf, Color.White));
            rules.Add(item.ParentOf(E<Label>()).FontColor(text));
        }
    }

    /// <summary>
    /// The traits tab paints fixed colors from its own sheetlet, these rules take its classes over.
    /// </summary>
    private static void AddTraitsRules(List<StyleRule> rules, EridaPalette p)
    {
        rules.Add(E<PanelContainer>().Class("TraitsHeaderPanel").Panel(Box(p.Layer2, EridaShapes.Card, 0, 0)));
        rules.Add(E<PanelContainer>().Class("TraitsSearchBar").Panel(Box(p.Layer2, EridaShapes.Card, 0, 0)));
        rules.Add(E<PanelContainer>().Class("TraitsFooterPanel").Panel(Box(p.Layer2, EridaShapes.Card, 0, 0)));
        rules.Add(E<LineEdit>().Class("TraitsSearchInput")
            .Prop(LineEdit.StylePropertyStyleBox, Box(p.Layer3, EridaShapes.Control, 8, 3)));

        rules.Add(E<Label>().Class("TraitsTitleLabel").FontColor(p.OnLayer2));
        rules.Add(E<Label>().Class("TraitsSubtitleLabel").FontColor(p.OnSurfaceVariant));
        rules.Add(E<Label>().Class("TraitsStatLabel").FontColor(p.Primary));
        rules.Add(E<Label>().Class("TraitsFooterText").FontColor(p.Outline));

        rules.Add(E<PanelContainer>().Class("TraitsProgressBarBg")
            .Panel(new ShapedStyleBox { BackgroundColor = p.SurfaceContainerHighest, Shape = ShapeStyle.Pill }));

        (string StyleClass, Color Color)[] fills =
        [
            ("TraitsProgressBarFill", p.Primary),
            ("TraitsProgressBarFull", p.Primary),
            ("TraitsProgressBarPartial", p.Tertiary),
            ("TraitsProgressBarLow", p.Error),
            ("TraitsProgressBarEmpty", Color.Transparent),
        ];

        foreach (var (styleClass, color) in fills)
        {
            rules.Add(E<PanelContainer>().Class(styleClass)
                .Panel(new ShapedStyleBox { BackgroundColor = color, Shape = ShapeStyle.Pill }));
        }

        rules.Add(E<PanelContainer>().Class("TraitsCategoryHeader").Panel(Box(p.Layer2, EridaShapes.Control, 0, 0)));
        rules.Add(E<PanelContainer>().Class("TraitsCategoryContent").Panel(Box(p.Layer1, EridaShapes.Control, 0, 0)));

        (string Pseudo, Color Fill)[] headerStates =
        [
            (ContainerButton.StylePseudoClassNormal, Color.Transparent),
            (ContainerButton.StylePseudoClassHover, p.Layer2Hover),
            (ContainerButton.StylePseudoClassPressed, p.Layer2Active),
            (ContainerButton.StylePseudoClassDisabled, Color.Transparent),
        ];

        foreach (var (pseudo, fill) in headerStates)
        {
            rules.Add(TraitsHeader().Pseudo(pseudo).Box(Box(fill, EridaShapes.Control, 0, 0)));
        }

        (string StyleClass, Color Color)[] headerLabels =
        [
            ("TraitsCategoryNameLabel", p.Primary),
            ("TraitsCategoryExpandIcon", p.OnSurfaceVariant),
            ("TraitsCategoryStatsLabel", p.OnSurfaceVariant),
            ("TraitsCategoryPointsLabel", p.Outline),
        ];

        foreach (var (styleClass, color) in headerLabels)
        {
            rules.Add(TraitsHeader().ParentOf(E()).ParentOf(E<Label>().Class(styleClass)).FontColor(color));
        }

        var entry = Box(p.Layer2, EridaShapes.Control, 0, 0);
        entry.BorderColor = p.OutlineVariant;
        entry.BorderThickness = 1f;

        var selected = Box(p.SecondaryContainer, EridaShapes.Control, 0, 0);
        selected.BorderColor = p.Primary;
        selected.BorderThickness = 1f;

        rules.Add(E<PanelContainer>().Class("TraitsEntryPanel").Panel(entry));
        rules.Add(E<PanelContainer>().Class("TraitsEntryPanel").Class("TraitsEntrySelected").Panel(selected));
        rules.Add(E<PanelContainer>().Class("TraitsEntryPanel").Class("TraitsEntryDisabled")
            .Panel(Box(p.Layer2Disabled, EridaShapes.Control, 0, 0)));
        rules.Add(E<Label>().Class("TraitsEntryNameLabel").FontColor(p.OnLayer2));
        rules.Add(E<RichTextLabel>().Class("TraitsEntryDescriptionLabel").FontColor(p.OnSurfaceVariant));
    }

    private static MutableSelectorElement TraitsHeader()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class("TraitsCategoryHeaderButton");
    }

    private static void AddScrollBarRules<T>(List<StyleRule> rules, EridaPalette p, Thickness inset)
        where T : ScrollBar
    {
        rules.Add(E<T>().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.OutlineVariant, inset)));
        rules.Add(E<T>().PseudoHovered().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.Outline, inset)));
        rules.Add(E<T>().PseudoPressed().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.OnSurfaceVariant, inset)));
    }

    private static ShapedStyleBox Grabber(Color color, Thickness inset)
    {
        var box = new ShapedStyleBox
        {
            BackgroundColor = color,
            Shape = ShapeStyle.Pill,
            Inset = inset,
        };

        box.SetContentMarginOverride(StyleBox.Margin.Left, 10);
        box.SetContentMarginOverride(StyleBox.Margin.Top, 10);
        return box;
    }

    private static ShapeStyle ControlShape(bool left, bool right)
    {
        var shape = EridaShapes.Control;

        return new ShapeStyle(
            left ? shape.TopLeft : Corner.Square,
            right ? shape.TopRight : Corner.Square,
            right ? shape.BottomRight : Corner.Square,
            left ? shape.BottomLeft : Corner.Square);
    }

    private static ShapedStyleBox Box(Color color, ShapeStyle shape, float horizontal, float vertical)
    {
        var box = new ShapedStyleBox
        {
            BackgroundColor = color,
            Shape = shape,
        };

        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, horizontal);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, vertical);
        return box;
    }
}
