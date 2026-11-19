cmd-erida_profiler-desc = Opens the server profiler: slow ticks, system timings and log errors.
cmd-erida_profiler-help = Usage: erida_profiler

erida-profiler-window-title = Server profiler
erida-profiler-open = Profiler
erida-profiler-copy = Copy report
erida-profiler-reset = Reset history
erida-profiler-disabled = Collection is disabled: erida.profiler.enabled is false.

erida-profiler-tab-systems = Systems
erida-profiler-tab-slow-ticks = Slow ticks
erida-profiler-tab-logs = Errors

erida-profiler-overview-waiting = Collecting data…
erida-profiler-overview = Last {$seconds} s: {$ticks} ticks, avg {$avg} ms, max {$max} ms, slow {$slow} | GC {$gc} ms (gen0 {$gen0}, gen1 {$gen1}, gen2 {$gen2}) | memory {$memory} | entities {$entities} | players {$players}

erida-profiler-systems-hint = Update time over the last 10 seconds. Time of event handlers counts toward the system whose update raised the event.
erida-profiler-column-name = Name
erida-profiler-column-total = Total, ms
erida-profiler-column-avg = Avg, ms
erida-profiler-column-max = Max, ms
erida-profiler-column-alloc = Alloc/call
erida-profiler-column-calls = Calls

erida-profiler-slow-ticks-threshold = Frames longer than {$ms} ms
erida-profiler-slow-ticks-hint = Nodes faster than 0.1 ms are hidden. Input is network messages, Update is mostly PVS. If a large GC pause has no slow system, the garbage collector is to blame.
erida-profiler-slow-ticks-empty = No slow ticks yet.
erida-profiler-slow-ticks-select = Select a slow tick on the left.
erida-profiler-slow-tick-item = {$time} · #{$tick} · {$ms} ms
erida-profiler-slow-tick-summary = {$time}, tick #{$tick}: frame {$ms} ms, ticks in frame {$ticks}, GC pause ≈{$gc} ms
erida-profiler-slow-tick-truncated = (tree truncated)
erida-profiler-node = {$name} — {$ms} ms, {$alloc}

erida-profiler-level-warning = Warnings
erida-profiler-level-error = Errors
erida-profiler-level-fatal = Fatal
erida-profiler-logs-empty = Nothing logged.
erida-profiler-log-seen = First {$first}, last {$last}, {$count} times
