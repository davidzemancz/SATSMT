using SatSolver.Cnf;

namespace SatSolver.Benchmarks;

// =====================================================================
//  InstanceGenerators - generovani CNF instanci
// =====================================================================
// Hodi se na dve veci: do testu (vim dopredu jestli to ma vyjit SAT/UNSAT) a
// jako benchmarky. Pigeonhole ma navic dlouhe klauzule, takze je na nem hezky
// videt rozdil mezi adjacency lists a watched literals.
public static class InstanceGenerators
{
    // Pigeonhole princip PHP(holes+1, holes): holes+1 holubu, holes der. Vzdycky NESPLNITELNE
    // (vic holubu nez der -> nejde je rozmistit tak aby kazdy byl sam v diry).
    // Promenna p(i,h) = "holub i sedi v diry h", index = (i-1)*holes + h.
    public static CnfFormula PigeonHole(int holes)
    {
        int pigeons = holes + 1;
        var cnf = new CnfFormula();
        int Var(int pigeon, int hole) => (pigeon - 1) * holes + hole; // lokalni funkce na vypocet indexu

        // kazdy holub musi byt aspon v jedne diry (dlouha klauzule delky `holes`)
        for (int i = 1; i <= pigeons; i++)
        {
            var lits = new int[holes];
            for (int h = 1; h <= holes; h++)
                lits[h - 1] = Var(i, h);
            cnf.AddClause(new Clause(lits));
        }

        // zadni dva holubi nesmi byt ve stejne diry
        for (int h = 1; h <= holes; h++)
            for (int i = 1; i <= pigeons; i++)
                for (int j = i + 1; j <= pigeons; j++)
                    cnf.AddClause(new Clause(new[] { -Var(i, h), -Var(j, h) }));

        return cnf;
    }

    // Nahodna k-SAT instance: `clauses` klauzuli, kazda s k ruznymi promennymi a
    // nahodnymi polaritami. U pomeru klauzule/promenne kolem 4.26 (pro 3-SAT) jsou
    // instance nejtezsi - to je takovy ten slavny "phase transition".
    public static CnfFormula RandomKSat(int vars, int clauses, int k, int seed)
    {
        var rng = new Random(seed);
        var cnf = new CnfFormula { VariableCount = vars };
        var chosen = new HashSet<int>(); // promenne uz pouzite v aktualni klauzuli (at se neopakuji)

        for (int c = 0; c < clauses; c++)
        {
            chosen.Clear();
            var lits = new int[k];
            int idx = 0;
            while (idx < k)
            {
                int v = rng.Next(1, vars + 1);
                if (!chosen.Add(v))
                    continue; // tahle promenna uz v klauzuli je, losuju znova
                lits[idx++] = rng.Next(2) == 0 ? v : -v; // nahodna polarita
            }
            cnf.AddClause(new Clause(lits));
        }
        return cnf;
    }
}
