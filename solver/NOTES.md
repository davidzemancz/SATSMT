# Poznamky k projektu (SAT solver)

Osobni TODO, nic oficialniho. Pak smazat.

## Plan podle ukolu
- [x] ukol 1: Tseitin NNF->CNF + DIMACS writer
- [x] ukol 2: DPLL (iterativni trail, nakonec lepsi nez rekurze)
- [x] ukol 3: watched literals (prvni verze blbe prehazovala watche, prepsano)
- [x] ukol 4: CDCL - 1-UIP, minimalizace, mazani podle LBD, restarty
- [ ] ukol 5: heuristiky (VSIDS) + assumptions

## CDCL poznamky
- 1-UIP: rezoluce dozadu dokud nezbyde jediny literal aktualniho levelu
- asertivni literal dat na index 0, druhy nejvyssi level na index 1 (kvuli watched)
- LBD = pocet ruznych decision levelu, mazat klauzule s vysokym LBD
- restarty: Luby vs geometricke (Luby vychazi nepatrne lip)

## TODO pred odevzdanim
- smazat DebugDump a Preprocessor (mrtvy kod)
- smazat tyhle poznamky
- prejmenovat reporty na cislovane (1-report-...)
