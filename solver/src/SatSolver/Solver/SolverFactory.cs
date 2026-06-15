using SatSolver.Heuristics;

namespace SatSolver.Solver;

// =====================================================================
//  SolverFactory - poskladani vymenitelnych komponent solveru
// =====================================================================
// Misto if/switch primo v SearchEngine si konkretni tridy poskladam tady.
// Engine pak zna jen rozhrani (IPropagator, IDecisionHeuristic) a netusi
// jestli jede adjacency nebo watched. Klasicky factory pattern.
internal static class SolverFactory
{
    public static IPropagator CreatePropagator(SolverOptions options) => options.Propagation switch
    {
        PropagationMode.AdjacencyList => new AdjacencyListPropagator(),
        PropagationMode.WatchedLiterals => new WatchedLiteralsPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };

    public static IDecisionHeuristic CreateHeuristic(SolverOptions options) => new FirstUnassigned();
}
