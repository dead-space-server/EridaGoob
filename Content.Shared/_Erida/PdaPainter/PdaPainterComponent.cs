// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.PdaPainter;

[RegisterComponent, NetworkedComponent]
public sealed partial class PdaPainterComponent : Component
{
    public const string PdaSlotId = "PdaPainter-pdaSlot";
    public const string CardSlotId = "PdaPainter-cardSlot";

    [DataField] public ItemSlot PdaSlot = new();

    [DataField] public ItemSlot CardSlot = new();
    [DataField(readOnly: true)] public ProtoId<TagPrototype> WhitelistTag = "PdaPainterWhitelist";

    public HashSet<ProtoId<EntityPrototype>> AllowedIdCardPrototypes = [];
    public HashSet<ProtoId<EntityPrototype>> AllowedPdaPrototypes = [];
}
