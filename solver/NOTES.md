# Poznamky k projektu (SAT solver)

Osobni TODO, nic oficialniho. Pak smazat.

## Plan podle ukolu
- [ ] ukol 1: Tseitin NNF->CNF + DIMACS writer
- [ ] ukol 2: DPLL (unit propagace, backtracking)
- [ ] ukol 3: watched literals
- [ ] ukol 4: CDCL (1-UIP uceni, restarty)
- [ ] ukol 5: heuristiky (VSIDS) + assumptions

## Napady
- literal jako int (DIMACS konvence), promenne 1..n
- klauzule = int[] (kvuli watched bude potreba menit poradi literalu)
