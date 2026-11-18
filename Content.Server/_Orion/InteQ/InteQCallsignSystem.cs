// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.RandomMetadata;
using Content.Shared._Orion.InteQ;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._Orion.InteQ;

public sealed class InteQCallsignSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly RandomMetadataSystem _randomMetadata = default!;

    private const string ClientTypeName = "InteQCallsignBoundUserInterface";

    private int _maxNameLength;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteQCallsignComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InteQCallsignComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<InteQCallsignComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<InteQCallsignComponent, BoundUserInterfaceMessageAttempt>(OnMessageAttempt);
        SubscribeLocalEvent<InteQCallsignComponent, InteQCallsignSetNameMessage>(OnSetName);
        SubscribeLocalEvent<InteQCallsignComponent, InteQCallsignRandomMessage>(OnRandomName);

        Subs.CVar(_cfg, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void OnInit(Entity<InteQCallsignComponent> ent, ref ComponentInit args)
    {
        var uiComp = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent, uiComp), InteQCallsignUIKey.Key, new InterfaceData(ClientTypeName, -1));
    }

    private void OnPlayerAttached(Entity<InteQCallsignComponent> ent, ref PlayerAttachedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        OpenUi(ent);
    }

    private void OnGetVerbs(Entity<InteQCallsignComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != ent.Owner || !args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("inteq-callsign-verb"),
            Act = () => OpenUi(ent),
        });
    }

    private void OnMessageAttempt(Entity<InteQCallsignComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey is InteQCallsignUIKey && args.Actor != ent.Owner)
            args.Cancel();
    }

    private void OnSetName(Entity<InteQCallsignComponent> ent, ref InteQCallsignSetNameMessage args)
    {
        var name = args.Name.Trim();

        if (name.Length == 0 || name.Length > _maxNameLength)
        {
            _popup.PopupEntity(Loc.GetString("inteq-callsign-popup-failure"), ent, args.Actor, PopupType.SmallCaution);
            return;
        }

        ApplyName(ent, name, args.Actor);
        CloseUi(ent, args.Actor);
    }

    private void OnRandomName(Entity<InteQCallsignComponent> ent, ref InteQCallsignRandomMessage args)
    {
        if (!TryComp<RandomMetadataComponent>(ent, out var randomMeta)
            || randomMeta.NameSegments is not { Count: > 0 })
        {
            _popup.PopupEntity(Loc.GetString("inteq-callsign-popup-failure"), ent, args.Actor, PopupType.SmallCaution);
            return;
        }

        var name = _randomMetadata.GetRandomFromSegments(randomMeta.NameSegments, randomMeta.NameFormat);
        UpdateUi(ent, name);
    }

    private void ApplyName(Entity<InteQCallsignComponent> ent, string name, EntityUid actor)
    {
        _metaData.SetEntityName(ent, name);

        if (TryComp<MindContainerComponent>(ent, out var mindContainer)
            && mindContainer.Mind != null
            && TryComp<MindComponent>(mindContainer.Mind, out var mind))
        {
            mind.CharacterName = name;
            Dirty(mindContainer.Mind.Value, mind);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(actor):player} set InteQ callsign of {ToPrettyString(ent):entity} to {name}");

        _popup.PopupEntity(Loc.GetString("inteq-callsign-popup-success"), ent, actor);

        UpdateUi(ent);
    }

    private void OpenUi(Entity<InteQCallsignComponent> ent)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        UpdateUi(ent);
        _ui.OpenUi((ent, uiComp), InteQCallsignUIKey.Key, ent.Owner);
    }

    private void UpdateUi(Entity<InteQCallsignComponent> ent)
    {
        UpdateUi(ent, Name(ent));
    }

    private void UpdateUi(Entity<InteQCallsignComponent> ent, string name)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        _ui.SetUiState((ent, uiComp), InteQCallsignUIKey.Key, new InteQCallsignBuiState(name));
    }

    private void CloseUi(Entity<InteQCallsignComponent> ent, EntityUid actor)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uiComp))
            return;

        _ui.CloseUi((ent, uiComp), InteQCallsignUIKey.Key, actor);
    }
}
