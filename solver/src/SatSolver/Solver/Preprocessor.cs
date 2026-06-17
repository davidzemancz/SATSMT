using SatSolver.Cnf;

namespace SatSolver.Solver;

// Experiment: predzpracovani formule pred resenim (propagace unit klauzuli na
// nejvyssi urovni, pripadne odstraneni cistych literalu). Nakonec to stejne resi
// rovnou SearchEngine pri nacitani klauzuli, takze je to asi k nicemu - necham
// to tu zatim, kdyztak smazu pred odevzdanim.
internal static class Preprocessor
{
    // Vrati zjednodusenou formuli. Zatim jen kostra (vrati kopii beze zmeny).
    public static CnfFormula Simplify(CnfFormula formula)
    {
        // TODO: opakovana unit propagace na levelu 0, dokud se neco meni
        return formula.Clone();
    }
}
