namespace SatSolver.Solver;

// =====================================================================
//  SolverStatistics - citace pro mereni a reporty
// =====================================================================
// Zadani ukolu 2 chce aspon CPU cas, pocet rozhodnuti a pocet kroku unit
// propagace. Par dalsich citacu si pridavam na porovnani (hlavne
// ClausesChecked na porovnani datovych struktur). Nechavam je schvalne jako
// verejne fieldy - je to jen takovy "bag of counters".
public sealed class SolverStatistics
{
    // Pocet rozhodnuti (vetveni), vcetne preklopeni faze v DPLL.
    public long Decisions;

    // Pocet literalu odvozenych unit propagaci (kroky propagace).
    public long Propagations;

    // Pocet konfliktu (klauzule co behem propagace zfalsovatela cela).
    public long Conflicts;

    // Kolikrat se propagator musel kouknout na nejakou klauzuli.
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
            $"  prohlednute klauzule : {ClausesChecked}\n" +
            $"  max. uroven          : {MaxDecisionLevel}";
    }
}
