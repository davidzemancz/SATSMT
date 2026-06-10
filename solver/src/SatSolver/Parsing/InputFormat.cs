using SatSolver.Cnf;
using SatSolver.Encoding;

namespace SatSolver.Parsing;

// Vstupni formaty co umime nacist.
public enum InputFormat
{
    // DIMACS CNF (pripona .cnf).
    Dimacs,

    // Zjednoduseny SMT-LIB / NNF format (pripona .sat).
    SmtLib
}

// =====================================================================
//  InputLoader - detekce formatu + nacteni vstupu rovnou jako CNF
// =====================================================================
// Maly helper, at to nemusim resit v kazdem commandu zvlast. SMT-LIB vstup
// se pri nacteni rovnou prozene Tseitinem, takze ven leze vzdycky CNF a
// zbytek solveru uz o ".sat" formatu vubec nemusi vedet.
public static class InputLoader
{
    // Hadame format podle pripony (.cnf = DIMACS, .sat = SMT-LIB).
    public static InputFormat DetectFromPath(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cnf" => InputFormat.Dimacs,
            ".sat" => InputFormat.SmtLib,
            _ => throw new FormatException(
                $"Nevim co je za format podle pripony '{ext}'. Pouzij .cnf nebo .sat, nebo zadej format rucne.")
        };
    }

    // Nacte text v danem formatu a vrati CNF (SMT-LIB cestou pres Tseitina).
    public static CnfFormula LoadAsCnf(string text, InputFormat format, bool useEquivalences = true)
    {
        return format switch
        {
            InputFormat.Dimacs => DimacsParser.Parse(text),
            InputFormat.SmtLib => new TseitinEncoder(useEquivalences).Encode(SmtLibParser.Parse(text)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}
