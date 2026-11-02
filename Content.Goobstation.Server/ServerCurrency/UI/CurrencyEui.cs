// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency.UI;
using Content.Server._Erida.Discord;
using Content.Server.Administration.Notes;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.ServerCurrency.UI
{
    public sealed partial class CurrencyEui : BaseEui
    {
        [Dependency] private ICommonCurrencyManager _currencyMan = default!;
        [Dependency] private IAdminNotesManager _notesMan = default!;
        [Dependency] private IPrototypeManager _protoMan = default!;

        [Dependency] private IChatManager _chat = default!; // Erida edit
        [Dependency] private EridaWebhooks _webhooks = default!; // Erida edit
        [Dependency] private ILogManager _log = default!;

        private readonly ISawmill _sawmill;
        public CurrencyEui()
        {
            IoCManager.InjectDependencies(this);
            _sawmill = _log.GetSawmill("server.currency");
        }

        public override void Opened()
        {
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            return new CurrencyEuiState();
        }


        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            switch (msg)
            {
                case CurrencyEuiMsg.Buy Buy:

                    BuyToken(Buy.TokenId, Player);
                    StateDirty();
                    break; //grrr fix formatting
            }
        }

        private async void BuyToken(ProtoId<TokenListingPrototype> tokenId, ICommonSession playerName)
        {
            var balance = _currencyMan.GetBalance(Player.UserId);

            if (!_protoMan.TryIndex<TokenListingPrototype>(tokenId, out var token))
                return;

            if (balance < token.Price)
                return;

            _currencyMan.RemoveCurrency(Player.UserId, token.Price);

            // erida edit
            try
            {
                await _notesMan.AddSystemRemark(Player.UserId, NoteType.Note,
                    Loc.GetString(token.AdminNote), NoteSeverity.None, false, null);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to add token note: {e}");
            }

            // Erida start
            _chat.SendAdminAnnouncement($"{Player.Name} " + Loc.GetString(token.AdminNote));
            _webhooks.SendTokenBoughtMessage(Player.UserId, token);
            // Erida end
        }
    }
}
