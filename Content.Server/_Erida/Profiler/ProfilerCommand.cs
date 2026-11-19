// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Erida.Profiler;

[AdminCommand(AdminFlags.Host)]
public sealed partial class ProfilerCommand : LocalizedEntityCommands
{
    [Dependency] private EuiManager _eui = default!;

    public override string Command => "erida_profiler";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _eui.OpenEui(new ProfilerEui(), player);
    }
}
