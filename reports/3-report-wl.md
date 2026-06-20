# Watched Literals (task 3)

https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_watched.php

Watched literals svuj ucel splnily: na kazde instanci se solver podiva na min klauzuli nez s adjacency lists (sloupec prohlednuto je vzdy nizsi). Rozhodnuti i propagace zustavaji skoro stejne, takze oba pristupy prochazeji cca stejny strom. Uspora je v cistem DPLL ale jen mirna. Duvod asi je, ze bez uceni klauzuli mame jen puvodni klauzule - PHOLE sice ma i delsi klauzule (kazdy holub aspon do jedne diry), proto je rozdil vubec videt, ale chybi dlouhe naucene klauzule, na kterych adjacency lists ztraceji nejvic. Cas tomu vetsinou odpovida.

## Testovování

Statistiky jdou na stderr (`c statistiky:` ...), tabulku nize jsem posbiral skriptem co projel instance v obou konfiguracich pres `dpll`.

### Adjacency lists vs watched literals

```powershell
dotnet run --project src/SatSolver -c Release -- dpll benchmarks/phole/hole7.cnf --prop adj
dotnet run --project src/SatSolver -c Release -- dpll benchmarks/phole/hole7.cnf --prop watched
```

| instance | konfigurace | vysledek | cas [ms] | rozhodnuti | propagace | konflikty | prohlednuto |
|---|---|---|--:|--:|--:|--:|--:|
| hole6.cnf | adj | UNSAT | 4.9 | 3794 | 21813 | 1898 | 40402 |
| hole6.cnf | watched | UNSAT | 3.2 | 3794 | 21813 | 1898 | 36254 |
| hole7.cnf | adj | UNSAT | 39.1 | 40582 | 265646 | 20292 | 499378 |
| hole7.cnf | watched | UNSAT | 31.8 | 40582 | 265564 | 20292 | 439798 |
| hole8.cnf | adj | UNSAT | 197.3 | 503188 | 3788970 | 251595 | 7051308 |
| hole8.cnf | watched | UNSAT | 122.6 | 503188 | 3786974 | 251595 | 6037031 |
| hole9.cnf | adj | UNSAT | 1738.6 | 7096976 | 59632208 | 3548489 | 111520982 |
| hole9.cnf | watched | UNSAT | 1786.1 | 7096976 | 59593085 | 3548489 | 94267776 |
| ais10.cnf | adj | SAT | 0.1 | 103 | 1154 | 29 | 6065 |
| ais10.cnf | watched | SAT | 0.1 | 103 | 1143 | 29 | 4619 |
| ais12.cnf | adj | SAT | 0.2 | 182 | 2994 | 58 | 14306 |
| ais12.cnf | watched | SAT | 0.1 | 182 | 2952 | 58 | 10939 |
