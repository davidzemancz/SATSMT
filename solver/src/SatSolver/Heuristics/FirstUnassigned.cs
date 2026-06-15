using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  FirstUnassigned - nejjednodussi mozna heuristika
// =====================================================================
// Vezme prvni neprirazenou promennou podle indexu a zkusi ji nejdriv dat na
// true. Nic chytreho, ale presne tohle chtelo zadani ukolu 2 jako default pro
// zakladni DPLL. Dobre se s tim porovnavaji ty chytrejsi heuristiky (kdyz uz i
// tahle blbost projde, tak je solver aspon korektni :)).
public sealed class FirstUnassigned : IDecisionHeuristic
{
    private SearchEngine _engine = null!;

    public void Initialize(SearchEngine engine, CnfFormula formula) => _engine = engine;

    public int PickBranchLiteral()
    {
        for (int v = 1; v <= _engine.VarCount; v++)
            if (!_engine.IsAssigned(v))
                return v; // kladny literal -> zkusim nejdriv true
        return 0; // nic neprirazeneho uz neni
    }

    // staticka heuristika -> hooky neresim
    public void OnVariableBump(int var) { }
    public void OnConflict() { }
    public void OnUnassign(int var) { }
}
