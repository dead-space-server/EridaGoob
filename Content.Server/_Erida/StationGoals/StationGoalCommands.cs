// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Admin management of station goals: list, set, clear, resend and force the result.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class StationGoalCommand : LocalizedEntityCommands
{
    [Dependency] private StationGoalSystem _goal = default!;

    private const string RandomGoal = "random";

    private static readonly string[] SubCommands = ["list", "set", "clear", "resend", "result"];
    private static readonly string[] Results = ["true", "false", "auto"];

    public override string Command => "stationgoal";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        switch (args[0])
        {
            case "list" when args.Length == 1:
                List(shell);
                break;
            case "set" when args.Length == 3:
                Set(shell, args[1], args[2]);
                break;
            case "clear" when args.Length == 2:
                Clear(shell, args[1]);
                break;
            case "resend" when args.Length == 2:
                Resend(shell, args[1]);
                break;
            case "result" when args.Length == 3:
                SetResult(shell, args[1], args[2]);
                break;
            default:
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                shell.WriteLine(Help);
                break;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(SubCommands, Loc.GetString("cmd-stationgoal-arg-subcommand"));

        if (args[0] == "list")
            return CompletionResult.Empty;

        return args.Length switch
        {
            2 => CompletionResult.FromHintOptions(ContentCompletionHelper.StationIds(EntityManager),
                Loc.GetString("cmd-stationgoal-arg-station")),
            3 when args[0] == "set" => CompletionResult.FromHintOptions(
                _goal.Goals.Select(id => new CompletionOption(id)).Prepend(new CompletionOption(RandomGoal)),
                Loc.GetString("cmd-stationgoal-arg-goal")),
            3 when args[0] == "result" => CompletionResult.FromHintOptions(Results,
                Loc.GetString("cmd-stationgoal-arg-result")),
            _ => CompletionResult.Empty,
        };
    }

    #region Subcommands

    private void List(IConsoleShell shell)
    {
        var any = false;
        var query = EntityManager.EntityQueryEnumerator<StationGoalComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var station, out var meta))
        {
            any = true;
            var stationName = $"{meta.EntityName} ({EntityManager.GetNetEntity(uid)})";
            if (!_goal.TryGetGoal((uid, station), out var goal))
            {
                shell.WriteLine(Loc.GetString("cmd-stationgoal-list-no-goal", ("station", stationName)));
                continue;
            }

            var progress = _goal.GetProgress(goal.Value);
            shell.WriteLine(Loc.GetString("cmd-stationgoal-list-goal",
                ("station", stationName),
                ("goal", Loc.GetString(goal.Value.Comp.Name)),
                ("id", EntityManager.GetComponent<MetaDataComponent>(goal.Value).EntityPrototype?.ID ?? string.Empty),
                ("percent", progress is { } value ? (int) MathF.Floor(value * 100f) : -1),
                ("forced", goal.Value.Comp.ForceCompleted?.ToString().ToLowerInvariant() ?? "auto")));
        }

        if (!any)
            shell.WriteLine(Loc.GetString("cmd-stationgoal-list-empty"));
    }

    private void Set(IConsoleShell shell, string stationArg, string goalArg)
    {
        if (!TryParseStation(shell, stationArg, out var station))
            return;

        EntProtoId goalId;
        if (goalArg == RandomGoal)
        {
            if (!_goal.TryPickGoal(station, out var picked))
            {
                shell.WriteError(Loc.GetString("cmd-stationgoal-no-fitting-goal"));
                return;
            }

            goalId = picked.Value;
        }
        else if (_goal.Goals.Contains(new EntProtoId(goalArg)))
        {
            goalId = goalArg;
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-invalid-goal", ("value", goalArg)));
            return;
        }

        var forceReset = _goal.TryGetGoal((station.Owner, station.Comp), out var old)
            && old.Value.Comp.ForceCompleted != null;

        if (!_goal.TryAssignGoal((station.Owner, station.Comp), goalId, out var faxes, shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-invalid-goal", ("value", goalArg)));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-stationgoal-set-success", ("goal", goalId.Id), ("faxes", faxes)));
        if (forceReset)
            shell.WriteLine(Loc.GetString("cmd-stationgoal-set-force-reset"));
        WriteFaxWarning(shell, faxes);
    }

    private void Clear(IConsoleShell shell, string stationArg)
    {
        if (!TryParseStation(shell, stationArg, out var station))
            return;

        if (!_goal.TryClearGoal((station.Owner, station.Comp), shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-no-goal"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-stationgoal-clear-success"));
    }

    private void Resend(IConsoleShell shell, string stationArg)
    {
        if (!TryParseStation(shell, stationArg, out var station))
            return;

        if (!_goal.TryResendGoal((station.Owner, station.Comp), out var faxes, shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-no-goal"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-stationgoal-resend-success", ("faxes", faxes)));
        WriteFaxWarning(shell, faxes);
    }

    private void SetResult(IConsoleShell shell, string stationArg, string resultArg)
    {
        if (!TryParseStation(shell, stationArg, out var station))
            return;

        bool? result;
        switch (resultArg)
        {
            case "true":
                result = true;
                break;
            case "false":
                result = false;
                break;
            case "auto":
                result = null;
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-stationgoal-invalid-result", ("value", resultArg)));
                return;
        }

        if (!_goal.TrySetForceCompleted((station.Owner, station.Comp), result, shell.Player))
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-no-goal"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-stationgoal-result-success", ("result", resultArg)));
    }

    #endregion

    #region Helpers

    private bool TryParseStation(IConsoleShell shell, string arg, out Entity<StationGoalComponent> station)
    {
        station = default;
        if (!NetEntity.TryParse(arg, out var netStation)
            || !EntityManager.TryGetEntity(netStation, out var uid)
            || !EntityManager.TryGetComponent<StationGoalComponent>(uid, out var comp))
        {
            shell.WriteError(Loc.GetString("cmd-stationgoal-invalid-station", ("value", arg)));
            return false;
        }

        station = (uid.Value, comp);
        return true;
    }

    private void WriteFaxWarning(IConsoleShell shell, int faxes)
    {
        if (faxes == 0)
            shell.WriteError(Loc.GetString("cmd-stationgoal-no-faxes"));
    }

    #endregion
}
