// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.InteQ;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.InteQ;

[UsedImplicitly]
public sealed partial class InteQCallsignBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private InteQCallsignWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<InteQCallsignWindow>();
        _window.OnSetName += name => SendMessage(new InteQCallsignSetNameMessage(name));
        _window.OnRandomName += () => SendMessage(new InteQCallsignRandomMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InteQCallsignBuiState cast || _window == null)
            return;

        _window.UpdateState(cast.Name);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
