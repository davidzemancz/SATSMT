using System.Text;
using SatSolver.Cnf;

namespace SatSolver.Encoding;

// =====================================================================
//  DimacsWriter - zapis CNF formule do textoveho DIMACS formatu
// =====================================================================
// Vystup vypada takhle:
//   - komentare zacinaji 'c' (sem si pisu mapping promennych)
//   - hlavicka "p cnf nbvar nbclauses"
//   - klauzule: literaly oddelene mezerou, na konci 0
//
// Do komentaru podle zadani ukolu 1 musim vypsat ktera CNF promenna odpovida
// ktere vstupni, ktere jsou pomocne (gate) a ktera je koren. Cestina v
// komentarich je zamerne bez diakritiky, at to nedela problem v ASCII vystupu.
public static class DimacsWriter
{
    public static string Write(CnfFormula formula)
    {
        var sb = new StringBuilder();

        // popis pojmenovanych (vstupnich) promennych
        if (formula.VariableNames.Count > 0)
        {
            sb.AppendLine("c Vstupni promenne (CNF index -> nazev):");
            foreach (var (idx, name) in formula.VariableNames.OrderBy(p => p.Key))
                sb.AppendLine($"c   {idx} -> {name}");
        }

        // popis pomocnych gate promennych
        if (formula.AuxiliaryVariables.Count > 0)
        {
            sb.AppendLine("c Pomocne promenne:");
            sb.AppendLine("c   " + string.Join(", ", formula.AuxiliaryVariables.OrderBy(x => x)));
        }

        // promenna co odpovida korenu formule
        if (formula.RootVariable != 0)
            sb.AppendLine($"c Koren: {formula.RootVariable}");

        sb.AppendLine("c");

        // hlavicka
        sb.AppendLine($"p cnf {formula.VariableCount} {formula.ClauseCount}");

        // klauzule - kazdou na svuj radek, na konci nula
        foreach (Clause clause in formula.Clauses)
        {
            foreach (int lit in clause.Literals)
            {
                sb.Append(lit);
                sb.Append(' ');
            }
            sb.AppendLine("0");
        }

        return sb.ToString();
    }
}
