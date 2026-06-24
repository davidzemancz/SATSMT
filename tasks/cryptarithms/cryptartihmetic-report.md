# Kryptoaritmetika

Reseno mym c# solverem.

Hadanku resim **bit-blastingem do CNF**. Bezne hadanky v bazi 10 jsou pod 0.3 s. Klicove je, ze pro bit-blasting nerozhoduje pocet pismen, ale velikost aritmetiky. Napriklad problem SO+MANY+...=TESTS (41 scitancu, 9961 promennych) trva ~97 s. Prosta funkce pismeno→cislice je volitelna (`--allow-dup`).

## Testovani

### Na vyzkouseni

```powershell
python cryptarithms.py "SEND + MORE = MONEY"                 # najde a vypise reseni
python cryptarithms.py "SEND + MORE = MONEY" --unique         # je reseni jednoznacne?
python cryptarithms.py "SEND + MORE = MONEY" --allow-dup --count   # spocita vsechna reseni
python cryptarithms.py "ABC + DEF = GHIJ" --base 16            # jina baze
```

### Becnhmark

```powershell
python bench.py run --budget 120 --csv results/timings.csv
```

### Najit reseni a dokazat jednoznacnost

| instance | mysolver | vysledek |
|---|--:|---|
| SEND + MORE = MONEY | 0.30 s | jednoznacne |
| COCA + COLA = OASIS | 0.27 s | jednoznacne |
| FORTY + TEN + TEN = SIXTY | 1.81 s | jednoznacne |
| DONALD + GERALD = ROBERT | 8.10 s | jednoznacne |
| (AB+CD=EF) && (BA+DC=FE) | 0.18 s | vic reseni |
| ABC + DEF = GHIJ (baze 16) | 0.18 s | vic reseni |

### Pocitani vsech reseni

```powershell
python cryptarithms.py "SEND + MORE = MONEY" --allow-dup --count
python cryptarithms.py "CP + IS + FUN = TRUE" --count
```

| instance | mysolver | pocet |
|---|--:|--:|
| ODD + ODD = EVEN | 0.30 s | 2 |
| CP + IS + FUN = TRUE | 9.20 s | 72 |
| SEND + MORE = MONEY (`--allow-dup`) | 15.74 s | 155 |

SEND+MORE=MONEY ma s prostym zobrazenim **jedine** reseni, ale s povolenymi duplicitami **155**. CP+IS+FUN=TRUE ma 10 ruznych pismen v bazi 10 (bijekce) a **72** reseni - oba pocty odpovidaji literature. Muj solver pocita pres opakovany spawn (jeden proces na reseni), takze cas roste primo s poctem reseni - proto u velkych vyctu trva nejdele.

### "SO + MANY + ... + TEN = TESTS

41 scitancu, 10 ruznych pismen (S,O,M,A,N,Y,R,E,T,H), v bazi 10. CNF ma 9961 promennych a 34349 klauzuli.

```powershell
python cryptarithms.py -f instances/tests.txt --dimacs results/tests.cnf
dotnet ../../solver/src/SatSolver/bin/Release/net10.0/SatSolver.dll solve results/tests.cnf --time-limit 600
python cryptarithms.py -f instances/tests.txt --unique
```

| operace | mysolver |
|---|--:|
| najit reseni | ~97 s |
| dokazat jednoznacnost (najit + UNSAT druheho) | ~290 s |

Reseni je **jednoznacne**: `A=7 E=0 H=5 M=2 N=6 O=1 R=8 S=3 T=9 Y=4`, tj. cely soucet dá **TESTS = 90393**. Dukaz jednoznacnosti = nalezeni reseni (~97 s) + dokazat, ze druhe neexistuje (UNSAT, ~193 s navic).

### Booleovske formule

```powershell
python cryptarithms.py "((TWO+TWO=FOUR) || (ONE+ONE=TWO)) && (NOT (ODD+ODD=EVEN))"
python cryptarithms.py "(SEND+MORE=MONEY) OR (SQUARE-DANCE=DANCER)" --allow-dup
```