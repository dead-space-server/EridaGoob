// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Erida.Stylesheets;

public static class EridaStyleClass
{
    public const string ScreenBase = "EridaScreenBase";
    public const string ScreenFrame = "EridaScreenFrame";
    public const string ScreenCorners = "EridaScreenCorners";

    public const string Panel = "EridaPanel";
    public const string Section = "EridaSection";
    public const string Card = "EridaCard";
    public const string BarGroup = "EridaBarGroup";
    public const string BarCorner = "EridaBarCorner";
    /// <summary>
    /// Toggled bar corner, its content sits on secondary container like end-4's toggled bar buttons.
    /// </summary>
    public const string BarCornerActive = "EridaBarCornerActive";
    public const string Divider = "EridaDivider";
    public const string Popup = "EridaPopup";
    public const string Window = "EridaWindow";

    public const string Title = "EridaTitle";
    public const string TextMuted = "EridaTextMuted";
    public const string TextValue = "EridaTextValue";
    public const string BarCaption = "EridaBarCaption";
    public const string BarTitle = "EridaBarTitle";
    public const string BarText = "EridaBarText";

    public const string Icon = "EridaIcon";
    public const string BarIcon = "EridaBarIcon";
    public const string BarLogo = "EridaBarLogo";

    public const string ButtonTonal = "EridaButtonTonal";
    public const string ButtonFilled = "EridaButtonFilled";
    public const string ButtonGhost = "EridaButtonGhost";
    public const string ButtonBar = "EridaButtonBar";
    public const string ButtonBarAccent = "EridaButtonBarAccent";
    public const string ButtonListItem = "EridaButtonListItem";
    public const string ButtonChip = "EridaButtonChip";

    /// <summary>
    /// Red accent for <see cref="Title"/>, <see cref="Icon"/>, <see cref="Card"/> and <see cref="ButtonFilled"/>.
    /// </summary>
    public const string AccentError = "EridaAccentError";
    /// <summary>
    /// Blue accent, see <see cref="AccentError"/>.
    /// </summary>
    public const string AccentInfo = "EridaAccentInfo";

    public const string ProgressTrack = "EridaProgressTrack";
    public const string ProgressFill = "EridaProgressFill";

    public const string ChatInput = "EridaChatInput";
    public const string ChatLineEdit = "EridaChatLineEdit";
    public const string ChatChip = "EridaChatChip";
    public const string ChatIconButton = "EridaChatIconButton";

    /// <summary>
    /// Vanilla <see cref="Robust.Client.UserInterface.Controls.PanelContainer"/> drawn as a section,
    /// only styled under <see cref="EridaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaSection = "EridaVanillaSection";

    /// <summary>
    /// Vanilla panel drawn as a list header card, only styled under <see cref="EridaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaHeader = "EridaVanillaHeader";

    /// <summary>
    /// Vanilla panel drawn as a card inside a section or tab, only styled under <see cref="EridaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaCard = "EridaVanillaCard";

    /// <summary>
    /// <see cref="VanillaCard"/> with tight paddings, for cards laid out in a grid.
    /// </summary>
    public const string VanillaCompactCard = "EridaVanillaCompactCard";

    /// <summary>
    /// Vanilla panel drawn as a raised block inside a card, only styled under <see cref="EridaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaInset = "EridaVanillaInset";

    /// <summary>
    /// Vanilla heading label recolored to the accent, only styled under <see cref="EridaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaHeading = "EridaVanillaHeading";
}
