// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Goobstation.Client.Research.UI;

// Erida start
public sealed partial class ResearchesContainerPanel : LayoutContainer
{
    public Vector2 PanOffset;
    public float Zoom = 1f;
    public readonly List<(Color Color, float TopY, float BottomY)> Swimlanes = new();

    private readonly Dictionary<string, FancyResearchConsoleItem> _itemsById = new();
    private readonly List<(Vector2 Start, Vector2 End, Color Color)> _highlightedEdges = new();

    protected override void Draw(DrawingHandleScreen handle)
    {
        DrawGrid(handle);
        DrawSwimlanes(handle);

        _itemsById.Clear();
        _highlightedEdges.Clear();

        foreach (var child in Children)
        {
            if (child is FancyResearchConsoleItem item)
                _itemsById[item.Prototype.ID] = item;
        }

        foreach (var item in _itemsById.Values)
        {
            if (item.Prototype.TechnologyPrerequisites.Count == 0)
                continue;

            foreach (var prereqId in item.Prototype.TechnologyPrerequisites)
            {
                if (!_itemsById.TryGetValue(prereqId, out var prereq))
                    continue;

                var start = new Vector2(
                    prereq.PixelPosition.X + prereq.PixelWidth,
                    prereq.PixelPosition.Y + prereq.PixelHeight * 0.5f);
                var end = new Vector2(
                    item.PixelPosition.X,
                    item.PixelPosition.Y + item.PixelHeight * 0.5f);

                var crossDisc = item.Prototype.Discipline != prereq.Prototype.Discipline;
                var isDimmed = item.IsSearchDimmed || prereq.IsSearchDimmed;
                var isHighlighted = !isDimmed && (item.IsHovered || item.IsSelected || prereq.IsHovered || prereq.IsSelected);
                var lineColor = ResearchUiPalette.GetLineColor(prereq.Availability, isHighlighted);

                if (isDimmed)
                    lineColor = lineColor.WithAlpha(0.08f);
                else if (crossDisc && !isHighlighted)
                    lineColor = lineColor.WithAlpha(0.18f);

                if (isHighlighted)
                {
                    _highlightedEdges.Add((start, end, lineColor));
                    continue;
                }

                DrawSteppedConnector(handle, start, end, lineColor, 1.5f);
            }
        }

        // Draw active paths last so they stay on top of normal edges.
        foreach (var (start, end, color) in _highlightedEdges)
        {
            DrawSteppedConnector(handle, start, end, color, 2.5f);
        }
    }

    private void DrawGrid(DrawingHandleScreen handle)
    {
        var step = Math.Max(16f, 40f * Zoom * UIScale);
        var offsetX = (PanOffset.X * UIScale) % step;
        var offsetY = (PanOffset.Y * UIScale) % step;

        if (offsetX < 0)
            offsetX += step;
        if (offsetY < 0)
            offsetY += step;

        for (var x = offsetX; x < PixelWidth; x += step)
            handle.DrawLine(new Vector2(x, 0), new Vector2(x, PixelHeight), ResearchUiPalette.CanvasGridLine);

        for (var y = offsetY; y < PixelHeight; y += step)
            handle.DrawLine(new Vector2(0, y), new Vector2(PixelWidth, y), ResearchUiPalette.CanvasGridLine);
    }

    private void DrawSwimlanes(DrawingHandleScreen handle)
    {
        foreach (var (color, topY, bottomY) in Swimlanes)
        {
            var yTop = (PanOffset.Y + topY) * UIScale;
            var yBottom = (PanOffset.Y + bottomY) * UIScale;

            handle.DrawRect(new UIBox2(0, yTop, PixelWidth, yBottom), color.WithAlpha(0.03f));
            handle.DrawLine(new Vector2(0, yTop), new Vector2(PixelWidth, yTop), color.WithAlpha(0.22f));
        }
    }

    private void DrawSteppedConnector(
        DrawingHandleScreen handle,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        if (end.X > start.X + 8f)
        {
            var midX = (start.X + end.X) * 0.5f;
            var p1 = new Vector2(midX, start.Y);
            var p2 = new Vector2(midX, end.Y);

            DrawThickLine(handle, start, p1, color, thickness);
            if (MathF.Abs(start.Y - end.Y) > 0.5f)
                DrawThickLine(handle, p1, p2, color, thickness);
            DrawThickLine(handle, p2, end, color, thickness);
            return;
        }

        // Route backward cross-discipline links around cards instead of drawing diagonal lines.
        var stub = 14f * Zoom * UIScale;
        var laneOffset = 34f * Zoom * UIScale;
        var midY = end.Y > start.Y ? start.Y + laneOffset : start.Y - laneOffset;

        var s1 = new Vector2(start.X + stub, start.Y);
        var s2 = new Vector2(start.X + stub, midY);
        var s3 = new Vector2(end.X - stub, midY);
        var s4 = new Vector2(end.X - stub, end.Y);

        DrawThickLine(handle, start, s1, color, thickness);
        DrawThickLine(handle, s1, s2, color, thickness);
        DrawThickLine(handle, s2, s3, color, thickness);
        DrawThickLine(handle, s3, s4, color, thickness);
        DrawThickLine(handle, s4, end, color, thickness);
    }

    private static void DrawThickLine(DrawingHandleScreen handle, Vector2 a, Vector2 b, Color color, float thickness)
    {
        if (thickness <= 1.5f)
        {
            handle.DrawLine(a, b, color);
            return;
        }

        var dir = b - a;
        var len = dir.Length();
        if (len < 0.001f)
            return;

        var perp = new Vector2(-dir.Y, dir.X) / len * (thickness * 0.35f);
        handle.DrawLine(a, b, color);
        handle.DrawLine(a + perp, b + perp, color);
        handle.DrawLine(a - perp, b - perp, color);
    }
}
// Erida end
