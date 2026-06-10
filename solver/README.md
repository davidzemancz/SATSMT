# SAT solver

## Build

    dotnet build src/SatSolver/SatSolver.csproj -c Release

## Spousteni

    dotnet run --project src/SatSolver -- formula2cnf [vstup] [vystup]

Prevede formuli v NNF (.sat) na CNF v DIMACS pres Tseitina.
Bez souboru cte stdin a pise na stdout. Prepinace --equiv (vychozi) / --implication.
