ui-options-hear-self = Слышать себя!
ui-options-voice-chat-volume = Громкость голосового чата
ui-options-function-voice-push-to-talk = Голосового чат: Режим рации (нажатие по кнопке) (рядом)
ui-options-function-voice-push-to-talk-radio = Голосового чат: Режим рации (нажатие по кнопке) (рация)

ui-escape-voice-chat = Голосовой чат

cmd-voicechat-desc = Показывает гайд по голосовому чату
cmd-voicechat-help = Использование: voicechat
cmd-voicechat-unavailable = Режим голосового чата выключен

voice-link-connected = Микрофон подключён.
voice-link-disconnected = Состояние микрофона будет указано на этой строке
voice-link-computer-title = На любом устройстве
voice-link-computer-text = Открой страницу голосового канала в браузере
voice-link-phone-title = С любого устройства
voice-link-phone-text = Зайди по ссылке и введи код:
voice-link-expiry = Не закрывай страницу, пока играешь. Код привязан, пока ты на сервере.
voice-link-loading = Обрабатываем ссылку...
voice-link-open = Открыть в браузере
voice-link-copy = Скопировать
voice-link-close = Закрыть

voice-guide-title = Голосовой чат
voice-guide-unbound = (клавиша не назначена)
voice-guide-intro = На данном сервере включен голосовой чат. Голос идет от твоего персонажа
voice-guide-talking-title = Речь
voice-guide-talking =
    Будешь далеко стоять от своего собеседника, то тебя будет плохо слышно. Абсолютно все обьекты глушат твой голос

    Чем громче твой голос, тем сильнее тебя будет слышно. Шёпот слышно буквально в паре тайлов.
    Голосовой режим не будет доступе состояния смерти, мута и прочих состояниях

    Зажми [bold]{$local}[/bold] и говори. Если режим рации не нравится, на странице голосового канала (в браузере) переключись на голосовую активацию.
voice-guide-radio-title = Рация
voice-guide-radio =
    Зажми [bold]{$radio}[/bold], чтобы говорить по рации. Рядом стоящие всё равно слышат тебя как обычно.

    Кнопка рации в чате переключает канал. Если стоит «Рядом», [bold]{$radio}[/bold] пойдёт на первый канал гарнитуры. Там же можно заглушить ненужные каналы и убавить громкость.

    При голосовой активации выбранный канал прилипает к рации и не убирается, так что не забудь переключить обратно на «Рядом». Рации в руках и интеркомы с включённым микрофоном тоже слышат, если говорить рядом.
voice-guide-station-title = По станции
voice-guide-station =
    Телефоны и голопады ловят всех рядом. Громкоговоритель слышно с нескольких тайлов. Режим транслировать теперь тоже работает.

    ИИ слышит и может говорить через камеры. Призраки могут общаться друг с другом, живые призраков не слышат
voice-guide-controls-title = Кнопки
voice-guide-controls =
    Микрофон и наушники слева от чата: первый глушит твой микрофон, второй отключает звук. По игроку можно ПКМ: заглушить его или подкрутить громкость.

    Громкость голоса в настройках, вкладка «Звук». Режим рации переназначается в управлении.
voice-guide-rules-title = Правила
voice-guide-rules =
    Те же правила, что и в текстовом чате, но с парой исключений:
    1. Микрофон используй по делу. Специально издавать страшные звуки разрывы вселенной не стоит, администрация имеет полное право тебя замутить до конца раунда. 
    2. SoundPad: только у клоуна
voice-guide-connect-title = Состояние голосового канала

cmd-voicemute-desc = Переключает, заглушён ли голосовой чат игрока.
cmd-voicemute-help = Использование: voicemute <username>
cmd-voicemute-hint = <username>
cmd-voicemute-muted = Голосовой чат для {$player} заглушён.
cmd-voicemute-unmuted = Голосовой для {$player} разглушен.

comms-console-menu-voice-broadcast-button = Голосовая трансляция
comms-console-menu-voice-broadcast-button-tooltip = Голосом на всю станцию, недолго.
comms-console-menu-voice-broadcast-stop = Закончить ({$seconds} с)
comms-console-menu-voice-broadcast-busy = Трансляция уже идёт
comms-console-menu-voice-broadcast-cooldown = Трансляция ({$seconds} с)

voice-broadcast-denied = Нет доступа.
voice-broadcast-not-connected = Сначала открой голосовой канал: Esc -> Голосовой чат.
voice-broadcast-started = Ты вещаешь на всю станцию, {$seconds} секунд.
voice-broadcast-ended = Трансляция закончена.

