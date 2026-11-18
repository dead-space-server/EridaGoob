# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Коин
server-currency-name-plural = Коины

## Команды

server-currency-gift-command = gift
server-currency-gift-command-description = Передать часть вашего баланса другому игроку.
server-currency-gift-command-help = Использование: подарить <игрок> <сумма>
server-currency-gift-command-error-1 = Вы не можете подарить себе!
server-currency-gift-command-error-2 = У вас недостаточно средств для передачи! Ваш баланс: { $balance }.
server-currency-gift-command-giver = Вы передали { $player } { $amount }.
server-currency-gift-command-reciever = { $player } передал вам { $amount }.

server-currency-balance-command = balance
server-currency-balance-command-description = Показывает ваш баланс.
server-currency-balance-command-help = Использование: баланс
server-currency-balance-command-return = У вас { $balance }.

server-currency-add-command = balance:add
server-currency-add-command-description = Добавляет валюту на счет игрока.
server-currency-add-command-help = Использование: баланс:добавить <игрок> <сумма>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Убирает валюту со счета игрока.
server-currency-remove-command-help = Использование: баланс:убрать <игрок> <сумма>

server-currency-set-command = balance:set
server-currency-set-command-description = Устанавливает баланс игрока.
server-currency-set-command-help = Использование: баланс:установить <игрок> <сумма>

server-currency-get-command = balance:get
server-currency-get-command-description = Узнаёт баланс указанного игрока.
server-currency-get-command-help = Использование: баланс:узнать <игрок>

server-currency-command-completion-1 = Имя игрока
server-currency-command-completion-2 = Значение
server-currency-command-error-1 = Игрок с таким именем не найден.
server-currency-command-error-2 = Значение должно быть целым числом.
server-currency-command-return = У { $player } { $balance }.

# Обновление 65%

gs-balanceui-title = Магазин
gs-balanceui-confirm = Подтвердить
gs-balanceui-gift-label = Перевод:
gs-balanceui-gift-player = Игрок
gs-balanceui-gift-player-tooltip = Введите имя игрока, которому хотите отправить деньги
gs-balanceui-gift-value = Сумма
gs-balanceui-gift-value-tooltip = Количество денег для перевода
gs-balanceui-shop-label = Магазин токенов
gs-balanceui-shop-empty = Нет в наличии!
gs-balanceui-shop-buy = Купить
gs-balanceui-shop-footer = ⚠ Используйте ваш токен через Ahelp. Только 1 раз в день.
gs-balanceui-shop-tab-main = Общее
gs-balanceui-shop-tab-antag = Антагонисты
gs-balanceui-shop-tittle-label = Титулы
gs-balanceui-shop-buy-token-admin-abuse = Купить токен на злоупотребление админом - { $price } Коинов
gs-balanceui-shop-buy-token-hat = Купить токен на шляпу - { $price } Коинов
gs-balanceui-shop-token-admin-abuse = Токен злоупотребления админом
gs-balanceui-shop-token-hat = Токен шляпы
gs-balanceui-shop-buy-token-admin-abuse-desc = Позволяет попросить админа злоупотребить своими полномочиями по отношению к вам. Админам рекомендуется не сдерживаться.
gs-balanceui-shop-buy-token-hat-desc = Админ выдаст вам случайную шляпу.
gs-balanceui-admin-add-label = Добавить (или убрать) деньги:
gs-balanceui-admin-add-player = Имя игрока
gs-balanceui-admin-add-value = Сумма
gs-balanceui-remark-token-admin-abuse = Куплен токен злоупотребления админом.
gs-balanceui-remark-token-hat = Куплен токен шляпы.

gs-balanceui-shop-buy-token-antag-derelict-cyborg = Купить токен на малф борга - { $price } Коинов
gs-balanceui-shop-buy-token-antag-lone-abductor = Купить токен на одинокого абдуктора - { $price } Коинов
gs-balanceui-shop-buy-token-antag-bingle = Купить токен на бинглов - { $price } Коинов
gs-balanceui-shop-buy-token-antag-blob = Купить токен на блоба - { $price } Коинов
gs-balanceui-shop-buy-token-antag-contractor = Купить токен на контрактника - { $price } Коинов
gs-balanceui-shop-buy-token-antag-dragon = Купить токен на дракона - { $price } Коинов
gs-balanceui-shop-buy-token-antag-hastur = Купить токен на короля в желтом - { $price } Коинов
gs-balanceui-shop-buy-token-antag-laughter-demon = Купить токен на демона смеха - { $price } Коинов
gs-balanceui-shop-buy-token-antag-ninja = Купить токен на ниндзю - { $price } Коинов
gs-balanceui-shop-buy-token-antag-wraith = Купить токен на фантома - { $price } Коинов
gs-balanceui-shop-buy-token-antag-lone-ops = Купить токен на одиночного оперативника - { $price } Коинов
gs-balanceui-shop-buy-token-antag-slasher = Купить токен на маньяка - { $price } Коинов
gs-balanceui-shop-buy-token-antag-slaughter-demon = Купить токен на демона резни - { $price } Коинов

gs-balanceui-shop-token-antag-derelict-cyborg = Токен на малф борга
gs-balanceui-shop-token-antag-lone-abductor = Токен на одинокого абдуктора
gs-balanceui-shop-token-antag-bingle = Токен на бинглов
gs-balanceui-shop-token-antag-blob = Токен на блоба
gs-balanceui-shop-token-antag-contractor = Токен на контрактника
gs-balanceui-shop-token-antag-dragon = Токен на дракона
gs-balanceui-shop-token-antag-hastur = Токен на короля в желтом
gs-balanceui-shop-token-antag-laughter-demon = Токен на демона смеха
gs-balanceui-shop-token-antag-ninja = Токен на ниндзю
gs-balanceui-shop-token-antag-wraith = Токен на фантома
gs-balanceui-shop-token-antag-lone-ops = Токен на одиночного оперативника
gs-balanceui-shop-token-antag-slasher = Токен на маньяка
gs-balanceui-shop-token-antag-slaughter-demon = Токен на демона резни

gs-balanceui-remark-token-antag-derelict-cyborg = Куплен токен на малф борга.
gs-balanceui-remark-token-antag-lone-abductor = Куплен токен на одинокого абдуктора.
gs-balanceui-remark-token-antag-bingle = Куплен токен на бинглов.
gs-balanceui-remark-token-antag-blob = Куплен токен на блоба.
gs-balanceui-remark-token-antag-contractor = Куплен токен на контрактника.
gs-balanceui-remark-token-antag-dragon = Куплен токен на дракона.
gs-balanceui-remark-token-antag-hastur = Куплен токен на короля в желтом.
gs-balanceui-remark-token-antag-laughter-demon = Куплен токен на демона смеха.
gs-balanceui-remark-token-antag-ninja = Куплен токен на ниндзю.
gs-balanceui-remark-token-antag-wraith = Куплен токен на фантома.
gs-balanceui-remark-token-antag-lone-ops = Куплен токен на одиночного оперативника.
gs-balanceui-remark-token-antag-slasher = Куплен токен на маньяка.
gs-balanceui-remark-token-antag-slaughter-demon = Куплен токен на демона резни.

gs-balanceui-shop-click-confirm = Нажмите ещё раз для подтверждения
gs-balanceui-shop-purchased = Куплено { $item }
