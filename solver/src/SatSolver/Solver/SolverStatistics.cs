namespace SatSolver.Solver;

// =====================================================================
//  SolverStatistics - citace pro mereni a reporty
// =====================================================================
// Zadani ukolu 2 chce aspon CPU cas, pocet rozhodnuti a pocet kroku unit
// propagace. Zbytek citacu jsem si pridal pro porovnani v dalsich ukolech
// (hlavne ClausesChecked na porovnani adjacency vs watched). Nechavam je
// schvalne jako verejne fieldy - je to jen takovy "bag of counters", neni
// duvod kolem toho delat property.
public sealed class SolverStatistics
{
    // Pocet rozhodnuti (vetveni), vcetne preklopeni faze v DPLL.
    public long Decisions;

    // Pocet literalu odvozenych unit propagaci (kroky propagace).
    public long Propagations;

    // Pocet konfliktu (prazdne klauzule co vznikly behem propagace).
    public long Conflicts;

    // Pocet naucenych klauzuli (CDCL).
    public long LearnedClauses;

    // Pocet smazanych naucenych klauzuli.
    public long DeletedClauses;

    // Pocet restartu.
    public long Restarts;

    // Kolikrat se propagator musel kouknout na nejakou klauzuli. Klicova
    // metrika na porovnani datovych struktur v ukolu 3.
    public long ClausesChecked;

    // Nejvyssi decision level kam jsme se behem hledani dostali.
    public int MaxDecisionLevel;

    // Celkovy CPU cas reseni.
    public TimeSpan SolveTime;

    // Hezky vypis statistik (jde na stderr u commandu solve). Bez diakritiky
    // at to vypada stejne i v terminalu co neumi UTF-8.
    public override string ToString()
    {
        return
            $"  cas (CPU)            : {SolveTime.TotalMilliseconds:F2} ms\n" +
            $"  rozhodnuti           : {Decisions}\n" +
            $"  unit-propagace       : {Propagations}\n" +
            $"  konflikty            : {Conflicts}\n" +
            $"  naucene klauzule     : {LearnedClauses}\n" +
            $"  smazane klauzule     : {DeletedClauses}\n" +
            $"  restarty             : {Restarts}\n" +
            $"  prohlednute klauzule : {ClausesChecked}\n" +
            $"  max. uroven          : {MaxDecisionLevel}";
    }
}
