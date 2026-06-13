# SAT solver

## Build

    dotnet build src/SatSolver/SatSolver.csproj -c Release

## Spousteni

    dotnet run --project src/SatSolver -- <prikaz> [args]

Prikazy:
- formula2cnf [vstup] [vystup] - prevod NNF (.sat) na CNF (DIMACS)
- solve [vstup]                - vyresi SAT (DPLL), vypise SAT/UNSAT (+ model)
- dpll [vstup]                 - zkratka pro zakladni DPLL

Format vstupu se pozna podle pripony (.cnf = DIMACS, .sat = SMT-LIB).
