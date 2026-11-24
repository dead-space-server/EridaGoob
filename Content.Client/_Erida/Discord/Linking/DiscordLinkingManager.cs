// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Discord.Linking;
using Robust.Shared.Network;

namespace Content.Client._Erida.Discord.Linking;

public sealed class DiscordLinkingManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;

    public event Action<DiscordLinkingCodeStatus, Guid>? CodeReceived;

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordLinkingCodeMsg>(OnCode);
        _net.RegisterNetMessage<DiscordLinkingRequestMsg>();
    }

    private void OnCode(DiscordLinkingCodeMsg message)
    {
        CodeReceived?.Invoke(message.Status, message.Code);
    }
}
