using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  AdjacencyListPropagator - unit propagace pres occurrence listy
// =====================================================================
// Tohle je ten jednodussi/naivnejsi pristup (puvodne GRASP, 1997). Pro
// kazdy literal si pamatuju seznam klauzuli ve kterych se vyskytuje. Kdyz
// se literal stane false, projdu vsechny klauzule kde je jeho negace a u
// kazde znovu spocitam jak na tom je (splnena / unit / konflikt).
//
// Je to primocare a urcite spravne, ale dela se tu strasne moc zbytecne
// prace (porad prochazim cele klauzule dokola). Presne proto se to pak v
// ukolu 3 porovnava s watched literals - a tady se ukaze proc jsou watched
// rychlejsi. ClausesChecked pocita kolikrat jsem se na klauzuli musel kouknout.
public sealed class AdjacencyListPropagator : IPropagator
{
    private SearchEngine _engine = null!;
    private List<Clause>[] _occur = null!; // _occur[idx(l)] = klauzule co obsahuji literal l
    private long _checked;

    public long ClausesChecked => _checked;

    public void Initialize(SearchEngine engine, CnfFormula formula)
    {
        _engine = engine;
        _occur = new List<Clause>[Literal.IndexArraySize(formula.VariableCount)];
        for (int i = 0; i < _occur.Length; i++)
            _occur[i] = new List<Clause>();
        // klauzule dodava engine pres AddClause (trivialni si resi sam)
    }

    // Predpoklad: klauzule ma aspon dva literaly (viz IPropagator.AddClause).
    public void AddClause(Clause clause)
    {
        foreach (int lit in clause.Literals)
            _occur[Literal.ToIndex(lit)].Add(clause);
    }

    public Clause? Propagate()
    {
        List<int> trail = _engine.Trail;
        while (_engine.QHead < trail.Count)
        {
            int assigned = trail[_engine.QHead];
            _engine.QHead++;

            // Tim ze jsem `assigned` nastavil na true se jeho negace stala false;
            // takze me zajimaji klauzule kde se ta negace vyskytuje.
            int falseLit = Literal.Negate(assigned);
            List<Clause> clauses = _occur[Literal.ToIndex(falseLit)];

            foreach (Clause c in clauses)
            {
                if (c.Deleted)
                    continue; // smazane naucene klauzule preskakuju
                _checked++;

                // Projdu celou klauzuli: hledam pravdivy literal (=> splnena) a po
                // ceste pocitam kolik je neprirazenych.
                bool satisfied = false;
                int lastFreeLit = 0;
                int freeCount = 0;
                foreach (int lit in c.Literals)
                {
                    int val = _engine.LiteralValue(lit);
                    if (val > 0) { satisfied = true; break; }
                    if (val == 0)
                    {
                        freeCount++;
                        lastFreeLit = lit;
                        if (freeCount > 1)
                            break; // dva a vic volnych -> neni ani unit ani konflikt, nezajima
                    }
                }

                if (satisfied || freeCount > 1)
                    continue;
                if (freeCount == 0)
                    return c; // vsechny literaly false -> konflikt!
                // prave jeden volny literal -> klauzule je unit, vynutim ho
                _engine.Enqueue(lastFreeLit, c);
            }
        }
        return null; // dosli mi literaly na frontu a zadny konflikt nenastal
    }
}
