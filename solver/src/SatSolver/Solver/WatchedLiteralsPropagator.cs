using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  WatchedLiteralsPropagator - unit propagace pres watched literals
// =====================================================================
// Tohle je ta chytra varianta (Chaff, 2001), dneska to maji vsechny solvery.
// Prinznam se ze tahle metoda mi zabrala asi nejvic casu z celeho ukolu, nez
// mi doslo proc to funguje :)
//
// Napad: kazda klauzule "sleduje" (watch) jen DVA svoje literaly (na indexech
// 0 a 1). O klauzuli se zajimam jedine kdyz nektery z tech dvou sledovanych
// zfalsovatel. Pak hledam jiny nezfalsovany literal na ktery sledovani prehodim.
// Kdyz zadny neni, je klauzule bud unit (jeden zbyly) nebo konflikt.
//
// Velka vyhoda oproti adjacency lists: pri backtrackingu NEMUSIM se sledovanim
// vubec nic delat (proste se odprirazene literaly zase "rozsviti"). Tim odpadne
// skoro vsechna zbytecna prace.
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
        // klauzule dodava engine pres AddClause (trivialni si resi sam)
    }

    // Predpoklad: klauzule ma aspon dva literaly (unit nelze sledovat dvema watchi).
    public void AddClause(Clause clause)
    {
        // sleduju prvni dva literaly
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

            // literal ktery se prave stal false (a kvuli kteremu musim checkovat klauzule)
            int falseLit = Literal.Negate(assigned);
            List<Clause> ws = _watch[Literal.ToIndex(falseLit)];

            // i = odkud ctu, j = kam zapisuju. Tohle je takovy in-place uklid watchlistu:
            // klauzule co u tohohle literalu zustanou prepisu dopredu, ostatni vypadnou.
            int i = 0, j = 0;
            Clause? conflict = null;

            while (i < ws.Count)
            {
                Clause c = ws[i];
                if (c.Deleted) { i++; continue; } // smazanou klauzuli ze sledovani vyhodim

                _checked++;

                // Trik: sledovany literal co zfalsovatel dam na pozici 1, ten druhy watch
                // tim padem skonci na pozici 0. Pak uz vzdycky vim ze "ten druhy" je [0].
                if (c.Literals[0] == falseLit)
                    (c.Literals[0], c.Literals[1]) = (c.Literals[1], c.Literals[0]);

                int other = c.Literals[0];

                // Kdyz je ten druhy watch uz true, klauzule je splnena -> necham sledovani byt.
                if (_engine.LiteralIsTrue(other))
                {
                    ws[j++] = c;
                    i++;
                    continue;
                }

                // Jinak hledam novy watch: nejaky nezfalsovany literal ve zbytku klauzule.
                bool foundNewWatch = false;
                for (int k = 2; k < c.Literals.Length; k++)
                {
                    if (!_engine.LiteralIsFalse(c.Literals[k]))
                    {
                        // prehodim sledovani z falseLit na ten novy literal
                        c.Literals[1] = c.Literals[k];
                        c.Literals[k] = falseLit;
                        _watch[Literal.ToIndex(c.Literals[1])].Add(c);
                        foundNewWatch = true;
                        break;
                    }
                }
                if (foundNewWatch)
                {
                    i++;
                    continue; // c uz nesleduje falseLit, takze ho do j NEkopiruju
                }

                // Nic jsem nenasel -> klauzule je unit nebo konflikt, sledovani falseLit nechavam.
                ws[j++] = c;
                i++;
                if (_engine.LiteralIsFalse(other))
                {
                    // konflikt: dokopiruju zbytek watchlistu (at o nic neprijdu) a vratim klauzuli
                    while (i < ws.Count)
                        ws[j++] = ws[i++];
                    conflict = c;
                    break;
                }
                // jinak je to unit -> vynutim ten druhy literal
                _engine.Enqueue(other, c);
            }

            ws.RemoveRange(j, ws.Count - j); // odriznu konec (klauzule co uz tu nesleduji)
            if (conflict != null)
                return conflict;
        }
        return null;
    }
}
