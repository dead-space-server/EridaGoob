// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.PdaPainter;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.PDA;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.PdaPainter;

public sealed class PdaPainterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    #region Init
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaPainterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<PdaPainterComponent, ComponentRemove>(OnCompRemove);
        SubscribeLocalEvent<PdaPainterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<PdaPainterComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        Subs.BuiEvents<PdaPainterComponent>(PdaPainterUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<PdaPainterPaintPdaMessage>(OnPaintPda);
            subs.Event<PdaPainterApplyCardMessage>(OnApplyCard);
            subs.Event<PdaPainterEjectMessage>(OnEject);
        });
    }

    #endregion
    #region Events

    private void OnCompInit(Entity<PdaPainterComponent> ent, ref ComponentInit args)
    {
        UpdateCachedPrototypes(ent);
        _itemSlots.AddItemSlot(ent, PdaPainterComponent.PdaSlotId, ent.Comp.PdaSlot);
        _itemSlots.AddItemSlot(ent, PdaPainterComponent.CardSlotId, ent.Comp.CardSlot);
    }

    private void OnCompRemove(Entity<PdaPainterComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.PdaSlot);
        _itemSlots.RemoveItemSlot(ent, ent.Comp.CardSlot);
    }

    private void OnEntInserted(Entity<PdaPainterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(ent);
    }

    private void OnEntRemoved(Entity<PdaPainterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(ent);
    }

    private void OnOpened(Entity<PdaPainterComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnPaintPda(Entity<PdaPainterComponent> ent, ref PdaPainterPaintPdaMessage args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        if (ent.Comp.PdaSlot.Item is not { } pda)
            return;

        if (!_proto.TryIndex<EntityPrototype>(args.ProtoId, out var proto))
            return;

        if (!ent.Comp.AllowedPdaPrototypes.Contains(proto.ID))
            return;

        var appearance = EnsureComp<AppearanceComponent>(pda);
        _appearance.SetData(pda, PdaPainterVisuals.PdaPrototype, proto.ID, appearance);

        if (proto.TryGetComponent<AppearanceComponent>(out var component)
            && component.AppearanceDataInit?.TryGetValue(PdaVisuals.PdaType, out var value) == true
            && value is string pdaType)
            _appearance.SetData(
                pda,
                PdaVisuals.PdaType,
                pdaType,
                appearance);

        _meta.SetEntityName(pda, proto.Name);
        _meta.SetEntityDescription(pda, proto.Description);
    }

    private void OnApplyCard(Entity<PdaPainterComponent> ent, ref PdaPainterApplyCardMessage args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        if (ent.Comp.CardSlot.Item is not { } card)
            return;

        if (!_proto.TryIndex<EntityPrototype>(args.ProtoId, out var proto))
            return;

        if (!ent.Comp.AllowedIdCardPrototypes.Contains(proto.ID))
            return;

        var appearance = EnsureComp<AppearanceComponent>(card);
        _appearance.SetData(card, PdaPainterVisuals.IdCardPrototype, proto.ID, appearance);
    }

    private void OnEject(Entity<PdaPainterComponent> ent, ref PdaPainterEjectMessage args)
    {
        var slotId = args.Pda ? PdaPainterComponent.PdaSlotId : PdaPainterComponent.CardSlotId;
        _itemSlots.TryEject(ent, slotId, args.Actor, out _);
    }

    #endregion
    #region Functions

    private void UpdateUi(Entity<PdaPainterComponent> ent)
    {
        var pda = ent.Comp.PdaSlot.Item;
        var card = ent.Comp.CardSlot.Item;

        var state = new PdaPainterBoundUserInterfaceState(
            pda != null ? Name(pda.Value) : null,
            card != null ? Name(card.Value) : null,
            ent.Comp.AllowedPdaPrototypes,
            ent.Comp.AllowedIdCardPrototypes
            );

        _ui.SetUiState(ent.Owner, PdaPainterUiKey.Key, state);
    }

    private void UpdateCachedPrototypes(Entity<PdaPainterComponent> ent)
    {
        ent.Comp.AllowedIdCardPrototypes.Clear();
        ent.Comp.AllowedPdaPrototypes.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || !proto.TryGetComponent<TagComponent>(component: out var tags))
                continue;

            var tagsCopy = tags.Tags;

            if (!tagsCopy.Contains(ent.Comp.WhitelistTag))
                continue;

            if (proto.HasComponent<PdaComponent>())
                ent.Comp.AllowedPdaPrototypes.Add(proto.ID);

            if (proto.HasComponent<IdCardComponent>())
                ent.Comp.AllowedIdCardPrototypes.Add(proto.ID);
        }

    }
    #endregion

}
