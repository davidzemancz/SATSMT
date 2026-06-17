using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  SearchEngine - druha cast: analyza konfliktu, uceni, mazani klauzuli
// =====================================================================
// Tahle cast je presne ten algoritmus z prednasky 2 ("Find an Assertive
// Clause With 1-UIP"). Bylo to asi nejtezsi na pochopeni z celeho ukolu 5 -
// rezoluce konfliktni klauzule dozadu az k prvnimu UIP. Snazil jsem se to
// okomentovat poradne at se v tom za pul roku zase vyznam.
public sealed partial class SearchEngine
{
    // Buffery co znovu pouzivam, at nealokuju porad dokola - analyza konfliktu
    // je nejteplejsi cesta v CDCL, takze tady se nove listy fakt hodit nemaji.
    private readonly List<int> _markedVars = new();       // promenne ktere jsem oznackoval (_seen)
    private readonly List<int> _learnedScratch = new();   // rozpracovana naucena klauzule
    private readonly HashSet<int> _lbdLevels = new();      // ruzne levely pro vypocet LBD

    // Zvedne aktivitu promenne (VSIDS) a oznackuje ji jako videnou.
    private void BumpVariable(int var)
    {
        _seen[var] = true;
        _markedVars.Add(var);
        _heuristic.OnVariableBump(var);
    }

    // Analyza konfliktu rezoluci az k prvnimu UIP. Vrati naucenou (asertivni)
    // klauzuli, level kam backjumpnout a jeji LBD.
    //
    // Naucena klauzule ma na indexu 0 asertivni literal (1-UIP) a na indexu 1
    // literal s nejvyssim zbyvajicim levelem - tohle chce watched literals: po
    // skoku zpet je klauzule unit a vynuti literal z indexu 0. (Tohle me chvili
    // matlo, nez mi doslo proc to musi byt zrovna takhle serazene.)
    private (int[] learned, int backjump, int lbd) Analyze(Clause conflict)
    {
        int d = DecisionLevel;
        List<int> learned = _learnedScratch;
        learned.Clear();
        learned.Add(0); // index 0 si rezervuju pro asertivni literal, doplnim ho az nakonec
        _markedVars.Clear();

        int pathCount = 0;     // kolik mam jeste nevyresenych literalu na aktualnim levelu d
        int pivotVar = 0;      // promenna naposled rezolvovaneho literalu (0 = zatim zadna)
        Clause? clause = conflict;
        int trailIdx = _trail.Count - 1;
        int uipLiteral = 0;

        do
        {
            // kdyz rezolvuju naucenou klauzuli, zvednu ji aktivitu (signal ze je uzitecna)
            if (clause!.IsLearned)
                clause.Activity += 1;

            // rezolvuju literaly tehle klauzule do naucene (pivotni promennou preskocim)
            foreach (int q in clause.Literals)
            {
                int v = Literal.Var(q);
                if (v == pivotVar) continue;              // pivot se rezoluci vyrusi
                if (_seen[v] || _level[v] == 0) continue; // uz zpracovano / fakt z levelu 0
                BumpVariable(v);
                if (_level[v] == d)
                    pathCount++;                          // literal aktualniho levelu -> "cesta" ke konfliktu
                else
                    learned.Add(q);                       // literal nizsiho levelu -> rovnou do naucene klauzule
            }

            // najdu v trailu posledni oznackovany literal (= nejhlubsi literal levelu d)
            while (!_seen[Literal.Var(_trail[trailIdx])])
                trailIdx--;
            uipLiteral = _trail[trailIdx];
            pivotVar = Literal.Var(uipLiteral);
            _seen[pivotVar] = false;
            clause = _reason[pivotVar];
            trailIdx--;
            pathCount--;
        }
        while (pathCount > 0); // koncim jakmile zbyde jediny literal levelu d -> to je 1-UIP

        // asertivni literal = negace 1-UIP (UIP je v ohodnoceni true, v klauzuli ho chci jako false)
        learned[0] = Literal.Negate(uipLiteral);

        // lokalni minimalizace (self-subsuming resolution): vyhodim literaly co jsou
        // zbytecne, protoze jejich antecedent uz je "pokryty" zbytkem klauzule
        if (_options.EnableClauseMinimization)
            MinimizeLearned(learned);

        // urcim level pro backjump a literal s nejvyssim levelem dam na index 1
        int backjump = 0;
        int maxIdx = -1;
        for (int i = 1; i < learned.Count; i++)
        {
            int lv = _level[Literal.Var(learned[i])];
            if (lv > backjump) { backjump = lv; maxIdx = i; }
        }
        if (maxIdx > 1)
            (learned[1], learned[maxIdx]) = (learned[maxIdx], learned[1]);

        // LBD = pocet ruznych decision levelu mezi literaly klauzule
        _lbdLevels.Clear();
        foreach (int lit in learned)
            _lbdLevels.Add(_level[Literal.Var(lit)]);
        int lbd = _lbdLevels.Count;

        // uklid: vycistim znacky _seen at je priste zase cista
        foreach (int v in _markedVars)
            _seen[v] = false;

        return (learned.ToArray(), backjump, lbd);
    }

