# DPLL Algorithm (task 2)

DPLL algoritmus
https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_dpll.php

## Testovování

Statistiky jdou na stderr (`c statistiky:` ...), tabulku nize jsem posbiral skriptem co projel instance pres `dpll`.

```powershell
cd solver
```

### Examples z Task 1

```powershell
dotnet run --project src/SatSolver -c Release -- dpll examples/task1/nested_8.sat --prop adj
```
| instance | konfigurace | vysledek | cas [ms] | rozhodnuti | propagace | konflikty | prohlednuto |
|---|---|---|--:|--:|--:|--:|--:|
| nested_5.sat | dpll | SAT | 0.8 | 6 | 16 | 0 | 50 |
| nested_8.sat | dpll | SAT | 0.7 | 65 | 1597 | 31 | 4808 |
| toy_100.sat | dpll | SAT | 0.1 | 100 | 99 | 0 | 395 |
| toy_5.sat | dpll | SAT | 0.0 | 5 | 4 | 0 | 15 |
| toy_50.sat | dpll | SAT | 0.0 | 50 | 49 | 0 | 195 |
