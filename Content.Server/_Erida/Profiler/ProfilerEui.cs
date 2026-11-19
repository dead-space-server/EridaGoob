// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Erida.Profiler;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server._Erida.Profiler;

public sealed partial class ProfilerEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntitySystemManager _systems = default!;

    private readonly ProfilerSystem _profiler;

    public ProfilerEui()
    {
        IoCManager.InjectDependencies(this);
        _profiler = _systems.GetEntitySystem<ProfilerSystem>();
    }

    public override void Opened()
    {
        base.Opened();

        if (!IsHost())
        {
            Close();
            return;
        }

        _adminManager.OnPermsChanged += OnPermsChanged;
        _profiler.AddSubscriber(this);
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();

        _adminManager.OnPermsChanged -= OnPermsChanged;
        _profiler.RemoveSubscriber(this);
    }

    public override EuiStateBase GetNewState()
    {
        return _profiler.GetState();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is ProfilerResetMessage && IsHost())
            _profiler.Reset();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !IsHost())
            Close();
    }

    private bool IsHost()
    {
        return _adminManager.HasAdminFlag(Player, AdminFlags.Host);
    }
}
