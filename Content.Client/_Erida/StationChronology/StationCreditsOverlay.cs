// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Erida.StationChronology;

/// <summary>
/// Taken from https://github.com/Lua-Frontier/sector-frontier-14/commit/681d59967202b7a9cc9107475b606e3a4e7cec1f
/// </summary>
public sealed class StationCreditsOverlay : Overlay
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    private Font _font;
    private Font _descriptionfont;

    public string? Text;
    public int Index;
    public bool Reverse;
    public TimeSpan CharInterval;
    private TimeSpan _nextUpd = TimeSpan.Zero;

    public string? TextDescription;
    public TimeSpan CharIntervalDescription;
    public int IndexDescription;
    public bool ReverseDescription;
    private TimeSpan _nextUpdDescription = TimeSpan.Zero;

    private const float TitleOffset = 160f;
    private const float DescriptionOffset = 270f;

    public StationCreditsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _cache.GetFont("/Fonts/_Erida/Doloto/Doloto-Regular.ttf", 75); //Lua Iceberg -> Doloto
        _descriptionfont = _cache.GetFont("/Fonts/_Erida/Doloto/Doloto-Regular.ttf", 30); //Lua Iceberg -> Doloto
    }

    public void Reset()
    {
        Text = null;
        Index = 0;
        Reverse = false;
        _nextUpd = TimeSpan.Zero;
    }
    public void ResetDescription()
    {
        TextDescription = null;
        IndexDescription = 0;
        ReverseDescription = false;
        _nextUpdDescription = TimeSpan.Zero;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Text == null || Text == string.Empty)
            return;

        DrawDescription(args); //if there is no description, this returns almost immediately so it shouldnt interfere

        var position = CalcPosition(args, _font, Text, TitleOffset);
        args.ScreenHandle.DrawString(_font, position, Text[..Index]);

        if (_nextUpd > _timing.CurTime)
            return;

        if (!Reverse && Index == Text.Length)
        {
            Reverse = true;

            //delay before text is erased
            _nextUpd += TimeSpan.FromSeconds(4);
            Index++;
        }

        if (Reverse && Index == 0)
        {
            Reset();
            return;
        }

        Index = Reverse ? Index - 1 : Index + 1;

        if (_nextUpd == TimeSpan.Zero)
            _nextUpd = _timing.CurTime;
        _nextUpd += CharInterval;
    }

    private void DrawDescription(OverlayDrawArgs args)
    {
        if (TextDescription == null || TextDescription == string.Empty)
            return;

        var position = CalcPosition(args, _descriptionfont, TextDescription, DescriptionOffset);
        args.ScreenHandle.DrawString(_descriptionfont, position, TextDescription[..IndexDescription], Color.DarkGray);

        if (_nextUpdDescription > _timing.CurTime)
            return;

        if (!ReverseDescription && IndexDescription == TextDescription.Length)
        {
            ReverseDescription = true;

            //delay before description is erased
            _nextUpdDescription += TimeSpan.FromSeconds(4);
            IndexDescription++;
        }

        if (ReverseDescription && IndexDescription == 0)
        {
            ResetDescription();
            return;
        }

        IndexDescription = ReverseDescription ? IndexDescription - 1 : IndexDescription + 1;

        if (_nextUpdDescription == TimeSpan.Zero)
            _nextUpdDescription = _timing.CurTime;
        _nextUpdDescription += CharIntervalDescription;
    }

    /// <summary>
    /// Centers the full string horizontally in the game viewport, so the typing animation does not shift it.
    /// </summary>
    private static Vector2 CalcPosition(in OverlayDrawArgs args, Font font, string str, float offset)
    {
        var bounds = args.ViewportBounds;
        var size = args.ScreenHandle.GetDimensions(font, str, 1f);
        return new Vector2(bounds.Left + (bounds.Width - size.X) / 2, bounds.Top + offset);
    }
}
