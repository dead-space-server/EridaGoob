// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._Erida.StationGoals;

/// <summary>
/// Switching meteor shield satellites on and off. A satellite works while switched on, anchored and powered,
/// a switched off satellite draws no power.
/// </summary>
public abstract class SharedMeteorShieldSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeteorShieldComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MeteorShieldComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<MeteorShieldComponent, MeteorShieldToggleDoAfterEvent>(OnToggleDoAfter);
        SubscribeLocalEvent<MeteorShieldComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MeteorShieldComponent, PowerChangedEvent>(OnPowerChanged);
    }

    #region Component events

    private void OnMapInit(Entity<MeteorShieldComponent> ent, ref MapInitEvent args)
    {
        _power.SetPowerDisabled(ent, !ent.Comp.Active);
        UpdateAppearance(ent);
    }

    private void OnActivate(Entity<MeteorShieldComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!ent.Comp.Active && !Transform(ent).Anchored)
        {
            _popup.PopupClient(Loc.GetString("meteor-shield-popup-not-anchored"), ent, args.User);
            args.Handled = true;
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.ToggleDelay, new MeteorShieldToggleDoAfterEvent(), ent, ent)
        {
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnToggleDoAfter(Entity<MeteorShieldComponent> ent, ref MeteorShieldToggleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (!ent.Comp.Active && !Transform(ent).Anchored)
        {
            _popup.PopupClient(Loc.GetString("meteor-shield-popup-not-anchored"), ent, args.User);
            return;
        }

        SetActive(ent, !ent.Comp.Active);

        var key = ent.Comp.Active ? "meteor-shield-popup-activated" : "meteor-shield-popup-deactivated";
        _popup.PopupClient(Loc.GetString(key), ent, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} switched {ToPrettyString(ent):entity} {(ent.Comp.Active ? "on" : "off")}");
    }

    private void OnAnchorChanged(Entity<MeteorShieldComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            SetActive(ent, false);
    }

    private void OnPowerChanged(Entity<MeteorShieldComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    #endregion

    #region Helpers

    public void SetActive(Entity<MeteorShieldComponent> ent, bool active)
    {
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        Dirty(ent);
        _power.SetPowerDisabled(ent, !active);
        UpdateAppearance(ent);
    }

    /// <summary>
    /// Switched on, anchored and powered.
    /// </summary>
    public bool IsWorking(Entity<MeteorShieldComponent> ent, TransformComponent xform)
    {
        return ent.Comp.Active && xform.Anchored && _power.IsPowered(ent.Owner);
    }

    private void UpdateAppearance(Entity<MeteorShieldComponent> ent)
    {
        _appearance.SetData(ent, MeteorShieldVisuals.Active, IsWorking(ent, Transform(ent)));
    }

    #endregion
}
