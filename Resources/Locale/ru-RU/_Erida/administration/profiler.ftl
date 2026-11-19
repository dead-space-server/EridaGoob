cmd-erida_profiler-desc = Открывает профайлер сервера: медленные тики, время систем и ошибки из логов.
cmd-erida_profiler-help = Использование: erida_profiler

erida-profiler-window-title = Профайлер сервера
erida-profiler-open = Профайлер
erida-profiler-copy = Копировать отчёт
erida-profiler-reset = Сбросить историю
erida-profiler-disabled = Сбор выключен: erida.profiler.enabled равен false.

erida-profiler-tab-systems = Системы
erida-profiler-tab-slow-ticks = Медленные тики
erida-profiler-tab-logs = Ошибки

erida-profiler-overview-waiting = Сбор данных…
erida-profiler-overview = За {$seconds} с: тиков {$ticks}, в среднем {$avg} мс, макс. {$max} мс, медленных {$slow} | GC {$gc} мс (gen0 {$gen0}, gen1 {$gen1}, gen2 {$gen2}) | память {$memory} | сущностей {$entities} | игроков {$players}

erida-profiler-systems-hint = Время Update за последние 10 секунд. Время обработчиков событий засчитывается системе, в чьём Update событие поднято.
erida-profiler-column-name = Название
erida-profiler-column-total = Всего, мс
erida-profiler-column-avg = Сред., мс
erida-profiler-column-max = Макс., мс
erida-profiler-column-alloc = Аллок./вызов
erida-profiler-column-calls = Вызовов

erida-profiler-slow-ticks-threshold = Кадры дольше {$ms} мс
erida-profiler-slow-ticks-hint = Узлы быстрее 0,1 мс скрыты. Input — сетевые сообщения, Update — в основном PVS. Если пауза GC большая, а медленной системы нет, виноват сборщик мусора.
erida-profiler-slow-ticks-empty = Медленных тиков пока нет.
erida-profiler-slow-ticks-select = Выберите медленный тик слева.
erida-profiler-slow-tick-item = {$time} · #{$tick} · {$ms} мс
erida-profiler-slow-tick-summary = {$time}, тик #{$tick}: кадр {$ms} мс, тиков в кадре {$ticks}, пауза GC ≈{$gc} мс
erida-profiler-slow-tick-truncated = (дерево обрезано)
erida-profiler-node = {$name} — {$ms} мс, {$alloc}

erida-profiler-level-warning = Предупреждения
erida-profiler-level-error = Ошибки
erida-profiler-level-fatal = Фатальные
erida-profiler-logs-empty = В логах пусто.
erida-profiler-log-seen = Впервые {$first}, последний раз {$last}, {$count} раз
