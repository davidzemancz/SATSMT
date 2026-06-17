namespace SatSolver.Solver;

// Rezim hledani: bud cista DPLL, nebo CDCL s ucenim klauzuli.
public enum SearchMode
{
    // DPLL: branch & bound s chronologickym backtrackingem, bez uceni.
    Dpll,

    // CDCL: uceni klauzuli, backjumping. Ten "ostry" rezim.
    Cdcl
}

// Jakou datovou strukturu pouzit na unit propagaci.
public enum PropagationMode
{
    // Adjacency lists (occurrence listy) - jednodussi, ale dela vic prace.
    AdjacencyList,

    // Watched literals - lina struktura, dneska standard.
    WatchedLiterals
}

// =====================================================================
//  SolverOptions - vsechno co se da na solveru nastavit
// =====================================================================
// Cely smysl je ze mam JEDEN engine ktery se da nastavit bud jako zakladni
// DPLL nebo jako CDCL (a vsechno mezi tim). Defaulty miri na ten lepsi
// rezim (CDCL + watched).
public sealed class SolverOptions
{
    public SearchMode SearchMode { get; set; } = SearchMode.Cdcl;
    public PropagationMode Propagation { get; set; } = PropagationMode.WatchedLiterals;

    // Uceni klauzuli (ma smysl jen v CDCL).
    public bool EnableClauseLearning { get; set; } = true;

    // Obcasne mazani naucenych klauzuli, at databaze neroste donekonecna.
    public bool EnableClauseDeletion { get; set; } = true;

    // Minimalizace naucene klauzule (self-subsuming resolution).
    public bool EnableClauseMinimization { get; set; } = true;

    // Phase saving - pri dalsim rozhodovani o promenne pouzij naposled prirazenou fazi.
    public bool EnablePhaseSaving { get; set; } = true;

    // Tvrdy casovy limit v sekundach (0 = bez limitu). Hodi se na bench at to nezamrzne.
    public double TimeLimitSeconds { get; set; } = 0;
}