voice-radio-button-off = Рядом
voice-radio-button-tooltip = Рация голосового канала: канал для речи, что слышишь, громкость.

voice-verb-category = Голосовой чат
voice-verb-mute = Заглушить
voice-verb-unmute = Разглушить
voice-verb-volume = Громкость {$percent}%

voice-prompt-later = Не сейчас
voice-prompt-never = Больше не спрашивать

voice-speaker-unknown = Неизвестный
voice-route-radio = Рация
voice-route-telephone = Телефон
voice-route-holopad = Голопад
voice-route-camera = Камера
voice-route-broadcast = Трансляция
voice-speaker-self = Ты
voice-self-blocked = Не можешь говорить

voice-radio-popup-talk = Говорить
voice-radio-popup-hear = Слышать
voice-radio-popup-volume = Громкость
voice-radio-popup-volume-value = {$percent}%

voice-loudness-shout = Крик
voice-loudness-whisper = Шёпот
voice-loudness-megaphone = Громкоговоритель

admin-player-actions-window-voice-logs = Голосовые записи
cmd-voicelogs-desc = Открывает логи голосового взаимодействия
cmd-voicelogs-help = Использование: voicelogs [username]
cmd-voicelogs-hint = [username]
voice-logs-verb = Голосовые записи
voice-logs-title = Голосовые записи
voice-logs-round = Раунд
voice-logs-round-entry = Раунд {$id} · {$length}
voice-logs-round-current = Раунд {$id} (текущий) · {$length}
voice-logs-player = Игрок
voice-logs-add = Добавить
voice-logs-refresh = Обновить
voice-logs-play = Играть
voice-logs-pause = Пауза
voice-logs-fit = Вписать
voice-logs-loading = Загрузка...
voice-logs-empty = Выбери игрока, нажми «Добавить», и дорожка появится тут.
voice-logs-mute = Заглушить дорожку
voice-logs-solo = Соло
voice-logs-remove = Убрать дорожку
voice-logs-lobby = Лобби
voice-logs-blocked = {$name} (заглушён)

voice-god-verb = Голос Бога
voice-god-verb-stop = Остановить
voice-god-started = Ты говоришь с {$target} голосом Бога. Другие не слышат.
voice-god-started-offline = Говоришь с {$target} голосом Бога, но страница голосового чата закрыта. Открой: Esc -> Голосовой чат.
voice-god-hear-self = Себя тоже услышишь, с тем же свечением на экране.
voice-god-stopped = Голос Бога отключён.
voice-god-name = Голос
voice-god-log-name = Голос Бога -> {$target}
voice-route-god = Божественный
voice-self-god = Голос Бога
cmd-voiceofgod-desc = Говорит голосом Бога одному, всем рядом, отделу или всей станции. Запусти без аргументов, чтобы закончить.
cmd-voiceofgod-help = Использование: voiceofgod <username> [hearSelf] | voiceofgod radius <tiles> [hearSelf] | voiceofgod department <id> [hearSelf] | voiceofgod station [hearSelf]
cmd-voiceofgod-hint = <username> | radius | department | station
cmd-voiceofgod-radius-hint = <tiles>
cmd-voiceofgod-department-hint = <department>
cmd-voiceofgod-hear-self-hint = [hearSelf]
cmd-voiceofgod-bad-hear-self = hearSelf: true или false (слышать себя)
cmd-voiceofgod-bad-radius = Радиус в тайлах, от 1 до {$max}.
cmd-voiceofgod-bad-department = Нет такого отдела.
voice-god-target-radius = все в {$radius} тайлах от тебя
voice-god-target-department = отдел {$department}
voice-god-target-station = вся станция
voice-god-cue = В голове звучит чей-то голос...

voice-mic-offline = Голосовой чат не открыт. Кликни, чтобы открыть.
voice-mic-ready = Микрофон включён. Клик чтобы заглушить.
voice-mic-live = Ты в эфире. Клик чтобы заглушить.
voice-mic-blocked = Персонаж сейчас не может говорить.
voice-mic-muted = Микрофон заглушён. Клик чтобы вернуть.
voice-mic-deafened = Звук выключен. Клик чтобы вернуть звук и микрофон.
voice-deafen-off = Выключить звук: глушит абсолютно все звуки игроков и твой микрофон.
voice-deafen-on = Звук выключен. Клик чтобы включить
voice-radio-popup-mute-all = Заглушить все
voice-radio-popup-unmute-all = Вернуть все
