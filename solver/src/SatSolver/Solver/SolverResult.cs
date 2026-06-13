using SatSolver.Cnf;

namespace SatSolver.Solver;

// Jak dopadlo reseni.
public enum SatResult
{
    Satisfiable,
    Unsatisfiable,

    // Beh se prerusil (typicky timeout), nevime jestli SAT nebo UNSAT.
    Unknown
}

// =====================================================================
//  SolverResult - to co solver vrati ven
// =====================================================================
// Status + pripadny model + statistiky. Model je pole indexovane promennou
// (1..n): true = promenna je v modelu 1, false = 0. Index 0 se nepouziva
// (stejna konvence jako u literalu).
public sealed class SolverResult
{
    public SatResult Status { get; }
    public bool[]? Model { get; }
    public SolverStatistics Statistics { get; }

    public SolverResult(SatResult status, bool[]? model, SolverStatistics statistics)
    {
        Status = status;
        Model = model;
        Statistics = statistics;
    }

    // Vrati model jako literaly serazene podle indexu promenne (kladny = true, zaporny = false).
    // Tohle chce zadani ukolu 2 do vystupu pri DIMACS vstupu (literaly rostouci dle indexu).
    public IEnumerable<int> ModelLiterals()
    {
        if (Model == null)
            yield break;
        for (int v = 1; v < Model.Length; v++)
            yield return Model[v] ? v : -v;
    }

    // Sanity check: opravdu ten model splnuje vsechny klauzule? (radsi si to overim)
    public bool Verify(CnfFormula formula) => Model != null && formula.IsSatisfiedBy(Model);
}
