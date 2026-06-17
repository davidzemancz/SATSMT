using SatSolver.Cnf;

namespace SatSolver.Solver;

// Pomocny vypis pro ladeni (docasne). Az to bude slapat, smazat.
internal static class DebugDump
{
    // Vypise celou formuli (kolik promennych/klauzuli + klauzule po radcich).
    public static void Clauses(CnfFormula formula)
    {
        Console.Error.WriteLine($"-- formule: {formula.VariableCount} prom., {formula.ClauseCount} klauzuli --");
        foreach (Clause c in formula.Clauses)
            Console.Error.WriteLine("  " + c);
    }

    // Vypise model jako literaly (kladny = true, zaporny = false).
    public static void Model(bool[] model)
    {
        var sb = new System.Text.StringBuilder("model:");
        for (int v = 1; v < model.Length; v++)
            sb.Append(' ').Append(model[v] ? v : -v);
        Console.Error.WriteLine(sb.ToString());
    }

    // Vypis naucenych klauzuli i s jejich LBD (ladeni CDCL).
    public static void Learned(IEnumerable<Clause> learned)
    {
        foreach (Clause c in learned)
            Console.Error.WriteLine($"  learned lbd={c.Lbd}: {c}");
    }
}
