namespace SatSolver.Restarts;

// =====================================================================
//  IRestartStrategy - kdy ma solver restartovat
// =====================================================================
// Engine pocita konflikty od posledniho restartu, a kdyz prekroci prah, tak
// restartuje (zahodi trail, ale ne naucene klauzule) a zepta se na dalsi prah.
// Prahy musi v case rust, jinak by solver porad jen restartoval a nikdy nic
// nedoresil (to se mi fakt stalo nez jsem to pochopil).
public interface IRestartStrategy
{
    // Vrati dalsi prah = za kolik konfliktu zase restartovat.
    long NextThreshold();
}