    // Lokalni minimalizace naucene klauzule. Literal (krome asertivniho) muzu
    // zahodit, kdyz kazdy literal jeho antecedentu je bud fakt z levelu 0, nebo
    // uz v klauzuli je (znacka _seen). Pak je tenhle literal zbytecny.
    private void MinimizeLearned(List<int> learned)
    {
        int write = 1; // index 0 (asertivni literal) zustava porad
        for (int read = 1; read < learned.Count; read++)
        {
            int lit = learned[read];
            Clause? reason = _reason[Literal.Var(lit)];
            bool redundant = reason != null;
            if (reason != null)
            {
                foreach (int q in reason.Literals)
                {
                    int v = Literal.Var(q);
                    if (v == Literal.Var(lit)) continue;
                    if (!_seen[v] && _level[v] > 0) { redundant = false; break; }
                }
            }
            if (!redundant)
                learned[write++] = lit; // literal neni zbytecny -> nechavam
        }
        learned.RemoveRange(write, learned.Count - write);
    }

    // Prida naucenou klauzuli, zaregistruje ji do propagatoru a vynuti asertivni literal.
    private void AddLearnedClause(int[] learned, int lbd)
    {
        _stats.LearnedClauses++;

        if (learned.Length == 1)
        {
            // jednoprvkova naucena klauzule: po backjumpu na level 0 je literal trvaly fakt
            Enqueue(learned[0], null);
            return;
        }

        var clause = new Clause(learned, isLearned: true) { Lbd = lbd };
        _learned.Add(clause);
        _propagator.AddClause(clause);
        Enqueue(learned[0], clause); // asertivni literal je na indexu 0
    }

    // Mazani naucenych klauzuli (politika podle LBD, inspirovano Glucose). Permanentni
    // necham klauzule s LBD <= 2 a "locked" (ty co jsou prave neceho duvodem). Ze zbytku
    // smazu polovinu tech s nejvyssim LBD (a pri shode s nejnizsi aktivitou).
    private void ReduceClauseDatabase()
    {
        // locked = klauzule co jsou prave ted antecedentem nejakeho prirazeni na trailu
        var locked = new HashSet<Clause>();
        foreach (int lit in _trail)
        {
            Clause? r = _reason[Literal.Var(lit)];
            if (r != null)
                locked.Add(r);
        }

        var removable = _learned
            .Where(c => !locked.Contains(c) && c.Lbd > 2)
            .ToList();

        // nejmin uzitecne napred: vysoke LBD, pri shode nizka aktivita
        removable.Sort((a, b) =>
        {
            int cmp = b.Lbd.CompareTo(a.Lbd);
            return cmp != 0 ? cmp : a.Activity.CompareTo(b.Activity);
        });

        int toRemove = removable.Count / 2;
        for (int i = 0; i < toRemove; i++)
            removable[i].Deleted = true;

        _learned.RemoveAll(c => c.Deleted);
        _stats.DeletedClauses += toRemove;
    }
}
