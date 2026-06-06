namespace SatSolver.Cnf;

// =====================================================================
//  CnfFormula - cela CNF formule = konjunkce klauzuli
// =====================================================================
// Promenne jsou cislovane 1..VariableCount. Krome klauzuli si formule drzi
// jeste par nepovinnych metadat z Tseitina (VariableNames, AuxiliaryVariables,
// RootVariable), aby je pak DimacsWriter mohl vypsat do komentaru - zadani
// ukolu 1 to chce (musi byt videt ktera CNF promenna je ktera vstupni, ktere
// jsou pomocne pro hradla a ktera odpovida korenu formule).
public sealed class CnfFormula
{
    // Nejvyssi index promenne pouzite ve formuli (v DIMACS hlavicce "nbvar").
    public int VariableCount { get; set; }

    // Vsechny klauzule formule.
    public List<Clause> Clauses { get; } = new();

    // Mapping: index CNF promenne -> jeji puvodni jmeno ze vstupni formule.
    // (Jen pro promenne co maji jmeno, pomocne gate promenne typicky zadne nemaji.)
    public Dictionary<int, string> VariableNames { get; } = new();

    // Indexy pomocnych (gate) promennych ktere zavedl Tseitin.
    public HashSet<int> AuxiliaryVariables { get; } = new();

    // Index promenne odpovidajici korenu vstupni formule (0 = neuvedeno).
    public int RootVariable { get; set; }

    // Prida klauzuli a kdyz je treba zvedne VariableCount.
    public void AddClause(Clause clause)
    {
        foreach (int lit in clause.Literals)
        {
            int v = Literal.Var(lit);
            if (v > VariableCount)
                VariableCount = v;
        }
        Clauses.Add(clause);
    }

    // Pohodlnejsi pridani klauzule rovnou z literalu, at nemusim porad psat new Clause(...).
    public void AddClause(params int[] literals) => AddClause(new Clause(literals));

    // Pocet klauzuli (v DIMACS hlavicce "nbclauses").
    public int ClauseCount => Clauses.Count;

    // Hluboka kopie formule (kazda klauzule dostane vlastni pole literalu).
    // Proc to vubec resim: solver pri propagaci prehazuje literaly uvnitr klauzuli
    // (watched literals), takze kdyz chci tu samou formuli pustit vickrat (testy,
    // bench), musim mit pokazde cerstvou kopii. Jednou jsem na to zapomnel a
    // druhy beh davical nesmysly :D
    public CnfFormula Clone()
    {
        var copy = new CnfFormula { VariableCount = VariableCount, RootVariable = RootVariable };
        foreach (Clause c in Clauses)
            copy.Clauses.Add(new Clause((int[])c.Literals.Clone()));
        foreach (var (idx, name) in VariableNames)
            copy.VariableNames[idx] = name;
        foreach (int aux in AuxiliaryVariables)
            copy.AuxiliaryVariables.Add(aux);
        return copy;
    }

    // Splnuje dane (uplne) ohodnoceni vsechny klauzule? Pole je indexovane
    // promennou (1..n): true = promenna je 1, false = 0. Pouzivam na finalni
    // kontrolu modelu, at mam jistotu ze solver nelze.
    public bool IsSatisfiedBy(bool[] value)
    {
        foreach (Clause c in Clauses)
        {
            bool ok = false;
            foreach (int lit in c.Literals)
            {
                bool litValue = Literal.IsPositive(lit) ? value[Literal.Var(lit)] : !value[Literal.Var(lit)];
                if (litValue) { ok = true; break; } // klauzule splnena, dalsi literaly uz neresim
            }
            if (!ok)
                return false; // jedna nesplnena klauzule a je po vsem
        }
        return true;
    }
}
