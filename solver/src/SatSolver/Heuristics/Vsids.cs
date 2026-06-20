using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  VSIDS - Variable State Independent Decaying Sum (Chaff, 2001)
// =====================================================================
// Tahle heuristika je dynamicka a je to ten hlavni duvod proc je CDCL tak
// rychly. Kazda promenna ma "aktivitu". Kdyz se promenna objevi v analyze
// konfliktu, prihodim ji k aktivite _increment. Po kazdem konfliktu pak
// _increment vynasobim 1/decay - cimz efektivne zlevnim vsechny stare aktivity
// (jako kdyby "starly"). Vybiram neprirazenou promennou s nejvyssi aktivitou.
//
// Vysledek: solver se sam od sebe soustredi na promenne co byly nedavno v
// konfliktech. Aby to bylo rychle, drzim promenne v binarni halde (VariableHeap),
// jinak by hledani maxima bylo O(n) pokazde.
public sealed class Vsids : IDecisionHeuristic
{
    private const double Decay = 0.95;
    private const double RescaleLimit = 1e100; // kdyz aktivita prerostle tohle, vsechno preskaluju (overflow)

    private SearchEngine _engine = null!;
    private double[] _activity = null!;
    private VariableHeap _heap = null!;
    private double _increment = 1.0;

    public void Initialize(SearchEngine engine, CnfFormula formula)
    {
        _engine = engine;
        _activity = new double[formula.VariableCount + 1];
        _heap = new VariableHeap(formula.VariableCount, _activity);
        for (int v = 1; v <= formula.VariableCount; v++)
            _heap.Insert(v);
    }

    public int PickBranchLiteral()
    {
        // taham z haldy dokud nenarazim na neprirazenou promennou
        // (prirazene tam muzou jeste zbyt, protoze odpriraz se resi az lazy pres Insert)
        while (!_heap.IsEmpty)
        {
            int v = _heap.RemoveMax();
            if (!_engine.IsAssigned(v))
                return -v; // default faze "false" jako v MiniSatu; phase saving ji pripadne prebije
        }
        return 0;
    }

    public void OnVariableBump(int var)
    {
        _activity[var] += _increment;
        if (_activity[var] > RescaleLimit)
            Rescale();
        _heap.Increase(var); // aktivita stoupla -> oprav pozici v halde
    }

    public void OnConflict()
    {
        // tim ze zvysim increment vlastne "zlevnim" vsechny minule aktivity = decay
        _increment /= Decay;
    }

    public void OnUnassign(int var)
    {
        // odprirazena promenna se vraci do haldy, at ji muzu zase vybrat
        _heap.Insert(var);
    }

    // Preskalovani at nepretece double - poradi promennych se nezmeni (delim vsechno stejne).
    private void Rescale()
    {
        for (int v = 1; v < _activity.Length; v++)
            _activity[v] /= RescaleLimit;
        _increment /= RescaleLimit;
    }
}
