// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Stylesheets.Colorspace;
using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Erida.Humanoid;

/// <summary>
/// Gives marking sprite layers their own instance of the gradient shader.
/// </summary>
public sealed class MarkingGradientSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static readonly ProtoId<ShaderPrototype> ShaderId = "EridaMarkingGradient";

    /// <summary>
    /// Part of a frame the head occupies, from the bottom of the frame. Other layers use the whole frame.
    /// </summary>
    private static readonly Vector2 HeadRegion = new(0.45f, 0.95f);

    private static readonly Vector2 FullRegion = new(0f, 1f);

    private static readonly HashSet<HumanoidVisualLayers> HeadLayers =
    [
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Face,
    ];

    /// <summary>
    /// Puts the marking's gradient on its sprite layers, or takes it off if the marking has none.
    /// Call it right after the layers got their colors: the first color moves from the layer into the shader,
    /// and only its alpha stays on the layer so translucent markings stay translucent.
    /// Markings with a shader of their own in the prototype keep it.
    /// </summary>
    public void Apply(Entity<SpriteComponent> sprite, MarkingPrototype proto, Marking marking)
    {
        foreach (var spriteSpecifier in proto.Sprites)
        {
            if (spriteSpecifier is not SpriteSpecifier.Rsi rsi
                || !_sprite.LayerMapTryGet(sprite.AsNullable(), $"{proto.ID}-{rsi.RsiState}", out var index, false)
                || sprite.Comp[index] is not SpriteComponent.Layer layer)
                continue;

            if (marking.Gradient is not { } gradient || proto.Shader != null || proto.ForcedColoring)
            {
                Remove(sprite, index);
                continue;
            }

            if (layer.Visible)
                ApplyToLayer(sprite, index, layer, proto, rsi, gradient.Sanitized());
        }
    }

    /// <summary>
    /// Takes the gradient shader off a layer and disposes it, if the layer has one.
    /// Call it before removing a marking layer, so the shader doesn't leak.
    /// </summary>
    public void Remove(Entity<SpriteComponent> sprite, int index)
    {
        if (sprite.Comp[index] is not SpriteComponent.Layer { Shader: { } shader } layer
            || !IsGradientLayer(layer))
            return;

        sprite.Comp.LayerSetShader(index, (ShaderInstance?) null);
        shader.Dispose();
    }

    public static bool IsGradientLayer(SpriteComponent.Layer layer)
    {
        return layer.ShaderPrototype == ShaderId.Id;
    }

    private void ApplyToLayer(
        Entity<SpriteComponent> sprite,
        int index,
        SpriteComponent.Layer layer,
        MarkingPrototype proto,
        SpriteSpecifier.Rsi rsi,
        MarkingGradient gradient)
    {
        var roots = layer.Color;
        var state = _sprite.GetState(rsi);

        // A layer that keeps its gradient reuses its shader, because markings are reapplied on every appearance change.
        var shader = IsGradientLayer(layer) && layer.Shader is { } existing
            ? existing
            : _proto.Index(ShaderId).InstanceUnique();

        shader.SetParameter("rootsLab", ToOklab(roots));
        shader.SetParameter("accentLab", ToOklab(gradient.Color));
        shader.SetParameter("gradientStyle", (int) gradient.Style);
        shader.SetParameter("amount", gradient.Offset);
        shader.SetParameter("region", HeadLayers.Contains(proto.BodyPart) ? HeadRegion : FullRegion);
        shader.SetParameter("frameSize", (Vector2) state.Size);

        // Only frame 0 of each direction fits in a uniform, so an animated state would flicker between its views.
        var directional = state.RsiDirections != RsiDirectionType.Dir1 && !state.IsAnimated;
        shader.SetParameter("frameNorth", directional ? FrameUv(state, RsiDirection.North) : Vector4.Zero);
        shader.SetParameter("frameEast", directional ? FrameUv(state, RsiDirection.East) : Vector4.Zero);
        shader.SetParameter("frameWest", directional ? FrameUv(state, RsiDirection.West) : Vector4.Zero);

        _sprite.LayerSetColor(sprite.AsNullable(), index, Color.White.WithAlpha(roots.A));

        if (layer.Shader != shader)
            sprite.Comp.LayerSetShader(index, shader, ShaderId.Id);
    }

    private static Vector3 ToOklab(Color color)
    {
        var lab = color.LabFromSrgb();
        return new Vector3(lab.X, lab.Y, lab.Z);
    }

    /// <summary>
    /// Returns the frame's rectangle in the atlas as the shader sees it in UV: min x, min y, max x, max y.
    /// </summary>
    private static Vector4 FrameUv(RSI.State state, RsiDirection direction)
    {
        if (state.GetFrame(direction, 0) is not AtlasTexture frame)
            return Vector4.Zero;

        var (width, height) = frame.SourceTexture.Size;
        var region = frame.SubRegion;
        return new Vector4(
            region.Left / width,
            (height - region.Bottom) / height,
            region.Right / width,
            (height - region.Top) / height);
    }
}
