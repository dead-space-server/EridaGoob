// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Inventory.Controls;

public sealed class InventoryDisplay : LayoutContainer
{
    // Erida start
    private float _columns;
    private float _rows;
    // Erida end
    private const int MarginThickness = 10;
    private const int ButtonSpacing = 5;
    private const int ButtonSize = 75;
    private readonly Control resizer;

    // Erida start
    public int CellSize { get; set; } = ButtonSize;
    public int CellSpacing { get; set; } = ButtonSpacing;
    public bool InvertY { get; set; }

    private readonly Dictionary<string, (SlotControl Button, Vector2 Offset, float Scale)> _buttons = [];
    // Erida end

    public InventoryDisplay()
    {
        resizer = new Control();
        AddChild(resizer);
    }

    public SlotControl AddButton(SlotControl newButton, Vector2 buttonOffset, float scale = 1f) // Erida edit
    {
        AddChild(newButton);
        HorizontalExpand = true;
        VerticalExpand = true;
        InheritChildMeasure = true;
        // Erida start
        if (!_buttons.TryAdd(newButton.SlotName, (newButton, buttonOffset, scale)))
            Logger.Warning("Tried to add button without a slot!");
        UpdateSizeData(buttonOffset, scale);
        Relayout();
        // Erida end
        return newButton;
    }

    public SlotControl? GetButton(string slotName)
    {
        return !_buttons.TryGetValue(slotName, out var foundButton) ? null : foundButton.Item1;
    }

    // Erida start
    private void UpdateSizeData(Vector2 buttonOffset, float scale)
    {
        _columns = MathF.Max(_columns, buttonOffset.X + scale - 1);
        _rows = MathF.Max(_rows, buttonOffset.Y + scale - 1);
        resizer.SetHeight = (_rows + 1) * (CellSize + CellSpacing);
        resizer.SetWidth = (_columns + 1) * (CellSize + CellSpacing);
    }

    public void Relayout()
    {
        foreach (var (button, offset, scale) in _buttons.Values)
        {
            var y = InvertY ? _rows + 1 - offset.Y - scale : offset.Y;
            SetPosition(button, new Vector2(offset.X, y) * CellSize + new Vector2(CellSpacing, CellSpacing));
        }
    }
    // Erida end

    public bool TryGetButton(string slotName, out SlotControl? button)
    {
        var success = _buttons.TryGetValue(slotName, out var buttonData);
        button = buttonData.Item1;
        return success;
    }

    public void RemoveButton(string slotName)
    {
        // Erida start
        if (!_buttons.Remove(slotName, out var removed))
            return;

        RemoveChild(removed.Button);
        //recalculate the size of the control when a slot is removed
        _columns = 0;
        _rows = 0;
        foreach (var (_, offset, scale) in _buttons.Values)
        {
            UpdateSizeData(offset, scale);
        }

        Relayout();
        // Erida end
    }

    public void ClearButtons()
    {
        // Erida start
        foreach (var (button, _, _) in _buttons.Values)
        {
            RemoveChild(button);
        }

        _buttons.Clear();
        _columns = 0;
        _rows = 0;
        resizer.SetWidth = 0;
        resizer.SetHeight = 0;
        // Erida end
    }
}