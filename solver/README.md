# SAT solver

## Build

    dotnet build src/SatSolver/SatSolver.csproj -c Release

## Spousteni

    dotnet run --project src/SatSolver -- <prikaz> [args]

Prikazy:
- formula2cnf [vstup] [vystup] - prevod NNF (.sat) na CNF (DIMACS)
- solve [vstup]                - vyresi SAT, vypise SAT/UNSAT (+ model)
- dpll [vstup]                 - zkratka pro zakladni DPLL
- bench ...                    - davkove mereni na benchmarkach (do reportu)

Format vstupu se pozna podle pripony (.cnf = DIMACS, .sat = SMT-LIB).

Hlavni prepinace u solve:
- --dpll / --cdcl          (vychozi cdcl)
- --prop adj | watched
- --heuristic first | random | jw | vsids
- --restart none | geom | luby

## Struktura

- src/SatSolver/  zdrojaky (Cnf, Parsing, Encoding, Solver, Heuristics, Restarts, Cli)
- benchmarks/     testovaci instance
- examples/       male ukazky

Komentare v kodu jsou cesky a neformalne (bez diakritiky).
