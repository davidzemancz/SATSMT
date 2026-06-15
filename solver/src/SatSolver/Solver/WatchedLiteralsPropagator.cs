using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  WatchedLiteralsPropagator - PRVNI POKUS
// =====================================================================
// Watched literals (Chaff, 2001): kazda klauzule sleduje jen dva svoje literaly
// (indexy 0 a 1). O klauzuli se zajimam jen kdyz nektery sledovany zfalsovatel.
//
// Tahle verze funguje, ale je neohrabana: pri prochazeni watchlistu si delam
// jeho kopii a sledovani prehazuju pres Remove/Add do Listu, coz je pomale
// (Remove je O(n)). Pozdeji prepsano na poradny in-place uklid watchlistu.
public sealed class WatchedLiteralsPropagator : IPropagator
{
    private SearchEngine _engine = null!;
    private List<Clause>[] _watch = null!; // _watch[idx(l)] = klauzule co sleduji literal l
    private long _checked;

    public long ClausesChecked => _checked;

    public void Initialize(SearchEngine engine, CnfFormula formula)
    {
        _engine = engine;
        _watch = new List<Clause>[Literal.IndexArraySize(formula.VariableCount)];
        for (int i = 0; i < _watch.Length; i++)
            _watch[i] = new List<Clause>();
    }

    // Predpoklad: klauzule ma aspon dva literaly (unit nelze sledovat dvema watchi).
    public void AddClause(Clause clause)
    {
        _watch[Literal.ToIndex(clause.Literals[0])].Add(clause);
        _watch[Literal.ToIndex(clause.Literals[1])].Add(clause);
    }

    public Clause? Propagate()
    {
        List<int> trail = _engine.Trail;
        while (_engine.QHead < trail.Count)
        {
            int assigned = trail[_engine.QHead];
            _engine.QHead++;

            int falseLit = Literal.Negate(assigned);

            // kopie watchlistu, abych ho mohl behem iterace menit (vim ze je to pomale)
            var ws = new List<Clause>(_watch[Literal.ToIndex(falseLit)]);
            foreach (Clause c in ws)
            {
                if (c.Deleted)
                    continue;
                _checked++;

                // sledovany co zfalsovatel dam na pozici 1, ten druhy je pak na [0]
                if (c.Literals[0] == falseLit)
                    (c.Literals[0], c.Literals[1]) = (c.Literals[1], c.Literals[0]);

                int other = c.Literals[0];
                if (_engine.LiteralIsTrue(other))
                    continue; // klauzule splnena, sledovani necham byt

                // hledam novy nezfalsovany literal na ktery prehodim sledovani
                bool found = false;
                for (int k = 2; k < c.Literals.Length; k++)
                {
                    if (!_engine.LiteralIsFalse(c.Literals[k]))
                    {
                        _watch[Literal.ToIndex(falseLit)].Remove(c); // POMALE: O(n) hledani v Listu
                        c.Literals[1] = c.Literals[k];
                        c.Literals[k] = falseLit;
                        _watch[Literal.ToIndex(c.Literals[1])].Add(c);
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;

                // nic jsem nenasel -> unit nebo konflikt
                if (_engine.LiteralIsFalse(other))
                    return c; // konflikt
                _engine.Enqueue(other, c); // unit -> vynutim druhy literal
            }
        }
        return null;
    }
}
