// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Stylesheets;
using Content.Client.Message;
using Content.Shared._Erida.CCVar;
using Content.Shared._Erida.Discord.Linking;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Erida.Discord.Linking;

public sealed class DiscordLinkingUIController : UIController, IOnSystemChanged<DiscordLinkingSystem>
{
    [Dependency] private IClipboardManager _clipboard = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private DiscordLinkingManager _linkingManager = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IUriOpener _uriOpener = default!;
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private EridaThemeManager _theme = default!;

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CopiedDelay = TimeSpan.FromSeconds(3);

    private DiscordLinkingWindow? _window;
    private TimeSpan _disableUntil;
    private Guid _code;
    private DiscordLinkingCodeStatus? _status;
    private TimeSpan _codeExpires;

    private DiscordLinkingSystem LinkingSystem => _entitySystemManager.GetEntitySystem<DiscordLinkingSystem>();

    private bool HasValidCode => _code != default && _timing.RealTime < _codeExpires;

    private bool IsLinked => LinkingSystem.IsVerified || _status == DiscordLinkingCodeStatus.AlreadyLinked;

    public override void Initialize()
    {
        _linkingManager.CodeReceived += OnCode;
    }

    public void OnSystemLoaded(DiscordLinkingSystem system)
    {
        system.VerifiedChanged += UpdateWindow;
    }

    public void OnSystemUnloaded(DiscordLinkingSystem system)
    {
        system.VerifiedChanged -= UpdateWindow;
    }

    private void OnCode(DiscordLinkingCodeStatus status, Guid code)
    {
        _status = status;
        _code = status == DiscordLinkingCodeStatus.Ok ? code : default;
        _codeExpires = _timing.RealTime + CodeLifetime;

        UpdateWindow();
    }

    private void UpdateWindow()
    {
        if (_window == null)
            return;

        var text = IsLinked
            ? Loc.GetString("rmc-ui-link-discord-account-already-linked")
            : Loc.GetString("rmc-ui-link-discord-account-text");

        var error = _status switch
        {
            DiscordLinkingCodeStatus.Unavailable => "erida-discord-linking-unavailable",
            DiscordLinkingCodeStatus.Error => "erida-discord-linking-error",
            _ => null,
        };

        if (error != null && !IsLinked)
            text = $"{text}\n\n[color={_theme.Palette.Error.ToHex()}]{Loc.GetString(error)}[/color]";

        _window.Label.SetMarkupPermissive(text);
        _window.CopyButton.Visible = !IsLinked;

        if (_disableUntil == default)
            _window.CopyButton.Disabled = !HasValidCode;
    }

    public void ToggleWindow()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        LinkingSystem.RequestVerificationStatus();

        _window = new DiscordLinkingWindow();
        _window.OnClose += () => _window = null;

        _window.CopyButton.OnPressed += _ =>
        {
            _clipboard.SetText(_code.ToString());
            _window.CopyButton.Text = Loc.GetString("rmc-ui-link-discord-account-copied");
            _window.CopyButton.Disabled = true;
            _disableUntil = _timing.RealTime + CopiedDelay;
        };

        var messageLink = _config.GetCVar(ECCVars.DiscordAccountLinkingMessageLink);
        _window.LinkButton.Visible = !string.IsNullOrEmpty(messageLink);
        _window.LinkButton.OnPressed += _ => _uriOpener.OpenUri(messageLink);

        if (!HasValidCode)
        {
            _status = null;
            _net.ClientSendMessage(new DiscordLinkingRequestMsg());
        }

        UpdateWindow();
        _window.OpenCentered();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_window == null || _disableUntil == default || _timing.RealTime < _disableUntil)
            return;

        _disableUntil = default;
        _window.CopyButton.Text = Loc.GetString("rmc-ui-link-discord-account-copy");
        _window.CopyButton.Disabled = !HasValidCode;
    }
}
