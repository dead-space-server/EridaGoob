cmd-stationgoal-desc = Управляет целями станций.
cmd-stationgoal-help = Использование:
    stationgoal list — станции, их цели, прогресс и принудительный результат
    stationgoal set <EntityUid станции> <ID цели|random> — выдать цель и отправить на факсы
    stationgoal clear <EntityUid станции> — снять цель
    stationgoal resend <EntityUid станции> — повторно отправить цель на факсы
    stationgoal result <EntityUid станции> <true|false|auto> — принудительно задать результат
cmd-stationgoal-arg-subcommand = <list|set|clear|resend|result>
cmd-stationgoal-arg-station = <EntityUid станции>
cmd-stationgoal-arg-goal = <ID цели или random>
cmd-stationgoal-arg-result = <true — выполнена, false — провалена, auto — по условиям>
cmd-stationgoal-invalid-station = { $value } не является станцией с целями.
cmd-stationgoal-invalid-goal = Цели с ID { $value } не существует.
cmd-stationgoal-invalid-result = Неверный результат { $value }, ожидается true, false или auto.
cmd-stationgoal-no-goal = У станции нет цели.
cmd-stationgoal-no-fitting-goal = Ни одна цель не подходит станции по составу экипажа и спискам.
cmd-stationgoal-no-faxes = Ни один факс станции не получил цель.
cmd-stationgoal-list-empty = Нет станций с целями.
cmd-stationgoal-list-no-goal = { $station }: цели нет
cmd-stationgoal-list-goal = { $station }: { $goal } [{ $id }], { $percent ->
        [-1] без условий
       *[other] прогресс { $percent }%
    }, результат: { $forced }
cmd-stationgoal-set-success = Цель { $goal } выдана, получили факсов: { $faxes }.
cmd-stationgoal-set-force-reset = Принудительный результат прежней цели сброшен.
cmd-stationgoal-clear-success = Цель снята.
cmd-stationgoal-resend-success = Цель отправлена повторно, получили факсов: { $faxes }.
cmd-stationgoal-result-success = Результат цели: { $result }.
