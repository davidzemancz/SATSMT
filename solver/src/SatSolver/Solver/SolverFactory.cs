using SatSolver.Heuristics;
using SatSolver.Restarts;

namespace SatSolver.Solver;

// =====================================================================
//  SolverFactory - poskladani vymenitelnych komponent solveru
// =====================================================================
// Misto abych v SearchEngine delal hromadu if/switch a tahal tam vsechny
// konkretni tridy, dam to sem. Engine pak zna jen rozhrani (IPropagator,
// IDecisionHeuristic, ...) a vubec netusi jestli jede zrovna VSIDS nebo random.
// Klasicky factory pattern.
internal static class SolverFactory
{
    public static IPropagator CreatePropagator(SolverOptions options) => options.Propagation switch
    {
        PropagationMode.AdjacencyList => new AdjacencyListPropagator(),
        PropagationMode.WatchedLiterals => new WatchedLiteralsPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };

    public static IDecisionHeuristic CreateHeuristic(SolverOptions options) => options.Heuristic switch
    {
        HeuristicKind.First => new FirstUnassigned(),
        HeuristicKind.Random => new RandomHeuristic(options.RandomSeed),
        HeuristicKind.JeroslowWang => new JeroslowWang(),
        HeuristicKind.Vsids => new Vsids(),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };

    // Restarty davaji smysl jen v CDCL; "zadne" vrati null a engine pak vi ze nerestartuje.
    public static IRestartStrategy? CreateRestart(SolverOptions options) => options.Restart switch
    {
        RestartKind.None => null,
        RestartKind.Geometric => new GeometricRestart(options.RestartBase, options.GeometricFactor),
        RestartKind.Luby => new LubyRestart(options.RestartBase),
        _ => throw new ArgumentOutOfRangeException(nameof(options))
    };
}
