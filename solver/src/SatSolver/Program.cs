// SAT solver - ukol 1: prevod formule v NNF (SMT-LIB) na CNF (DIMACS) Tseitinem.
// Zatim to delam natvrdo primo tady v Program.cs, protoze je jen jeden "prikaz".
// Az pribudou dalsi (solve, bench), rozsekam to na poradne commandy.
using SatSolver.Cnf;
using SatSolver.Encoding;
using SatSolver.Parsing;

bool useEquivalences = true; // default plne ekvivalence
string? inputPath = null;
string? outputPath = null;

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
            {
                Console.Error.WriteLine($"Neznamy parametr '{arg}'.");
                return 1;
            }
            if (inputPath == null) inputPath = arg;
            else if (outputPath == null) outputPath = arg;
            else { Console.Error.WriteLine("Moc argumentu (cekam max vstup a vystup)."); return 1; }
            break;
    }
}

// nacteni vstupu (soubor nebo stdin)
string inputText = inputPath != null ? File.ReadAllText(inputPath) : Console.In.ReadToEnd();

// naparsovat NNF a prohnat Tseitinem do CNF, vypsat v DIMACS
Formula formula = SmtLibParser.Parse(inputText);
CnfFormula cnf = new TseitinEncoder(useEquivalences).Encode(formula);
string output = DimacsWriter.Write(cnf);

if (outputPath != null) File.WriteAllText(outputPath, output);
else Console.Out.Write(output);
return 0;
