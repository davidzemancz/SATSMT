namespace SatSolver.Cnf;

// =====================================================================
//  Literal - pomocne funkce na praci s literaly
// =====================================================================
// Literal si nedrzim jako vlastni tridu, jenom jako int (DIMACS konvence,
// tak to delaji vsude). Diky tomu nemusim porad alokovat objekty:
//    kladne cislo   = promenna,  napr.  3  je literal x3
//    zaporne cislo  = negace,    napr. -3  je literal -x3
// Nula se jako literal nepouziva (v DIMACS oddeluje klauzule).
//
// Promenne cisluju od 1 do n (index 0 nechavam prazdny), aby kladny literal
// byl rovnou cislo promenne. Trochu se tim plytva jednim slotem v polich,
// ale je to mnohem min matouci nez posouvat indexy o 1.
public static class Literal
{
    // Promenna, ke ktere literal patri (vzdycky kladne cislo 1..n).
    public static int Var(int literal) => Math.Abs(literal);

    // True kdyz je literal kladny (x), false kdyz negovany (-x).
    public static bool IsPositive(int literal) => literal > 0;

    // Negace: z x udela -x a naopak. Je to fakt jen zmena znamenka :)
    public static int Negate(int literal) => -literal;

    // Prevede literal na nezaporny index do poli indexovanych literalem
    // (watchlisty, occurrence listy). Pro promennou v to mapuju takhle:
    //    kladny literal   v  -> 2*v
    //    zaporny literal  -v -> 2*v + 1
    // Takze pole indexovane literalem musi byt velke 2*(varCount+1).
    public static int ToIndex(int literal)
    {
        int v = Var(literal);
        return IsPositive(literal) ? 2 * v : 2 * v + 1;
    }

    // Kolik mista potrebuju pro pole, ktere chci indexovat pres ToIndex().
    public static int IndexArraySize(int varCount) => 2 * (varCount + 1);

    // Hezky vypis literalu pro debug / vystup, napr. "x3" nebo "-x3".
    public static string ToDisplay(int literal) =>
        (IsPositive(literal) ? "x" : "-x") + Var(literal);
}
