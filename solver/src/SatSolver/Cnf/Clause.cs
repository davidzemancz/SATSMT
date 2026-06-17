namespace SatSolver.Cnf;

// =====================================================================
//  Clause - jedna klauzule = disjunkce literalu  (napr. x1 | -x3 | x4)
// =====================================================================
// Literaly jsou ulozene v poli Literals. Schvalne to mam jako verejne
// pole ktere jde menit - watched literals propagace prehazuje literaly
// na indexech 0 a 1 (uplne stejne jako MiniSat), takze potrebuje sahat
// primo do toho pole. Vim ze public field neni zrovna ucebnicovy OOP,
// ale property s getterem/setterem by tady jen prekazela.
//
// Krome literalu si klauzule nese par metadat pro CDCL: jestli je naucena,
// jeji LBD a aktivitu (to se hodi az pri mazani naucenych klauzuli).
public sealed class Clause
{
    // Literaly klauzule. U watched literals jsou ty dva sledovane na indexu 0 a 1.
    public int[] Literals;

    // True kdyz je to klauzule naucena behem CDCL (ne z puvodni formule).
    public bool IsLearned;

    // LBD = Literal Block Distance. Pocet ruznych decision levelu mezi literaly
    // klauzule v okamziku kdy se naucila. Mensi LBD = uzitecnejsi klauzule (Glucose).
    public int Lbd;

    // Aktivita pro heuristiku mazani - zvedam ji kdyz se klauzule pouzije v konfliktu.
    public double Activity;

    // Flag ze klauzule byla "logicky" smazana (mazani naucenych klauzuli).
    // Propagatory smazane klauzule preskakuji, fyzicky se z poli vyhodi az davkou.
    public bool Deleted;

    public Clause(int[] literals, bool isLearned = false)
    {
        Literals = literals;
        IsLearned = isLearned;
    }

    // Kolik literalu klauzule ma.
    public int Size => Literals.Length;

    // Hezky vypis pro debug, napr. "(x1 | -x3 | x4)".
    public override string ToString() =>
        "(" + string.Join(" | ", Literals.Select(Cnf.Literal.ToDisplay)) + ")";
}
