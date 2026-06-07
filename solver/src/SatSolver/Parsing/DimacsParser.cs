using SatSolver.Cnf;

namespace SatSolver.Parsing;

// =====================================================================
//  DimacsParser - cteni DIMACS CNF souboru
// =====================================================================
// Format (viz zadani ukolu 1):
//   - radky komentaru zacinaji 'c'
//   - hlavicka "p cnf nbvar nbclauses"
//   - klauzule = posloupnost nenulovych cisel ukoncena nulou
//
// Schvalne to mam napsane dost benevolentne: klauzule muze byt rozsekana
// pres vic radku, vic klauzuli muze byt na jednom radku, na whitespace
// nezalezi. Cisla z hlavicky "p cnf" vlastne ani nepotrebuju - pocet
// promennych si dopocitam z literalu sam, takze me nerozhodi kdyz nekdo
// v souboru trochu lze (a SATLIB soubory obcas lzou).
public static class DimacsParser
{
    public static CnfFormula Parse(string text)
    {
        var formula = new CnfFormula();
        var currentClause = new List<int>(); // literaly klauzule kterou prave skladam

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();

            // prazdne radky a komentare proste preskocim
            if (trimmed.Length == 0 || trimmed[0] == 'c')
                continue;

            // hlavicku "p cnf ..." taky jen preskocim (cisla z ni nepouzivam)
            if (trimmed[0] == 'p')
                continue;

            // nektere SATLIB soubory davaji na konec dat '%', tak tam koncim
            if (trimmed[0] == '%')
                break;

            // jinak jsou na radku cisla oddelena mezerami, 0 ukoncuje klauzuli
            foreach (string token in trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(token, out int value))
                    throw new FormatException($"Nevalidni token '{token}' v DIMACS souboru.");

                if (value == 0)
                {
                    // konec klauzule -> ulozim a zacnu novou
                    formula.AddClause(new Clause(currentClause.ToArray()));
                    currentClause.Clear();
                }
                else
                {
                    currentClause.Add(value);
                }
            }
        }

        // kdyby posledni klauzule nahodou nebyla ukoncena nulou, tak ji taky pridam
        if (currentClause.Count > 0)
            formula.AddClause(new Clause(currentClause.ToArray()));

        return formula;
    }
}
