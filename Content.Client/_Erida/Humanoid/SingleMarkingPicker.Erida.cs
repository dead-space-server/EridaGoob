// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.Humanoid;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Stylesheets;
using Content.Shared.Humanoid.Markings;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed partial class SingleMarkingPicker
{
    /// <summary>
    ///     What happens if a marking's gradient is changed or swapped with its first color.
    ///     Sends a 'slot' number, and the marking with both its colors and its gradient.
    /// </summary>
    public Action<(int slot, Marking marking)>? OnGradientChanged;

    private const float TileWidth = 76f;
    private const float TileArtSize = 64f;
    private const float WideLayoutHeight = 300f;
    private const float WideLayoutColorsWidth = 380f;

    private static readonly UIBox2 HairRegion = new(0.25f, 0f, 0.75f, 0.5f);
    private static readonly UIBox2 FacialHairRegion = new(0.25f, 0.08f, 0.75f, 0.58f);
    private const int TileGap = 4;
    private const float ScrollBarWidth = 12f;

    private readonly List<(ItemList.Item Item, ShapedButton Tile)> _tiles = [];

    private MarkingGradientControl? _gradientControl;
    private GridContainer _tileGrid = default!;
    private bool _wideLayout;

    /// <summary>
    /// Adds the gradient editor under the color sliders. Windows that don't handle
    /// <see cref="OnGradientChanged"/>, such as the wizard's mirror, don't get it.
    /// </summary>
    private void AddGradientControl(Marking marking, MarkingPrototype proto)
    {
        EridaColorSliders.Restyle(ColorSelectorContainer);

        _gradientControl = null;
        if (proto.ForcedColoring || OnGradientChanged == null)
            return;

        var control = new MarkingGradientControl();
        control.SetGradient(marking.Gradient, MarkingGradientControl.Roots(marking));
        control.GradientChanged += gradient =>
        {
            marking.Gradient = gradient;
            OnGradientChanged?.Invoke((_slot, marking));
        };
        control.SwapPressed += () =>
        {
            if (!MarkingGradientControl.TrySwap(marking))
                return;

            OnGradientChanged?.Invoke((_slot, marking));
            PopulateColors();
        };

        ColorSelectorContainer.AddChild(control);
        _gradientControl = control;
    }

    /// <summary>
    /// Keeps the gradient's color suggestions in step with the color sliders, which don't rebuild the editor.
    /// </summary>
    private void UpdateGradientRoots(Marking marking)
    {
        _gradientControl?.SetRoots(MarkingGradientControl.Roots(marking));

        if (marking.MarkingColors.Count > 0)
            UpdateTileTint(marking.MarkingColors[0]);
    }

    /// <summary>
    /// Shows the markings as a grid of tiles with large sprites. The vanilla list stays hidden as the selection model,
    /// so the upstream code keeps working with it.
    /// </summary>
    private void InitializeMarkingTiles()
    {
        _tileGrid = new GridContainer
        {
            HorizontalExpand = true,
            HSeparationOverride = TileGap,
            VSeparationOverride = TileGap,
        };

        MarkingList.Visible = false;
        MarkingList.Parent?.AddChild(_tileGrid);
        MarkingList.OnItemSelected += _ => UpdateTileSelection();
        MarkingList.OnItemDeselected += _ => UpdateTileSelection();

        CategoryName.AddStyleClass(EridaStyleClass.TextMuted);
    }

    /// <summary>
    /// Puts the color sliders beside the tiles instead of under a tall list, so the style and its color are edited
    /// without scrolling. Only for wide hosts like the profile editor.
    /// </summary>
    public void UseWideLayout()
    {
        if (MarkingList.Parent is not ScrollContainer tiles || ColorSelectorContainer.Parent is not ScrollContainer colors)
            return;

        _wideLayout = true;
        MarkingSelectorContainer.Orientation = LayoutOrientation.Horizontal;
        MarkingSelectorContainer.SeparationOverride = TileGap * 3;

        tiles.MinHeight = 0;
        tiles.SetHeight = WideLayoutHeight;
        tiles.HScrollEnabled = false;

        colors.MinHeight = 0;
        colors.SetHeight = WideLayoutHeight;
        colors.SetWidth = WideLayoutColorsWidth;
        colors.HorizontalExpand = false;
        colors.HScrollEnabled = false;
    }

    protected override void Resized()
    {
        base.Resized();

        var width = _wideLayout ? Width - WideLayoutColorsWidth - TileGap * 3 : Width;
        _tileGrid.Columns = Math.Max(1, (int) ((width - ScrollBarWidth + TileGap) / (TileWidth + TileGap)));
    }

    /// <summary>
    /// Rebuilds the tiles with the current marking first, so the editor opens on it.
    /// The list is only rebuilt on new data or a search, never on a click, so the picked tile doesn't jump away.
    /// </summary>
    private void UpdateMarkingTiles()
    {
        _tileGrid.RemoveAllChildren();
        _tiles.Clear();

        var tint = CurrentMarkingColor();
        foreach (var item in MarkingList)
        {
            var tile = CreateTile(item, tint);
            _tiles.Add((item, tile));
            _tileGrid.AddChild(tile);

            if (item.Selected)
                tile.SetPositionFirst();
        }

        if (MarkingList.Parent is ScrollContainer scroll)
            scroll.SetScrollValue(Vector2.Zero);

        UpdateTileSelection();
    }

    private ShapedButton CreateTile(ItemList.Item item, Color tint)
    {
        var tile = new ShapedButton
        {
            ToolTip = item.Text,
            SetSize = new Vector2(TileWidth, TileWidth),
        };
        tile.AddStyleClass(EridaStyleClass.ButtonListItem);

        var art = new TextureRect
        {
            Texture = item.Icon is { } icon ? CropToHead(icon) : null,
            SetSize = new Vector2(TileArtSize, TileArtSize),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            ModulateSelfOverride = tint,
        };
        tile.Content.AddChild(art);

        tile.OnPressed += _ =>
        {
            if (!item.Selected)
                item.Selected = true;
        };

        return tile;
    }

    /// <summary>
    /// Marking sprites cover the whole body frame, the hair or beard only takes the top middle of it.
    /// RSI frames are already regions of an atlas and the renderer only unwraps one level,
    /// so the crop is taken from the atlas itself.
    /// </summary>
    private Texture CropToHead(Texture texture)
    {
        var region = Category == MarkingCategories.FacialHair ? FacialHairRegion : HairRegion;
        var size = (Vector2) texture.Size;
        var source = texture;
        var offset = Vector2.Zero;

        if (texture is AtlasTexture atlas)
        {
            source = atlas.SourceTexture;
            offset = atlas.SubRegion.TopLeft;
        }

        return new AtlasTexture(source, new UIBox2(
            offset + region.TopLeft * size,
            offset + region.BottomRight * size));
    }

    private Color CurrentMarkingColor()
    {
        if (_markings == null || Slot < 0 || _markings[Slot].MarkingColors.Count == 0)
            return Color.White;

        return _markings[Slot].MarkingColors[0];
    }

    private void UpdateTileTint(Color tint)
    {
        foreach (var (_, tile) in _tiles)
        {
            if (tile.Content.ChildCount > 0 && tile.Content.GetChild(tile.Content.ChildCount - 1) is TextureRect art)
                art.ModulateSelfOverride = tint;
        }
    }

    private void UpdateTileSelection()
    {
        foreach (var (item, tile) in _tiles)
        {
            tile.IsActive = item.Selected;
        }
    }

    /// <summary>
    /// Shows only the slot controls that do something: the slot list with more than one slot, adding while there is
    /// room and removing while there is a slot. Vanilla keeps them all, dimmed or stretched over the empty space.
    /// </summary>
    private void UpdateSlotButtons()
    {
        SlotSelectorContainer.SeparationOverride = TileGap;

        Control[] slotControls = [SlotSelector, AddButton, RemoveButton];
        foreach (var control in slotControls)
        {
            control.RemoveStyleClass(StyleClass.ButtonOpenLeft);
            control.RemoveStyleClass(StyleClass.ButtonOpenRight);
            control.RemoveStyleClass(StyleClass.ButtonOpenBoth);
        }

        SlotSelector.Visible = PointsUsed > 1;
        AddButton.Visible = !AddButton.Disabled;
        RemoveButton.Visible = !RemoveButton.Disabled;
        AddButton.HorizontalExpand = false;
        RemoveButton.HorizontalExpand = false;
    }
}
