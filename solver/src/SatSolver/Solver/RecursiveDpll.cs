using System.Diagnostics;
using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  RecursiveDpll - PRVNI VERZE solveru (rekurzivni DPLL)
// =====================================================================
// Nejjednodussi co me napadlo na ukol 2: rekurze pres promenne + naivni unit
// propagace (poporad scanuju vsechny klauzule, dokud se neco meni). Vetvim na
// prvni neprirazenou promennou, zkusim true, pak false.
//
// Funguje na malych instancich, ale ma dva problemy: (1) na vetsich instancich
// pretece zasobnik (hluboka rekurze), (2) propagace je pomala (kazdy krok projde
// uplne vsechny klauzule). Pozdeji jsem to cele prepsal na iterativni engine
// nad trailem + watched literals (viz SearchEngine).
public sealed class RecursiveDpll
{
    private readonly CnfFormula _formula;
    private readonly SolverOptions _options;
    private readonly SolverStatistics _stats = new();
    private readonly int _varCount;
    private readonly sbyte[] _value;   // 0 = neprirazeno, +1 = true, -1 = false
    private readonly Stopwatch _sw = new();
    private bool _timedOut;

    public RecursiveDpll(CnfFormula formula, SolverOptions options)
    {
        _formula = formula;
        _options = options;
        _varCount = formula.VariableCount;
        _value = new sbyte[_varCount + 1];
    }

    public SolverResult Solve()
    {
        _sw.Restart();
        bool sat = Search(0);
        _sw.Stop();
        _stats.SolveTime = _sw.Elapsed;

        if (_timedOut)
            return new SolverResult(SatResult.Unknown, null, _stats);

        bool[]? model = sat ? BuildModel() : null;
        return new SolverResult(sat ? SatResult.Satisfiable : SatResult.Unsatisfiable, model, _stats);
    }

    // Rekurzivni hledani: nejdriv propaguj, pak vetvi na prvni neprirazene promenne.
    private bool Search(int depth)
    {
        if (_options.TimeLimitSeconds > 0 && _sw.Elapsed.TotalSeconds > _options.TimeLimitSeconds)
        {
            _timedOut = true;
            return false;
        }
        if (depth > _stats.MaxDecisionLevel)
            _stats.MaxDecisionLevel = depth;

        if (!Propagate())
        {
            _stats.Conflicts++;
            return false; // konflikt na teto vetvi
        }

        int v = FirstUnassigned();
        if (v == 0)
            return true; // vsechno prirazeno a zadny konflikt -> SAT

        _stats.Decisions++;
        // zkus true, pri neuspechu vrat prirazeni a zkus false
        sbyte[] snapshot = (sbyte[])_value.Clone();
        _value[v] = 1;
        if (Search(depth + 1)) return true;
        Array.Copy(snapshot, _value, _value.Length);

        _value[v] = -1;
        if (Search(depth + 1)) return true;
        Array.Copy(snapshot, _value, _value.Length);
        return false;
    }

    // Naivni unit propagace: dokola scanuj vsechny klauzule. Vrati false pri konfliktu.
    private bool Propagate()
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Clause c in _formula.Clauses)
            {
                _stats.ClausesChecked++;
                bool satisfied = false;
                int freeCount = 0;
                int lastFree = 0;
                foreach (int lit in c.Literals)
                {
                    int val = Value(lit);
                    if (val > 0) { satisfied = true; break; }
                    if (val == 0) { freeCount++; lastFree = lit; }
                }
                if (satisfied) continue;
                if (freeCount == 0) return false;     // vsechny literaly false -> konflikt
                if (freeCount == 1)
                {
                    int var = Literal.Var(lastFree);
                    _value[var] = Literal.IsPositive(lastFree) ? (sbyte)1 : (sbyte)-1;
                    _stats.Propagations++;
                    changed = true;
                }
            }
        }
        return true;
    }

    private int Value(int lit)
    {
        sbyte v = _value[Literal.Var(lit)];
        if (v == 0) return 0;
        bool varTrue = v == 1;
        bool litTrue = Literal.IsPositive(lit) ? varTrue : !varTrue;
        return litTrue ? 1 : -1;
    }

    private int FirstUnassigned()
    {
        for (int v = 1; v <= _varCount; v++)
            if (_value[v] == 0) return v;
        return 0;
    }

    private bool[] BuildModel()
    {
        var model = new bool[_varCount + 1];
        for (int v = 1; v <= _varCount; v++)
            model[v] = _value[v] == 1;
        return model;
    }
}
