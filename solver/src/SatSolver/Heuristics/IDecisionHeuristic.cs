using SatSolver.Cnf;
using SatSolver.Solver;

namespace SatSolver.Heuristics;

// =====================================================================
//  IDecisionHeuristic - jak solver vybira vetvici literal
// =====================================================================
// Tohle je vymenitelne (ukol 4 ruzne heuristiky porovnava). Hooky
// OnVariableBump / OnConflict / OnUnassign jsou tu kvuli dynamickym
// heuristikam (VSIDS), aby mohly reagovat na konflikty a na backtracking.
// Staticke heuristiky je proste nechaji prazdne.
public interface IDecisionHeuristic
{
    // Inicializace nad formuli (treba spocitani statickeho skore u JW).
    void Initialize(SearchEngine engine, CnfFormula formula);

    // Vrati vetvici literal (promennou + preferovanou fazi), nebo 0 kdyz uz je
    // vsechno prirazene. Engine pak muze fazi jeste prepsat podle phase savingu.
    int PickBranchLiteral();

    // CDCL: zvednout aktivitu promenne ktera se objevila v analyze konfliktu.
    void OnVariableBump(int var);

    // CDCL: zavola se po kazdem konfliktu (treba decay aktivit u VSIDS).
    void OnConflict();

    // Promenna byla pri backtrackingu odprirazena (VSIDS si ji vrati do haldy).
    void OnUnassign(int var);
}
