// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Strip;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

[Prototype]
public sealed partial class InventoryTemplatePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField("slots")] public SlotDefinition[] Slots { get; private set; } = Array.Empty<SlotDefinition>();
}

[DataDefinition]
public sealed partial class SlotDefinition
{
    [DataField("name", required: true)] public string Name { get; private set; } = string.Empty;
    [DataField("slotTexture")] public string TextureName { get; private set; } = "pocket";
    /// <summary>
    /// The texture displayed in a slot when it has an item inside of it.
    /// </summary>
    [DataField] public string FullTextureName { get; private set; } = "SlotBackground";
    [DataField("slotFlags")] public SlotFlags SlotFlags { get; private set; } = SlotFlags.PREVENTEQUIP;
    [DataField("showInWindow")] public bool ShowInWindow { get; private set; } = true;
    [DataField("slotGroup")] public string SlotGroup { get; private set; } = "Default";
    [DataField("stripTime")] public TimeSpan StripTime { get; private set; } = TimeSpan.FromSeconds(4f);

    [DataField("uiWindowPos", required: true)]
    public Vector2 UIWindowPosition { get; private set; } // Erida edit

    [DataField("strippingWindowPos", required: true)]
    public Vector2i StrippingWindowPos { get; private set; }

    // Erida start
    public const SlotFlags CompactUiSlotFlags =
        SlotFlags.UNDERWEART | SlotFlags.UNDERWEARB | SlotFlags.SOCKS | SlotFlags.UNDERPOCKET | SlotFlags.FINGER;

    [DataField("uiWindowScale")]
    public float? UIWindowScale { get; private set; }

    public float GetUiWindowScale()
    {
        if (UIWindowScale is { } scale)
            return Math.Clamp(scale, 0.25f, 1f);

        return (SlotFlags & CompactUiSlotFlags) != 0 ? 0.5f : 1f;
    }
    // Erida end

    [DataField("dependsOn")] public string? DependsOn { get; private set; }

    [DataField("dependsOnComponents")] public ComponentRegistry? DependsOnComponents { get; private set; }

    [DataField("displayName", required: true)]
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    ///     Whether or not this slot will have its item hidden in the strip menu, and block interactions.
    ///     <seealso cref="SharedStrippableSystem.IsStripHidden"/>
    /// </summary>
    [DataField("stripHidden")] public bool StripHidden { get; private set; }

    // Erida start
    [DataField] public bool StripHiddenForce { get; set; }

    [DataField] public bool StripBlocked { get; set; }
    // Erida end

    /// <summary>
    ///     Offset for the clothing sprites.
    /// </summary>
    [DataField("offset")] public Vector2 Offset { get; private set; } = Vector2.Zero;

    /// <summary>
    ///     Entity whitelist for CanEquip checks.
    /// </summary>
    [DataField("whitelist")] public EntityWhitelist? Whitelist = null;

    /// <summary>
    ///     Entity blacklist for CanEquip checks.
    /// </summary>
    [DataField("blacklist")] public EntityWhitelist? Blacklist = null;
}
