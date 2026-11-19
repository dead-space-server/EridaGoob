// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Shared._Erida.Profiler;
using Content.Shared.Eui;

namespace Content.Client._Erida.Profiler;

public sealed class ProfilerEui : BaseEui
{
    private ProfilerWindow? _window;

    public override void Opened()
    {
        base.Opened();

        _window = new ProfilerWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.ResetPressed += () => SendMessage(new ProfilerResetMessage());
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window?.Close();
        _window = null;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is ProfilerEuiState profilerState)
            _window?.SetState(profilerState);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case ProfilerStatsMessage stats:
                _window?.UpdateStats(stats.Overview, stats.Systems);
                break;
            case ProfilerSlowTicksMessage slowTicks:
                _window?.AddSlowTicks(slowTicks.SlowTicks);
                break;
            case ProfilerLogsMessage logs:
                _window?.UpdateLogs(logs.Logs);
                break;
        }
    }
}
