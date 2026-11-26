// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Research;

namespace Content.Goobstation.Client.Research.UI;

public static class ResearchUiPalette
{
    public static readonly Color PanelBackground = Color.FromHex("#181A20");
    public static readonly Color CardBackground = Color.FromHex("#1F2129");
    public static readonly Color CardBorder = Color.FromHex("#2E313C");
    public static readonly Color SelectedBorder = Color.FromHex("#E0A85B");
    public static readonly Color SearchMatchBorder = Color.FromHex("#61AFEF");
    public static readonly Color HeaderAccent = Color.FromHex("#E0A85B");
    public static readonly Color CanvasGridLine = Color.FromHex("#1C1F27");

    public static readonly Color TextPrimary = Color.FromHex("#E6E8EE");
    public static readonly Color TextSecondary = Color.FromHex("#9BA1B0");
    public static readonly Color TextDimmed = Color.FromHex("#6B707D");

    public static (Color Accent, Color Bg, Color Hover, Color Border) GetAvailabilityColors(ResearchAvailability availability)
    {
        return availability switch
        {
            ResearchAvailability.Researched => (
                Color.FromHex("#59A869"),
                Color.FromHex("#1B261F"),
                Color.FromHex("#233328"),
                Color.FromHex("#35593E")
            ),
            ResearchAvailability.Available => (
                Color.FromHex("#D4A74A"),
                Color.FromHex("#262319"),
                Color.FromHex("#332F20"),
                Color.FromHex("#6E5A2B")
            ),
            ResearchAvailability.PrereqsMet => (
                Color.FromHex("#5688C7"),
                Color.FromHex("#1A212B"),
                Color.FromHex("#222B39"),
                Color.FromHex("#344B6B")
            ),
            _ => (
                Color.FromHex("#7A4248"),
                Color.FromHex("#1B1C21"),
                Color.FromHex("#24252C"),
                Color.FromHex("#2B2D36")
            )
        };
    }

    public static Color GetLineColor(ResearchAvailability prereqAvailability, bool highlighted)
    {
        var baseColor = prereqAvailability switch
        {
            ResearchAvailability.Researched => Color.FromHex("#59A869"),
            ResearchAvailability.Available => Color.FromHex("#D4A74A"),
            ResearchAvailability.PrereqsMet => Color.FromHex("#5688C7"),
            _ => Color.FromHex("#4A4F5E")
        };

        return baseColor.WithAlpha(highlighted ? 0.95f : 0.45f);
    }
}
