using SatSolver.Heuristics;
using SatSolver.Restarts;

namespace SatSolver.Solver;

// =====================================================================
//  SolverFactory - poskladani vymenitelnych komponent solveru
// =====================================================================
// Misto if/switch primo v SearchEngine si konkretni tridy poskladam tady.
// Engine pak zna jen rozhrani (IPropagator, IDecisionHeuristic,
// IRestartStrategy) a netusi co zrovna jede. Klasicky factory pattern.
internal static class SolverFactory
{
    public static IPropagator CreatePropagator(SolverOptions options) => options.Propagation switch
    {
        PropagationMode.AdjacencyList => new AdjacencyListPropagator(),
        PropagationMode.WatchedLiterals => new WatchedLiteralsPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };

    public static IDecisionHeuristic CreateHeuristic(SolverOptions options) => new FirstUnassigned();

    // Restarty davaji smysl jen v CDCL; "zadne" vrati null a engine pak vi ze nerestartuje.
    public static IRestartStrategy? CreateRestart(SolverOptions options) => options.Restart switch
    {
        RestartKind.None => null,
        RestartKind.Geometric => new GeometricRestart(options.RestartBase, options.GeometricFactor),
        RestartKind.Luby => new LubyRestart(options.RestartBase),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };
}
