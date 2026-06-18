# CDCL (task 4)

https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_cdcl.php

## Testovování

Statistiky jdou na stderr (`c statistiky:` ...), tabulku nize jsem posbiral skriptem co projel instance pres `solve` a zprumeroval. Konfigurace (sloupec): vse = default `--cdcl`, geom = `--restart geom`, no-min = `--no-minimize`, no-del = `--no-delete`, no-phase = `--no-phase-saving`, no-restart = `--restart none`, no-learn = `--no-learn`.

### Nesplnitelny 3SAT

```powershell
Get-ChildItem benchmarks/rnd3sat/uuf100/*.cnf | Select-Object -First 30 | ForEach-Object {
  dotnet run --project src/SatSolver -c Release -- solve $_.FullName --cdcl
}
```

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| vse | 30 | 0 | 30 | 0 | 2.3 | 571.3 | 14262.7 | 475.4 | 51173.5 |
| geom | 30 | 0 | 30 | 0 | 2.2 | 559.8 | 14151.8 | 469.9 | 50808.2 |
| no-min | 30 | 0 | 30 | 0 | 2.3 | 587.9 | 14673.8 | 492.9 | 53051.5 |
| no-del | 30 | 0 | 30 | 0 | 2.3 | 553.6 | 13874.6 | 463.9 | 60488.2 |
| no-phase | 30 | 0 | 30 | 0 | 2.3 | 568.5 | 14408.1 | 481.5 | 55284.9 |
| no-restart | 30 | 0 | 30 | 0 | 2.7 | 666.5 | 17097.6 | 570.7 | 62958.8 |
| no-learn | 30 | 0 | 30 | 0 | 1603.4 | 2205010.2 | 24054070.4 | 1089647.0 | 61882320.9 |
