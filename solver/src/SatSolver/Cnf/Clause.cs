namespace SatSolver.Cnf;

// =====================================================================
//  Clause - jedna klauzule = disjunkce literalu  (napr. x1 | -x3 | x4)
// =====================================================================
// Literaly jsou ulozene v poli Literals. Schvalne to mam jako verejne
// pole ktere jde menit - propagace si bude prehazovat literaly na indexech
// 0 a 1 (stejne jako MiniSat), takze potrebuje sahat primo do toho pole.
// Vim ze public field neni ucebnicovy OOP, ale property by tu jen prekazela.
public sealed class Clause
{
    // Literaly klauzule.
    public int[] Literals;

    // Flag ze klauzule byla "logicky" smazana - propagatory ji pak preskoci.
    public bool Deleted;

    public Clause(int[] literals)
    {
        Literals = literals;
    }

    // Kolik literalu klauzule ma.
    public int Size => Literals.Length;

    // Hezky vypis pro debug, napr. "(x1 | -x3 | x4)".
    public override string ToString() =>
        "(" + string.Join(" | ", Literals.Select(Cnf.Literal.ToDisplay)) + ")";
}
