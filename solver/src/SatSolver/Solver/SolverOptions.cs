namespace SatSolver.Solver;

// Jakou datovou strukturu pouzit na unit propagaci.
public enum PropagationMode
{
    // Adjacency lists (occurrence listy) - jednodussi, ale dela vic prace.
    AdjacencyList
}

// =====================================================================
//  SolverOptions - co se da na solveru nastavit
// =====================================================================
// Zatim toho moc neni (zakladni DPLL z ukolu 2) - jen volba propagacni
// struktury a casovy limit. Jak budou pribyvat dalsi ukoly, pribudou
// sem i dalsi prepinace.
public sealed class SolverOptions
{
    public PropagationMode Propagation { get; set; } = PropagationMode.AdjacencyList;

    // Tvrdy casovy limit v sekundach (0 = bez limitu). Hodi se na bench at to nezamrzne.
    public double TimeLimitSeconds { get; set; } = 0;
}
