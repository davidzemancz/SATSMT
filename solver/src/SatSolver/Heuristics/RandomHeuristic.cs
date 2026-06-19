using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  RandomHeuristic - nahodny vyber promenne i faze
// =====================================================================
// Slouzi jako "baseline" do ukolu 4 - kdyz nejaka chytra heuristika neporazi
// ani nahodu, tak je k nicemu. Seed beru z options at jdou mereni zopakovat.
public sealed class RandomHeuristic : IDecisionHeuristic
{
    private readonly Random _rng;
    private SearchEngine _engine = null!;
    private readonly List<int> _free = new(); // neprirazene promenne, sbiram je sem znova pri kazdem vyberu

    public RandomHeuristic(int seed) => _rng = new Random(seed);

    public void Initialize(SearchEngine engine, CnfFormula formula) => _engine = engine;

    public int PickBranchLiteral()
    {
        _free.Clear();
        for (int v = 1; v <= _engine.VarCount; v++)
            if (!_engine.IsAssigned(v))
                _free.Add(v);

        if (_free.Count == 0)
            return 0;

        int var = _free[_rng.Next(_free.Count)];
        bool positive = _rng.Next(2) == 0; // hod minci na fazi
        return positive ? var : -var;
    }

    public void OnVariableBump(int var) { }
    public void OnConflict() { }
    public void OnUnassign(int var) { }
}
