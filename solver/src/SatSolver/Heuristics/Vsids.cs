using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  VSIDS - PRVNI POKUS (linearni sken)
// =====================================================================
// Variable State Independent Decaying Sum: kazda promenna ma aktivitu, ktera
// se zvedne kdyz se promenna objevi v analyze konfliktu, a postupne "vyhasina".
// Vybiram promennou s nejvyssi aktivitou.
//
// Tahle verze proste projde vsechny neprirazene promenne a vezme max - tedy
// O(n) na kazde rozhodnuti. Funguje, ale na vetsich instancich to brzdi.
// Pozdeji prepsano na binarni haldu (VariableHeap), at je vyber log(n).
public sealed class Vsids : IDecisionHeuristic
{
    private SearchEngine _engine = null!;
    private double[] _activity = null!;
    private double _inc = 1.0;
    private const double DecayFactor = 0.95;

    public void Initialize(SearchEngine engine, CnfFormula formula)
    {
        _engine = engine;
        _activity = new double[engine.VarCount + 1];
    }

    public int PickBranchLiteral()
    {
        int best = 0;
        double bestActivity = -1.0;
        for (int v = 1; v <= _engine.VarCount; v++)
        {
            if (_engine.IsAssigned(v))
                continue;
            if (_activity[v] > bestActivity)
            {
                bestActivity = _activity[v];
                best = v;
            }
        }
        return best; // kladny literal, fazi resi phase saving
    }

    public void OnVariableBump(int var)
    {
        _activity[var] += _inc;
    }

    public void OnConflict()
    {
        // "decay": zvysim inkrement, cimz relativne zlevni stare aktivity
        _inc /= DecayFactor;
    }

    // pri linearnim skenu se o odprirazene promenne nemusim starat
    public void OnUnassign(int var) { }
}
