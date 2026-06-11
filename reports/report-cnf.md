# Tseitin Encoding and DIMACS Format (task 1)

Překlad formule v NNF do DIMACS CNF pomocí Tseitionvo kódování.
https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_tseitin.php

## Benchmark

Prevod NNF (.sat) na DIMACS CNF dela prikaz `formula2cnf`. Default pouziva ekvivalence, `--implication` vynuti jen l2r implikace.
```powershell
cd solver
dotnet run --project src/SatSolver -c Release -- formula2cnf benchmarks/task1/nested_8.sat
dotnet run --project src/SatSolver -c Release -- formula2cnf benchmarks/task1/nested_8.sat --implication
```

Pres vsechny instance v benchmarks/task1 a obe nastaveni jsem to prohnal kratkym foreach skriptem.
