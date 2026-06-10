using SatSolver.Cnf;
using SatSolver.Encoding;
using SatSolver.Parsing;

namespace SatSolver.Cli;

// =====================================================================
//  Formula2CnfCommand - prikaz "formula2cnf" (ukol 1)
// =====================================================================
// Nacte formuli v NNF a vypise ji jako CNF v DIMACS pres Tseitinovo kodovani.
// Volani:  SatSolver formula2cnf [vstup [vystup]] [--equiv | --implication]
// Cte bud ze souboru nebo ze stdin, pise bud do souboru nebo na stdout.
public static class Formula2CnfCommand
{
    public static int Run(string[] args)
    {
        bool useEquivalences = true; // default jsou plne ekvivalence
        string? inputPath = null;
        string? outputPath = null;

        // projdu argumenty: dva flagy + max dve cesty (vstup, vystup)
        foreach (string arg in args)
        {
            switch (arg)
            {
                case "--equiv":
                    useEquivalences = true;
                    break;
                case "--implication":
                case "--impl":
                    useEquivalences = false;
                    break;
                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Neznamy parametr '{arg}'.");
                    if (inputPath == null)
                        inputPath = arg;
                    else if (outputPath == null)
                        outputPath = arg;
                    else
                        throw new ArgumentException("Moc argumentu (cekam max vstup a vystup).");
                    break;
            }
        }

        // nacteni vstupu (soubor nebo stdin)
        string inputText = inputPath != null
            ? File.ReadAllText(inputPath)
            : Console.In.ReadToEnd();

        // naparsovat NNF a prohnat Tseitinem do CNF
        Formula formula = SmtLibParser.Parse(inputText);
        CnfFormula cnf = new TseitinEncoder(useEquivalences).Encode(formula);

        // vypsat v DIMACS (soubor nebo stdout)
        string output = DimacsWriter.Write(cnf);
        if (outputPath != null)
            File.WriteAllText(outputPath, output);
        else
            Console.Out.Write(output);

        return 0;
    }
}
