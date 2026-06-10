using SatSolver.Cnf;
using SatSolver.Parsing;

namespace SatSolver.Encoding;

// =====================================================================
//  TseitinEncoder - prevod NNF formule na CNF (Tseitinovo kodovani)
// =====================================================================
// Naivne by se dalo roznasobit AND/OR distributivnim zakonem, ale to muze
// exponencialne nabobtnat. Tseitin to obejde: vysledna CNF je jen
// "equisatisfiable" (ma stejnou splnitelnost, ne uplne stejne modely), zato
// je vetsi jen o konstantni faktor. Tohle bylo na prednasce (kap. 1).
//
// Postup:
//   1. kazdemu vstupnimu literalu odpovida primo jeho promenna
//   2. kazdemu hradlu (vnitrni uzel and/or) dam cerstvou pomocnou promennou g
//   3. pridam klauzule co popisuji chovani hradla (g <-> funkce vstupu)
//   4. promennou korene pridam jako unit klauzuli (formule musi platit)
//
// Flag useEquivalences (v konstruktoru):
//   true  - hradla definuju jako plnou ekvivalenci g <-> ...
//   false - jen implikace zleva doprava g -> ... (Plaisted-Greenbaum). Pro NNF
//           to staci na zachovani equisatisfiability a vyleze min klauzuli.
public sealed class TseitinEncoder
{
    // pouzit plne ekvivalence (true) nebo jen implikace zleva doprava (false)
    private readonly bool _useEquivalences;

    // mapping: jmeno vstupni promenne -> jeji index v CNF
    private readonly Dictionary<string, int> _varIndex = new();
    private CnfFormula _cnf = null!;
    private int _nextVar; // odsud nahoru prideluju pomocne (gate) promenne

    public TseitinEncoder(bool useEquivalences = true)
    {
        _useEquivalences = useEquivalences;
    }

    // Hlavni metoda: vezme strom a vyrobi z nej CNF.
    public CnfFormula Encode(Formula root)
    {
        _cnf = new CnfFormula();
        _varIndex.Clear();

        // 1. faze: vstupnim promennym dam nizke indexy 1..k v poradi jak se poprve
        //    objevi. Diky tomu jsou v DIMACS vystupu hezky oddelene od pomocnych
        //    gate promennych (a v komentarich se to pak lip cte).
        AssignInputVariables(root);
        _nextVar = _varIndex.Count; // gate promenne zacnou az od _nextVar + 1

        // 2. faze: rekurzivne zakoduju strom. Vrati literal reprezentujici koren.
        int rootLiteral = EncodeNode(root);

        // 3. koren musi byt true -> unit klauzule
        _cnf.AddClause(new Clause(new[] { rootLiteral }));
        _cnf.RootVariable = Literal.Var(rootLiteral);

        // jeste si zapamatuju jmena vstupnich promennych do metadat (pro komentare v DIMACS)
        foreach (var (name, idx) in _varIndex)
            _cnf.VariableNames[idx] = name;

        return _cnf;
    }

    // Projde strom a vstupnim promennym priradi indexy 1..k.
    private void AssignInputVariables(Formula f)
    {
        switch (f)
        {
            case VarFormula v:
                if (!_varIndex.ContainsKey(v.Name))
                    _varIndex[v.Name] = _varIndex.Count + 1;
                break;
            case AndFormula a:
                AssignInputVariables(a.Left);
                AssignInputVariables(a.Right);
                break;
            case OrFormula o:
                AssignInputVariables(o.Left);
                AssignInputVariables(o.Right);
                break;
        }
    }

    // Vyrobi novou pomocnou (gate) promennou a zapise si ji do metadat.
    private int NewGateVariable()
    {
        _nextVar++;
        _cnf.AuxiliaryVariables.Add(_nextVar);
        return _nextVar;
    }

    // Rekurzivne zakoduje uzel a vrati literal, jehoz pravdivost = pravdivost uzlu.
    private int EncodeNode(Formula f)
    {
        switch (f)
        {
            case VarFormula v:
            {
                // list: literal je rovnou promenna (pripadne jeji negace)
                int var = _varIndex[v.Name];
                return v.Negated ? Literal.Negate(var) : var;
            }
            case AndFormula a:
            {
                int left = EncodeNode(a.Left);
                int right = EncodeNode(a.Right);
                int g = NewGateVariable();
                EncodeAndGate(g, left, right);
                return g;
            }
            case OrFormula o:
            {
                int left = EncodeNode(o.Left);
                int right = EncodeNode(o.Right);
                int g = NewGateVariable();
                EncodeOrGate(g, left, right);
                return g;
            }
            default:
                throw new InvalidOperationException("Neznamy typ uzlu formule.");
        }
    }

    // Klauzule pro hradlo g <-> (a AND b).
    private void EncodeAndGate(int g, int a, int b)
    {
        // smer g -> a AND b, tzn. (-g | a) a (-g | b). Tohle staci pro equisat v NNF.
        _cnf.AddClause(new Clause(new[] { Literal.Negate(g), a }));
        _cnf.AddClause(new Clause(new[] { Literal.Negate(g), b }));

        if (_useEquivalences)
        {
            // opacny smer (a AND b) -> g, tzn. (g | -a | -b)
            _cnf.AddClause(new Clause(new[] { g, Literal.Negate(a), Literal.Negate(b) }));
        }
    }

    // Klauzule pro hradlo g <-> (a OR b).
    private void EncodeOrGate(int g, int a, int b)
    {
        // smer g -> a OR b, tzn. (-g | a | b)
        _cnf.AddClause(new Clause(new[] { Literal.Negate(g), a, b }));

        if (_useEquivalences)
        {
            // opacny smer (a OR b) -> g, tzn. (g | -a) a (g | -b)
            _cnf.AddClause(new Clause(new[] { g, Literal.Negate(a) }));
            _cnf.AddClause(new Clause(new[] { g, Literal.Negate(b) }));
        }
    }
}
