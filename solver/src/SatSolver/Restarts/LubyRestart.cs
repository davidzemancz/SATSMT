namespace SatSolver.Restarts;

// =====================================================================
//  LubyRestart - restarty podle Lubyho posloupnosti
// =====================================================================
// (Luby, Sinclair, Zuckerman 1993.) Posloupnost je u * t_i, kde t_i jde:
//   1,1,2,1,1,2,4,1,1,2,1,1,2,4,8,...
// Tzn. vetsinou kratke behy (caste restarty), ale obcas nechame solver bezet
// mnohem dyl. Tohle je v praxi nejlepsi default, proto je to vychozi strategie.
// u je zakladni jednotka (kolikrat se ten Lubyho clen vynasobi).
public sealed class LubyRestart : IRestartStrategy
{
    private readonly long _unit;
    private long _index; // poradi restartu (1-based)

    public LubyRestart(long unit) => _unit = unit;

    public long NextThreshold()
    {
        _index++;
        return _unit * Luby(_index);
    }

    // i-ty clen Lubyho posloupnosti (i je 1-based). Vzorecek jsem si nasel,
    // sam bych ho asi nevymyslel :D
    private static long Luby(long i)
    {
        // kdyz i = 2^k - 1, vrat 2^(k-1)
        for (int k = 1; k < 63; k++)
        {
            if (i == (1L << k) - 1)
                return 1L << (k - 1);
        }
        // jinak najdi k tak, ze 2^(k-1) <= i < 2^k - 1, a rekurzivne t_{i - 2^(k-1) + 1}
        for (int k = 1; ; k++)
        {
            if ((1L << (k - 1)) <= i && i < (1L << k) - 1)
                return Luby(i - (1L << (k - 1)) + 1);
        }
    }
}
