// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Server.Chat.Managers;
using Content.Server.Radio;
using Content.Server._EinsteinEngines.Language;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared.Body.Organ;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Erida.SplitPersonality;

public sealed partial class BrainPersonalitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly Robust.Server.Player.IPlayerManager _players = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    private void InitializeChat()
    {
        SubscribeLocalEvent<PersonalityChannelComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetMessage);
    }

    public bool TrySendTelepathy(EntityUid source, string message, bool hideChat)
    {
        if (!TryComp<PersonalityChannelComponent>(source, out var link))
            return false;
        // A participant in this mechanic never broadcasts onto the unrelated global psionic channel.
        if (!TryComp<BrainPersonalityComponent>(link.Brain, out var state)
            || !TryComp<OrganComponent>(link.Brain, out var organ) || organ.Body is not {} body
            || !IsConscious(body) || state.Primary is not {} primary || state.Secondary is not {} secondary
            || !_mind.TryGetMind(source, out var sender, out _) || sender != primary && sender != secondary)
            return true;
        message = FormattedMessage.RemoveMarkupPermissive(message);
        if (string.IsNullOrWhiteSpace(message))
            return true;
        var speaker = Loc.GetString(sender == primary ? "personality-primary" : "personality-secondary");
        var wrapped = Loc.GetString("personality-telepathy-message", ("speaker", speaker), ("message", FormattedMessage.EscapeText(message)));
        foreach (var participant in new[] { primary, secondary })
        {
            if (TryComp<MindComponent>(participant, out var mind) && mind.UserId is {} user
                && _players.TryGetSessionById(user, out var session))
                _chat.ChatMessageToOne(ChatChannel.Telepathic, message, wrapped, source, hideChat, session.Channel, colorOverride: Color.PaleVioletRed);
        }
        _adminLog.Add(LogType.Chat, LogImpact.Low, $"Private personality telepathy from {ToPrettyString(sender)} in {ToPrettyString(link.Brain)}: {message}");
        return true;
    }

    private bool TrySession(EntityUid mind, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return TryComp<MindComponent>(mind, out var component) && component.UserId is {} user
            && _players.TryGetSessionById(user, out session);
    }

    private void OnHeadsetMessage(Entity<PersonalityChannelComponent> host, ref HeadsetRadioReceiveRelayEvent args)
    {
        if (!TryComp<BrainPersonalityComponent>(host.Comp.Brain, out var state) || state.Host != host.Owner
            || state.Observer is not {} observer || !_mind.TryGetMind(observer, out var inactive, out _)
            || !TrySession(inactive, out var session))
            return;
        var receive = args.RelayedEvent;
        _net.ServerSendMessage(new MsgChatMessage
        {
            Message = _language.CanUnderstand(host.Owner, receive.Language.ID) ? receive.OriginalChatMsg : receive.LanguageObfuscatedChatMsg
        }, session.Channel);
    }
}
