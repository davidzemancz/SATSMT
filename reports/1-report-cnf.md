# Tseitin Encoding and DIMACS Format (task 1)

Překlad formule v NNF do DIMACS CNF pomocí Tseitionvo kódování.
https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_tseitin.php

## Benchmark

Defaultní nastavení s ekvivalencemi.
```powershell
cd solver
dotnet run --project src/SatSolver -c Release -- bench benchmarks/task1 --convert
```

Porovnani nastaveni s ekvivalencemi a pouze l2r implikacemi.
```powershell
 dotnet run --project src/SatSolver -c Release -- bench benchmarks/task1 --convert --configs "equiv:--equiv;impl:--implication"
```

