using SatSolver.Heuristics;

namespace SatSolver.Solver;

// =====================================================================
//  SolverFactory - poskladani vymenitelnych komponent solveru
// =====================================================================
// Misto abych v SearchEngine delal hromadu if/switch a tahal tam konkretni
// tridy, dam to sem. Engine pak zna jen rozhrani (IPropagator,
// IDecisionHeuristic). Klasicky factory pattern. Zatim je tu jen jedna
// propagace a jedna heuristika, ale prave proto to ma smysl - dalsi ukoly
// sem uz jen pribudou.
internal static class SolverFactory
{
    public static IPropagator CreatePropagator(SolverOptions options) => options.Propagation switch
    {
        PropagationMode.AdjacencyList => new AdjacencyListPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };

    public static IDecisionHeuristic CreateHeuristic(SolverOptions options) => new FirstUnassigned();
}
