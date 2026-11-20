// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;

namespace Content.Shared._Erida.Silicons.CellService;

public sealed class SiliconCellServiceSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconCellServiceComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<SiliconCellServiceComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<SiliconCellServiceComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
        SubscribeLocalEvent<SiliconCellServiceComponent, ExaminedEvent>(OnExamined);
    }

    private void OnLockToggleAttempt(Entity<SiliconCellServiceComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (args.Cancelled || args.User != ent.Owner)
            return;

        args.Cancelled = true;
        if (!args.Silent)
            _popup.PopupClient(Loc.GetString("silicon-cell-service-self-lock"), ent, args.User);
    }

    private void OnInsertAttempt(Entity<SiliconCellServiceComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (IsSelfService(ent, args.User, args.Slot))
            args.Cancelled = true;
    }

    private void OnEjectAttempt(Entity<SiliconCellServiceComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (IsSelfService(ent, args.User, args.Slot))
            args.Cancelled = true;
    }

    private void OnExamined(Entity<SiliconCellServiceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || _lock.IsLocked(ent.Owner))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            args.PushMarkup(Loc.GetString("silicon-cell-service-examine-empty"));
            return;
        }

        var charge = _battery.GetChargeLevel(battery.Value.AsNullable()) * 100;
        args.PushMarkup(Loc.GetString("silicon-cell-service-examine",
            ("cell", battery.Value.Owner),
            ("charge", $"{charge:F0}")));
    }

    private static bool IsSelfService(Entity<SiliconCellServiceComponent> ent, EntityUid? user, ItemSlot slot)
    {
        return user == ent.Owner && slot.ID == ent.Comp.CellSlot;
    }
}
