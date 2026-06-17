namespace SatSolver.Restarts;

// =====================================================================
//  GeometricRestart - geometricka posloupnost prahu
// =====================================================================
// Prah zacne na nejake zakladni hodnote a po kazdem restartu se vynasobi
// konstantnim faktorem (> 1). Takze intervaly mezi restarty postupne rostou.
// Jednoduche, ale Luby je obvykle lepsi.
public sealed class GeometricRestart : IRestartStrategy
{
    private double _current;
    private readonly double _factor;

    public GeometricRestart(double initial, double factor)
    {
        _current = initial;
        _factor = factor;
    }

    public long NextThreshold()
    {
        long threshold = (long)_current;
        _current *= _factor;
        return Math.Max(1, threshold); // pojistka at nikdy nevratim 0
    }
}
