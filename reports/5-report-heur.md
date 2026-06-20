# Decision Heuristics and Assumptions (task 5)

Rozhodovaci heuristiky a predpoklady (assumptions).
https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_decision.php

Porovnavam dve heuristiky proti sobe a proti nahodnemu vyberu:
- **vsids** (Variable State Independent Decaying Sum) - dynamicka, conflict-driven; skore proměnnych roste, kdyz se objevi v konfliktu, a casem snizuje.
- **jw** (Jeroslow-Wang) - staticka, spocita se jednou; preferuje literaly z kratkych klauzuli `J(l) = sum_{C obsahuje l} 2^(-|C|)`,
- **random** - nahodna promenna i faze (srovnavaci zaklad),
- **first** - prvni nepriraena promenna (trivialni zaklad).

Vsechny testy bezi na CDCL na defaultnim setupu (watched literals, Luby restarty, uceni + mazani).

Netrivialni heuristiky jasne porazi random i first, a cim tezsi instance, tim vetsi rozdil. Na splnitelnych uf100 je jw/vsids cca 4-6x rychlejsi nez random/first, na nesplnitelnych uuf100 je vsids dokonce je ~46x rychlejsi nez random. Na SAT je nejlepsi jw. Staticke skore podle delek klauzuli dobre nasmeruje hledani modelu a nestoji skoro nic. Na UNSAT je to ale naopak a vede vsids (1.4x rychlejsi nez jw, 6x nez first, 46x nez random). Random je nejhorsi hlavne na UNSAT, kde se hledani bez jakekoli struktury utopi (3.3M prohlednutych klauzuli). 

## Testovování

### Splnitelny 3SAT (uf100)

```powershell
dotnet run --project src/SatSolver -c Release -- bench benchmarks/rnd3sat/uf100 --avg --timeout 15 --limit 30 --configs "vsids:--cdcl --heuristic vsids;jw:--cdcl --heuristic jw;random:--cdcl --heuristic random --seed 1;first:--cdcl --heuristic first"
```

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| vsids | 30 | 30 | 0 | 0 | 1.4 | 272.2 | 6464.4 | 211.1 | 22590.9 |
| jw | 30 | 30 | 0 | 0 | 1.0 | 207.4 | 4194.2 | 135.5 | 14995.9 |
| random | 30 | 30 | 0 | 0 | 5.8 | 1339.0 | 22178.8 | 769.9 | 91655.6 |
| first | 30 | 30 | 0 | 0 | 5.9 | 1288.8 | 27374.0 | 896.9 | 107790.1 |

### Nesplnitelny 3SAT (uuf100)

```powershell
dotnet run --project src/SatSolver -c Release -- bench benchmarks/rnd3sat/uuf100 --avg --timeout 15 --limit 30 --configs "vsids:--cdcl --heuristic vsids;jw:--cdcl --heuristic jw;random:--cdcl --heuristic random --seed 1;first:--cdcl --heuristic first"
```

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| vsids | 30 | 0 | 30 | 0 | 2.4 | 571.3 | 14262.7 | 475.4 | 51173.5 |
| jw | 30 | 0 | 30 | 0 | 3.5 | 887.9 | 21023.6 | 693.5 | 78722.4 |
| first | 30 | 0 | 30 | 0 | 15.9 | 4120.3 | 90747.4 | 2991.7 | 435402.4 |
| random | 30 | 0 | 30 | 0 | 109.7 | 20444.6 | 376538.8 | 13057.4 | 3320427.4 |

## Predpoklady (assumptions)

Parametr (`--assume`). 
Pouziti je, ze naucene klauzule jsou odvozene jen z formule, ne z predpokladu, takze je lze mezi behy s ruznymi predpoklady **zachovat**. Literaly z `--assume` se spotrebuji jako rozhodnuti (jedno na uroven) jeste pred heuristikou. Kdyz je nejaky predpoklad v rozporu s formuli, vyjde UNSAT pod predpoklady.

Priklad na `(x1 v x2)` (`p cnf 2 1` / `1 2 0`):

```powershell
dotnet run --project src/SatSolver -c Release -- solve assume_demo.cnf --assume "1"     # SAT, model: 1 -2 0  (vynuti x1=1)
dotnet run --project src/SatSolver -c Release -- solve assume_demo.cnf --assume "-1"    # SAT, model: -1 2 0  (vynuti x1=0)
dotnet run --project src/SatSolver -c Release -- solve assume_demo.cnf --assume "1 -1"  # UNSAT (protichudne predpoklady)

