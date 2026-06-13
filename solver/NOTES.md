# Poznamky k projektu (SAT solver)

Osobni TODO, nic oficialniho. Pak smazat.

## Plan podle ukolu
- [x] ukol 1: Tseitin NNF->CNF + DIMACS writer  (hotovo)
- [ ] ukol 2: DPLL (unit propagace, backtracking)
- [ ] ukol 3: watched literals
- [ ] ukol 4: CDCL (1-UIP uceni, restarty)
- [ ] ukol 5: heuristiky (VSIDS) + assumptions

## DPLL - jak na to
- rekurzivne nebo iterativne? Zkusim nejdriv rekurzivne, je jednodussi.
  POZOR: na velkych instancich muze pretect stack -> mozna predelat na iterativni trail.
- statistiky: rozhodnuti, propagace, konflikty, cas
- debug: vypisovat trail a klauzule (DebugDump)

## Napady
- literal jako int (DIMACS konvence), promenne 1..n
- klauzule = int[] (kvuli watched bude potreba menit poradi literalu)
