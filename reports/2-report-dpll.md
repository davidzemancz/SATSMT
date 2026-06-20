# DPLL Algorithm (task 2)

DPLL algoritmus
https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_dpll.php

Cas reseni roste s velikosti instance zhruba exponencialne, coz sedi s tim, ze DPLL prohledava strom rozhodnuti. U AIS skoci cas z 1,5 ms (ais6) na 42,5 ms (ais10), tedy radove vic na kazdy krok. Nejhur dopadaji nesplnitelne PHOLE instance, kde se musi projit cely strom (hole6 6,3 ms vs hole7 43,2 ms). U nahodnych 3-SAT roste prumer z 0,1 ms (uf20) na 34,8 ms (uf100), pokazde asi sestkrat. Pocet rozhodnuti a propagaci jde porad ruku v ruce s casem, takze rozhoduje velikost stromu, ne rezie jednoho kroku.

## Testovování

Statistiky jdou na stderr (`c statistiky:` ...), tabulky nize jsem posbiral skriptem co projel instance pres `dpll` a stderr zprumeroval.

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

### AIS

```powershell
dotnet run --project src/SatSolver -c Release -- dpll benchmarks/ais/ais10.cnf --prop adj
```

| instance | konfigurace | vysledek | cas [ms] | rozhodnuti | propagace | konflikty | prohlednuto |
|---|---|---|--:|--:|--:|--:|--:|
| ais6.cnf | dpll | SAT | 1.5 | 57 | 515 | 27 | 3234 |
| ais8.cnf | dpll | SAT | 2.4 | 550 | 7423 | 273 | 59100 |
| ais10.cnf | dpll | SAT | 42.5 | 8973 | 138777 | 4484 | 1270788 |

### PHOLE

```powershell
dotnet run --project src/SatSolver -c Release -- dpll benchmarks/phole/hole7.cnf --prop adj
```

| instance | konfigurace | vysledek | cas [ms] | rozhodnuti | propagace | konflikty | prohlednuto |
|---|---|---|--:|--:|--:|--:|--:|
| hole6.cnf | dpll | UNSAT | 6.3 | 6490 | 29322 | 3246 | 63161 |
| hole7.cnf | dpll | UNSAT | 43.2 | 65560 | 318495 | 32781 | 722098 |

### Random 3 SAT

```powershell
foreach ($c in 'uf20','uf50','uf75','uf100') {
  Write-Host "=== $c ==="
  Get-ChildItem "benchmarks/rnd3sat/$c/*.cnf" | Select-Object -First 50 | ForEach-Object {
    dotnet run --project src/SatSolver -c Release -- dpll $_.FullName --prop adj
  }
}
```
(prumery pres prvnich 50 instanci kazde velikosti, statistiky ze stderr)

#### uf20

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| dpll | 50 | 50 | 0 | 0 | 0.1 | 20.8 | 73.5 | 8.0 | 417.0 |

#### uf50

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| dpll | 50 | 50 | 0 | 0 | 1.0 | 533.2 | 3807.8 | 261.9 | 17732.2 |

#### uf75

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| dpll | 50 | 50 | 0 | 0 | 6.1 | 4758.1 | 46542.0 | 2372.3 | 210703.6 |

#### uf100

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| dpll | 50 | 50 | 0 | 0 | 34.8 | 35907.1 | 400906.3 | 17944.9 | 1790207.0 |
