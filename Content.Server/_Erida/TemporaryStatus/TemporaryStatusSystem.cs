// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Erida.TemporaryStatus;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Erida.TemporaryStatus;

public sealed class TemporaryStatusSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/information.svg.192dpi.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryStatusComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<TemporaryStatusComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TemporaryStatusComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnGetVerbs(Entity<TemporaryStatusComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (args.User != ent.Owner
            || !TryComp<ActorComponent>(ent, out var actor))
            return;

        var session = actor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("temporary-status-verb"),
            Message = Loc.GetString("temporary-status-verb-message"),
            Icon = VerbIcon,
            Act = () => OpenDialog(ent, session),
        });
    }

    private void OnExamined(Entity<TemporaryStatusComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Text is not { } text)
            return;

        if (ent.Comp.ExpiresAt <= _timing.CurTime)
        {
            ent.Comp.Text = null;
            return;
        }

        args.PushMarkup(Loc.GetString("temporary-status-examine", ("status", FormattedMessage.EscapeText(text))), 99);
    }

    private void OnMobStateChanged(Entity<TemporaryStatusComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            ent.Comp.Text = null;
    }

    private void OpenDialog(Entity<TemporaryStatusComponent> ent, ICommonSession session)
    {
        _quickDialog.OpenDialog(session,
            Loc.GetString("temporary-status-dialog-title", ("max", ent.Comp.MaxLength)),
            Loc.GetString("temporary-status-dialog-prompt"),
            (string input) => SetStatus(ent, session, input));
    }

    private void SetStatus(Entity<TemporaryStatusComponent> ent, ICommonSession session, string input)
    {
        if (TerminatingOrDeleted(ent)
            || session.AttachedEntity != ent.Owner
            || _mobState.IsDead(ent))
            return;

        var text = input.Trim();
        if (text.Length == 0)
        {
            ent.Comp.Text = null;
            _popup.PopupEntity(Loc.GetString("temporary-status-popup-cleared"), ent.Owner, ent.Owner);
            return;
        }

        if (text.Length > ent.Comp.MaxLength)
            text = text[..ent.Comp.MaxLength];

        ent.Comp.Text = text;
        ent.Comp.ExpiresAt = _timing.CurTime + ent.Comp.Duration;
        _popup.PopupEntity(Loc.GetString("temporary-status-popup-set"), ent.Owner, ent.Owner);
        _adminLog.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(ent.Owner):user} set temporary status: {text}");
    }
}
