# CDCL (task 4)

https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_cdcl.php

Nejvic je videt, ze cele CDCL stoji a pada na uceni klauzuli. Bez nej (no-learn) to spadne na cisty DPLL a je to radove horsi - na nesplnitelnych uuf100 ~800x pomalejsi, na splnitelnych uf100 jen ~25x. Na UNSAT musi solver projit cely strom a naucene klauzule prorezavaji opakovane konflikty, kdezto SAT instanci nekdy najdes i stastnym pruchodem bez uceni. Vsechny ostatni komponenty delaji na takhle malych instancich (100 promennych) jen male rozdily, ale vsechny mirne pomahaji - nejlip je to videt na sloupci prohlednuto. Z nich nejvic zaberou restarty (no-restart ma na uuf100 nejvic rozhodnuti, konfliktu i casu, 2.7 ms), Luby vs geometricke jsou prakticky nerozeznatelne. Minimalizace a mazani podle LBD ubiraji konflikty resp. prohlednuto, ale do casu se to na teto velikosti skoro nepromitne. Phase saving je zajimavy, protozye na UNSAT lehce pomaha, ale na SAT instancich je dokonce mirne kontraproduktivni (no-phase ma nejmin rozhodnuti i konfliktu). Asi proto, ze tlaci hledani porad do stejne casti prostoru, coz pri hledani jednoho modelu nemusi dobre fungovat. Vychozi konfigurace (vse zapnuto, Luby) je tak rozumny default, akorat 100 promennych je jeste maly na to, aby se ladeni vedlejsich heuristik fakt projevilo na case.

## Testovování

Statistiky jdou na stderr (`c statistiky:` ...), tabulky nize jsem posbiral skriptem co projel instance pres `solve` a zprumeroval. Konfigurace (sloupec): vse = default `--cdcl`, geom = `--restart geom`, no-min = `--no-minimize`, no-del = `--no-delete`, no-phase = `--no-phase-saving`, no-restart = `--restart none`, no-learn = `--no-learn`.

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

### Splnitelny 3SAT

```powershell
Get-ChildItem benchmarks/rnd3sat/uf100/*.cnf | Select-Object -First 30 | ForEach-Object {
  dotnet run --project src/SatSolver -c Release -- solve $_.FullName --cdcl
}
```

| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| vse | 30 | 30 | 0 | 0 | 1.2 | 272.2 | 6464.4 | 211.1 | 22590.9 |
| geom | 30 | 30 | 0 | 0 | 1.0 | 267.0 | 6302.9 | 206.5 | 21999.2 |
| no-min | 30 | 30 | 0 | 0 | 1.0 | 276.2 | 6395.9 | 212.5 | 22236.8 |
| no-del | 30 | 30 | 0 | 0 | 1.0 | 260.8 | 6175.4 | 202.1 | 23564.9 |
| no-phase | 30 | 30 | 0 | 0 | 1.0 | 241.6 | 5693.0 | 188.6 | 20809.1 |
| no-restart | 30 | 30 | 0 | 0 | 1.4 | 323.7 | 7977.9 | 262.6 | 28572.3 |
| no-learn | 30 | 30 | 0 | 0 | 30.6 | 35698.4 | 392552.3 | 17458.8 | 1020397.9 |

### SW100
https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/SW-GCP/sw100-8-lp0-c5.tar.gz

Sw100 jsou pro CDCL skoro trivialni — prumerne jen ~3 konflikty  instanci, takze mazani, minimalizace, restarty ani typ restartu nemaji co delat a vsechny radky cca vyjdou identicky. Rozdil je videt jen u no-learn (cisty DPLL), kde bez uceni naroste pocet konfliktu na ~300.

```powershell
Get-ChildItem benchmarks/sw100/*.cnf | ForEach-Object {
  dotnet run --project src/SatSolver -c Release -- solve $_.FullName --cdcl
}
```

 | konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| vse | 100 | 100 | 0 | 0 | 0.1 | 158.8 | 521.1 | 3.3 | 2036.8 |
| geom | 100 | 100 | 0 | 0 | 0.1 | 158.8 | 521.1 | 3.3 | 2036.8 |
| no-min | 100 | 100 | 0 | 0 | 0.1 | 158.8 | 521.1 | 3.3 | 2036.9 |
| no-d  el | 100 | 100 | 0 | 0 | 0.1 | 158.8 | 521.1 | 3.3 | 2036.8 |
| no-phase | 100 | 100 | 0 | 0 | 0.1 | 159.4 | 505.8 | 3.1 | 1986.5 |
| no-restart | 100 | 100 | 0 | 0 | 0.1 | 158.8 | 521.1 | 3.3 | 2036.8 |
| no-learn | 100 | 100 | 0 | 0 | 1.1 | 939.2 | 10304.0 | 302.0 | 36672.9 |
