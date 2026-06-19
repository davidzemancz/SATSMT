using System.Globalization;
using SatSolver.Parsing;
using SatSolver.Solver;

namespace SatSolver.Cli;

// =====================================================================
//  CommandLineOptions - parsovani prepinacu solveru
// =====================================================================
// Sdileny kus kodu pro "solve" i "bench", at se konfigurace zadava vsude
// stejne. Klasicky pruchod argumenty + velky switch. Neni to zadny poradny
// argparse, ale pro tenhle projekt to bohate staci.
public static class CommandLineOptions
{
    // Naparsuje prepinace do SolverOptions. Pozicni argumenty (co nezacinaji '-')
    // vrati v positionals, vynuceny format ve format a "nevypisovat model" v printModel.
    public static SolverOptions Parse(
        IReadOnlyList<string> args,
        out List<string> positionals,
        out InputFormat? format,
        out bool printModel)
    {
        var options = new SolverOptions();
        positionals = new List<string>();
        format = null;
        printModel = true;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--dpll":
                    // DPLL rezim = vypnout vsechny CDCL vychytavky
                    options.SearchMode = SearchMode.Dpll;
                    options.EnableClauseLearning = false;
                    options.Restart = RestartKind.None;
                    options.EnableClauseDeletion = false;
                    options.EnablePhaseSaving = false;
                    break;
                case "--cdcl":
                    options.SearchMode = SearchMode.Cdcl;
                    options.EnableClauseLearning = true;
                    break;
                case "--prop":
                    options.Propagation = Value(args, ref i) switch
                    {
                        "adj" or "adjacency" => PropagationMode.AdjacencyList,
                        "watched" or "watch" => PropagationMode.WatchedLiterals,
                        var v => throw new ArgumentException($"Neznama hodnota --prop '{v}'.")
                    };
                    break;
                case "--heuristic":
                    options.Heuristic = Value(args, ref i) switch
                    {
                        "first" => HeuristicKind.First,
                        "random" => HeuristicKind.Random,
                        "jw" or "jeroslow" => HeuristicKind.JeroslowWang,
                        "vsids" => HeuristicKind.Vsids,
                        var v => throw new ArgumentException($"Neznama heuristika '{v}'.")
                    };
                    break;
                case "--restart":
                    options.Restart = Value(args, ref i) switch
                    {
                        "none" => RestartKind.None,
                        "geom" or "geometric" => RestartKind.Geometric,
                        "luby" => RestartKind.Luby,
                        var v => throw new ArgumentException($"Neznama strategie restartu '{v}'.")
                    };
                    break;
                case "--no-learn": options.EnableClauseLearning = false; break;
                case "--no-delete": options.EnableClauseDeletion = false; break;
                case "--no-minimize": options.EnableClauseMinimization = false; break;
                case "--phase-saving": options.EnablePhaseSaving = true; break;
                case "--no-phase-saving": options.EnablePhaseSaving = false; break;
                case "--seed": options.RandomSeed = int.Parse(Value(args, ref i)); break;
                case "--restart-base": options.RestartBase = int.Parse(Value(args, ref i)); break;
                case "--time-limit":
                    // pozor: parsuju s invariant culture, jinak by to na cz locale chtelo carku misto tecky
                    options.TimeLimitSeconds = double.Parse(Value(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--format":
                    format = Value(args, ref i) switch
                    {
                        "cnf" or "dimacs" => InputFormat.Dimacs,
                        "sat" or "smtlib" => InputFormat.SmtLib,
                        var v => throw new ArgumentException($"Neznamy format '{v}'.")
                    };
                    break;
                case "--no-model": printModel = false; break;
                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Neznamy prepinac '{arg}'.");
                    positionals.Add(arg); // neni to flag -> bude to vstupni soubor
                    break;
            }
        }
        return options;
    }

    // Naparsuje prepinace rovnou z jednoho retezce (napr. "--dpll --prop adj"). Pouziva bench.
    public static SolverOptions ParseFlags(string flags)
    {
        string[] tokens = flags.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return Parse(tokens, out _, out _, out _);
    }

    // Precte hodnotu co stoji za prepinacem (a posune i). Sdili i BenchCommand.
    internal static string Value(IReadOnlyList<string> args, ref int i)
    {
        if (i + 1 >= args.Count)
            throw new ArgumentException($"Prepinac '{args[i]}' chce jeste hodnotu za sebou.");
        return args[++i];
    }
}
