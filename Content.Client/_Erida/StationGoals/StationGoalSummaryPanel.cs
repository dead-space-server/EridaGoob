// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Message;
using Content.Shared._Erida.StationGoals;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Erida.StationGoals;

/// <summary>
/// Station goal results at the top of the round end player manifest.
/// </summary>
public sealed class StationGoalSummaryPanel : PanelContainer
{
    /// <summary>
    /// Same background as the player panels below, StyleNano.StyleClassBackgroundBaseDark is obsolete.
    /// </summary>
    private const string BackgroundStyleClass = "PanelBackgroundBaseDark";

    public StationGoalSummaryPanel(IReadOnlyList<StationGoalResult> results)
    {
        StyleClasses.Add(BackgroundStyleClass);
        Margin = new Thickness(0, 0, 0, 6);

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(6, 4),
            SeparationOverride = 2,
        };

        var header = new RichTextLabel();
        header.SetMarkup(Loc.GetString("station-goal-round-end-header", ("count", results.Count)));
        box.AddChild(header);

        foreach (var result in results)
        {
            var line = new RichTextLabel();
            line.SetMarkup(result.Format(results.Count > 1));
            box.AddChild(line);
        }

        AddChild(box);
    }
}
