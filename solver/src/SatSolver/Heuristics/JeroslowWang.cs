using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  JeroslowWang - staticka heuristika (1990)
// =====================================================================
// Napad: preferuj literaly co se casto objevuji v KRATKYCH klauzulich (ty
// jsou totiz "skoro splnene", takze se vyplati je resit drive). Skore literalu
// l je:
//     J(l) = suma pres klauzule C kde l je v C z  2^(-|C|)
// Spocitam to jednou na zacatku (proto "staticka") a pak uz jen vybiram
// neprirazenou promennou s nejvyssim skore nejake sve polarity.
public sealed class JeroslowWang : IDecisionHeuristic
{
    private SearchEngine _engine = null!;
    private double[] _score = null!; // indexovano literalem (Literal.ToIndex)

    public void Initialize(SearchEngine engine, CnfFormula formula)
    {
        _engine = engine;
        _score = new double[Literal.IndexArraySize(formula.VariableCount)];
        foreach (Clause c in formula.Clauses)
        {
            double weight = Math.Pow(2.0, -c.Size); // kratsi klauzule = vetsi vaha
            foreach (int lit in c.Literals)
                _score[Literal.ToIndex(lit)] += weight;
        }
    }

    public int PickBranchLiteral()
    {
        double best = -1.0;
        int bestLit = 0;
        for (int v = 1; v <= _engine.VarCount; v++)
        {
            if (_engine.IsAssigned(v))
                continue;

            double posScore = _score[Literal.ToIndex(v)];
            double negScore = _score[Literal.ToIndex(-v)];
            // pro tuhle promennou si vyberu lepsi polaritu
            double s = Math.Max(posScore, negScore);
            if (s > best)
            {
                best = s;
                bestLit = posScore >= negScore ? v : -v;
            }
        }
        return bestLit; // 0 kdyz uz je vsechno prirazene
    }

    public void OnVariableBump(int var) { }
    public void OnConflict() { }
    public void OnUnassign(int var) { }
}
