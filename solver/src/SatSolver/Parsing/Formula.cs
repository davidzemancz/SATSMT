namespace SatSolver.Parsing;

// =====================================================================
//  Formula - AST (strom) vstupni formule v NNF
// =====================================================================
// Gramatika ze zadani ukolu 1:
//   formula ::= '(' 'and' formula formula ')'
//             | '(' 'or'  formula formula ')'
//             | '(' 'not' variable ')'
//             | variable
//
// Protoze je vstup v NNF (negace jen u promennych), nemam zadny extra uzel
// "not" nad podstromem - negaci si pamatuju primo v listu (VarFormula) jako
// flag Negated. Mensi strom, min prace.
//
// Pozn.: udelal jsem abstraktni Formula a tri potomky misto jedne tridy s
// nejakym enum tagem. Da se to pak hezky matchovat pres switch (pattern matching).
public abstract class Formula
{
}

// List stromu: promenna nebo jeji negace (proste literal).
public sealed class VarFormula : Formula
{
    // Jmeno promenne ze vstupu (napr. "a1").
    public string Name { get; }

    // True kdyz byl literal zapsany jako (not var).
    public bool Negated { get; }

    public VarFormula(string name, bool negated)
    {
        Name = name;
        Negated = negated;
    }
}

// Vnitrni uzel: AND dvou podformuli.
public sealed class AndFormula : Formula
{
    public Formula Left { get; }
    public Formula Right { get; }

    public AndFormula(Formula left, Formula right)
    {
        Left = left;
        Right = right;
    }
}

// Vnitrni uzel: OR dvou podformuli.
public sealed class OrFormula : Formula
{
    public Formula Left { get; }
    public Formula Right { get; }

    public OrFormula(Formula left, Formula right)
    {
        Left = left;
        Right = right;
    }
}
