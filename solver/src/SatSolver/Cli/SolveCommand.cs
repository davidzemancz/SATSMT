using SatSolver.Cnf;
using SatSolver.Parsing;
using SatSolver.Solver;

namespace SatSolver.Cli;

// =====================================================================
//  SolveCommand - prikaz "solve" (ukoly 2-5)
// =====================================================================
// Nacte CNF (DIMACS) nebo NNF (SMT-LIB, ten se zakoduje Tseitinem), pusti
// nakonfigurovany solver a vypise vysledek (SAT/UNSAT), pripadne model a
// statistiky.
public static class SolveCommand
{
    public static int Run(string[] args)
    {
        SolverOptions options = CommandLineOptions.Parse(args, out List<string> positionals,
            out InputFormat? forcedFormat, out bool printModel);

        if (positionals.Count > 1)
            throw new ArgumentException("Cekam max jeden vstupni soubor.");
        string? inputPath = positionals.Count == 1 ? positionals[0] : null;

        // --- nacteni vstupu (soubor nebo stdin) ---
        string inputText = inputPath != null ? File.ReadAllText(inputPath) : Console.In.ReadToEnd();
        InputFormat format = forcedFormat
            ?? (inputPath != null ? InputLoader.DetectFromPath(inputPath) : InputFormat.Dimacs);
        CnfFormula cnf = InputLoader.LoadAsCnf(inputText, format);

        // --- vlastni reseni ---
        var engine = new SearchEngine(cnf, options);
        SolverResult result = engine.Solve();

        // --- vypis ---
        PrintResult(result, printModel);
        return result.Status switch
        {
            // konvence ze SAT competition: 10 = SAT, 20 = UNSAT
            SatResult.Satisfiable => 10,
            SatResult.Unsatisfiable => 20,
            _ => 0
        };
    }

    private static void PrintResult(SolverResult result, bool printModel)
    {
        switch (result.Status)
        {
            case SatResult.Satisfiable:
                Console.WriteLine("SAT");
                if (printModel)
                {
                    // u DIMACS chce zadani literaly rostouci podle indexu promenne
                    string model = string.Join(" ", result.ModelLiterals());
                    Console.WriteLine("v " + model + " 0");
                }
                break;
            case SatResult.Unsatisfiable:
                Console.WriteLine("UNSAT");
                break;
            default:
                Console.WriteLine("UNKNOWN");
                break;
        }

        // statistiky davam na stderr, at nezaspini strojove citelny vystup na stdout
        Console.Error.WriteLine("c statistiky:");
        Console.Error.WriteLine(result.Statistics.ToString());
    }
}
