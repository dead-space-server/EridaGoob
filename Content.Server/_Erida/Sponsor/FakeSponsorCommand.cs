// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Erida.Sponsor;

[AdminCommand(AdminFlags.Host)]
public sealed partial class FakeSponsorCommand : LocalizedEntityCommands
{
    private const string Off = "off";

    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SponsorManagerSystem _sponsors = default!;

    public override string Command => "fakesponsor";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_player.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError(Loc.GetString("cmd-fakesponsor-player-not-found", ("player", args[0])));
            return;
        }

        var admin = shell.Player?.Name ?? "server console";

        if (args[1] == Off)
        {
            _ = _sponsors.ClearFakeRank(session);
            _adminLog.Add(LogType.Action, LogImpact.High, $"{admin} cleared the fake Boosty rank of {session.Name}");
            shell.WriteLine(Loc.GetString("cmd-fakesponsor-cleared", ("player", session.Name)));
            return;
        }

        if (!int.TryParse(args[1], out var rank) || !_sponsors.TrySetFakeRank(session, rank))
        {
            shell.WriteError(Loc.GetString("cmd-fakesponsor-invalid-rank", ("rank", args[1])));
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.High, $"{admin} set the fake Boosty rank {rank} on {session.Name}");
        shell.WriteLine(Loc.GetString("cmd-fakesponsor-set", ("player", session.Name), ("rank", rank)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-fakesponsor-hint-player")),
            2 => CompletionResult.FromHintOptions([Off], Loc.GetString("cmd-fakesponsor-hint-rank")),
            _ => CompletionResult.Empty,
        };
    }
}
