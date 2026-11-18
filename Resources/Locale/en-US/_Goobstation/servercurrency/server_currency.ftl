# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Airship Coin
server-currency-name-plural = Airship Coins

## Commands

server-currency-gift-command = gift
server-currency-gift-command-description = Gifts some of your balance to another player.
server-currency-gift-command-help = Usage: gift <player> <value>
server-currency-gift-command-error-1 = You can't gift yourself!
server-currency-gift-command-error-2 = You can not afford to gift this! You have a balance of {$balance}.
server-currency-gift-command-giver = You gave {$player} {$amount}.
server-currency-gift-command-reciever = {$player} gave you {$amount}.

server-currency-balance-command = balance
server-currency-balance-command-description = Returns your balance.
server-currency-balance-command-help = Usage: balance
server-currency-balance-command-return = You have {$balance}.

server-currency-add-command = balance:add
server-currency-add-command-description = Adds currency to a player's balance.
server-currency-add-command-help = Usage: balance:add <player> <value>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Removes currency from a player's balance.
server-currency-remove-command-help = Usage: balance:rem <player> <value>

server-currency-set-command = balance:set
server-currency-set-command-description = Sets a player's balance.
server-currency-set-command-help = Usage: balance:set <player> <value>

server-currency-get-command = balance:get
server-currency-get-command-description = Gets the balance of a player.
server-currency-get-command-help = Usage: balance:get <player>

server-currency-command-completion-1 = Username
server-currency-command-completion-2 = Value
server-currency-command-error-1 = Unable to find a player by that name.
server-currency-command-error-2 = Value must be an integer.
server-currency-command-return = {$player} has {$balance}.

# 65% Update

gs-balanceui-title = Store
gs-balanceui-confirm = Confirm

gs-balanceui-gift-label = Transfer:
gs-balanceui-gift-player = Player
gs-balanceui-gift-player-tooltip = Insert the name of the player you want to send the money to
gs-balanceui-gift-value = Value
gs-balanceui-gift-value-tooltip = Amount of money to transfer

gs-balanceui-shop-label = Tokens Store
gs-balanceui-shop-empty = Out of stock!
gs-balanceui-shop-buy = Buy
gs-balanceui-shop-footer = ⚠ Ahelp to use your token. Only 1 use per day.

gs-balanceui-shop-tab-main = General
gs-balanceui-shop-tab-antag = Antagonists
gs-balanceui-shop-tittle-label = Titles

gs-balanceui-shop-buy-token-admin-abuse = Buy an Admin Abuse Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-hat = Buy a Hat Token - {$price} Airship Coins

gs-balanceui-shop-token-admin-abuse = Admin Abuse Token
gs-balanceui-shop-token-hat = Hat Token

gs-balanceui-shop-buy-token-admin-abuse-desc = Allows you to request an admin to abuse their powers against you. Admins are encouraged to go wild.
gs-balanceui-shop-buy-token-hat-desc = An admin will give you a random hat.

gs-balanceui-admin-add-label = Add (or subtract) money:
gs-balanceui-admin-add-player = Player name
gs-balanceui-admin-add-value = Value

gs-balanceui-remark-token-admin-abuse = Bought an admin abuse token.
gs-balanceui-remark-token-hat = Bought a hat token.

gs-balanceui-shop-buy-token-antag-derelict-cyborg = Buy a Malfunctioning Cyborg Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-lone-abductor = Buy a Lone Abductor Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-bingle = Buy a Bingle Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-blob = Buy a Blob Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-contractor = Buy a Contractor Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-dragon = Buy a Space Dragon Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-hastur = Buy a Hastur Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-laughter-demon = Buy a Laughter Demon Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-ninja = Buy a Space Ninja Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-wraith = Buy a Wraith Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-lone-ops = Buy a Lone Operative Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-slasher = Buy a Slasher Token - {$price} Airship Coins
gs-balanceui-shop-buy-token-antag-slaughter-demon = Buy a Slaughter Demon Token - {$price} Airship Coins

gs-balanceui-shop-token-antag-derelict-cyborg = Malfunctioning Cyborg Token
gs-balanceui-shop-token-antag-lone-abductor = Lone Abductor Token
gs-balanceui-shop-token-antag-bingle = Bingle Token
gs-balanceui-shop-token-antag-blob = Blob Token
gs-balanceui-shop-token-antag-contractor = Contractor Token
gs-balanceui-shop-token-antag-dragon = Space Dragon Token
gs-balanceui-shop-token-antag-hastur = Hastur Token
gs-balanceui-shop-token-antag-laughter-demon = Laughter Demon Token
gs-balanceui-shop-token-antag-ninja = Space Ninja Token
gs-balanceui-shop-token-antag-wraith = Wraith Token
gs-balanceui-shop-token-antag-lone-ops = Lone Operative Token
gs-balanceui-shop-token-antag-slasher = Slasher Token
gs-balanceui-shop-token-antag-slaughter-demon = Slaughter Demon Token

gs-balanceui-remark-token-antag-derelict-cyborg = Bought a malfunctioning cyborg token.
gs-balanceui-remark-token-antag-lone-abductor = Bought a lone abductor token.
gs-balanceui-remark-token-antag-bingle = Bought a bingle token.
gs-balanceui-remark-token-antag-blob = Bought a blob token.
gs-balanceui-remark-token-antag-contractor = Bought a contractor token.
gs-balanceui-remark-token-antag-dragon = Bought a space dragon token.
gs-balanceui-remark-token-antag-hastur = Bought a Hastur token.
gs-balanceui-remark-token-antag-laughter-demon = Bought a laughter demon token.
gs-balanceui-remark-token-antag-ninja = Bought a space ninja token.
gs-balanceui-remark-token-antag-wraith = Bought a wraith token.
gs-balanceui-remark-token-antag-lone-ops = Bought a lone operative token.
gs-balanceui-remark-token-antag-slasher = Bought a slasher token.
gs-balanceui-remark-token-antag-slaughter-demon = Bought a slaughter demon token.

gs-balanceui-shop-click-confirm = Click again to confirm
gs-balanceui-shop-purchased = Purchased {$item}
