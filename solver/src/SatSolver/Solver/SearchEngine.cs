using System.Diagnostics;
using SatSolver.Cnf;
using SatSolver.Heuristics;
using SatSolver.Restarts;

namespace SatSolver.Solver;

// =====================================================================
//  SearchEngine - srdce celeho solveru
// =====================================================================
// Jeden iterativni engine nad "trailem" (zasobnik prirazeni). Podle
// SolverOptions se chova bud jako DPLL (chronologicky backtracking, bez
// uceni), nebo jako CDCL (analyza konfliktu, 1-UIP uceni, backjumping,
// restarty, mazani klauzuli). Schvalne je to iterativni a ne rekurzivni -
// rekurze by na velkych instancich prepetekla stack.
//
// Datova struktura propagace i heuristika jsou vymenitelne, engine s nimi
// mluvi jen pres rozhrani (IPropagator / IDecisionHeuristic).
//
// Trida je rozsekana do dvou souboru (partial): tady je stav + hlavni smycka,
// v ConflictAnalysis.cs je analyza konfliktu, uceni a mazani klauzuli (jinak
// by to byl jeden megasoubor a v tom se neda vyznat).
public sealed partial class SearchEngine
{
    private readonly SolverOptions _options;
    private readonly SolverStatistics _stats = new();
    private readonly int _varCount;

    private readonly IPropagator _propagator;
    private readonly IDecisionHeuristic _heuristic;
    private readonly IRestartStrategy? _restart;

    // --- Stav prirazeni (pole indexovana promennou 1.._varCount) ---
    private readonly sbyte[] _value;       // 0 = neprirazeno, +1 = true, -1 = false
    private readonly int[] _level;         // decision level promenne (-1 = neprirazeno)
    private readonly Clause?[] _reason;     // duvodova klauzule (antecedent), null = rozhodnuti/fakt
    private readonly sbyte[] _savedPhase;  // phase saving: naposled prirazena hodnota (+1/-1, 0 = zadna)

    // --- Trail a decision levely ---
    private readonly List<int> _trail = new();        // prirazene literaly v poradi jak prisly
    private readonly List<int> _trailLim = new();     // _trailLim[d] = index v trailu kde zacina level d+1
    private readonly List<bool> _decisionFlipped = new(); // DPLL: byl uz tenhle level preklopen na druhou fazi?
    private int _qhead;                                // hlava fronty pro propagaci (index do _trail)

    // --- Databaze klauzuli ---
    private readonly List<Clause> _learned = new();   // naucene klauzule (jen CDCL)
    private bool _trivialUnsat;                        // prazdna klauzule na vstupu -> rovnou UNSAT

    // --- Pomocne buffery / citace ---
    private readonly bool[] _seen;                     // znackovani pri analyze konfliktu
    private long _conflictsSinceRestart;
    private long _restartThreshold;
    private int _maxLearned;
    private readonly Stopwatch _stopwatch = new();

    public SearchEngine(CnfFormula formula, SolverOptions options)
    {
        _options = options;
        _varCount = formula.VariableCount;

        // vsechna stavova pole jsou velka varCount+1, slot [0] se nepouziva
        _value = new sbyte[_varCount + 1];
        _level = new int[_varCount + 1];
        _reason = new Clause?[_varCount + 1];
        _savedPhase = new sbyte[_varCount + 1];
        _seen = new bool[_varCount + 1];
        Array.Fill(_level, -1);

        // poskladam si vymenitelne komponenty podle configu (viz SolverFactory)
        _propagator = SolverFactory.CreatePropagator(options);
        _heuristic = SolverFactory.CreateHeuristic(options);
        _restart = SolverFactory.CreateRestart(options);

        // init: propagator si naalokuje indexy, pak mu nahazim klauzule. Trivialni
        // (prazdne / unit) si vyresim rovnou sam v RegisterClause.
        _propagator.Initialize(this, formula);
        foreach (Clause clause in formula.Clauses)
            RegisterClause(clause);
        _heuristic.Initialize(this, formula);

        _maxLearned = Math.Max(100, formula.ClauseCount / 3);
        _restartThreshold = _restart?.NextThreshold() ?? long.MaxValue;
    }

    // ----- par "getteru" co potrebuji propagatory a analyza konfliktu -----

    // Nejvyssi index promenne.
    internal int VarCount => _varCount;

    // Aktualni decision level (= pocet rozhodnuti na trailu).
    internal int DecisionLevel => _trailLim.Count;

    // Trail (zasobnik prirazenych literalu) - sahaji do nej propagatory i analyza.
    internal List<int> Trail => _trail;

    // Hlava fronty propagace (index do trailu). Propagator si ji posouva sam.
    internal int QHead { get => _qhead; set => _qhead = value; }

    // Je promenna prirazena?
    internal bool IsAssigned(int var) => _value[var] != 0;

    // Na jakem levelu byla promenna prirazena.
    internal int LevelOf(int var) => _level[var];

    // Hodnota literalu: +1 pravdivy, -1 nepravdivy, 0 nedefinovany.
    internal int LiteralValue(int lit)
    {
        sbyte v = _value[Literal.Var(lit)];
        if (v == 0) return 0;
        bool varTrue = v == 1;
        bool litTrue = Literal.IsPositive(lit) ? varTrue : !varTrue;
        return litTrue ? 1 : -1;
    }

    internal bool LiteralIsTrue(int lit) => LiteralValue(lit) > 0;
    internal bool LiteralIsFalse(int lit) => LiteralValue(lit) < 0;

    // Priradi literal na aktualnim levelu s danym duvodem (null = rozhodnuti/fakt).
    // Vrati false kdyz uz je literal prirazeny opacne (konflikt) - to vyuziju hlavne
    // kdyz na zacatku nahazuju unit klauzule (dve protichudne unit klauzule = UNSAT).
    internal bool Enqueue(int lit, Clause? reason)
    {
        int v = Literal.Var(lit);
        sbyte want = Literal.IsPositive(lit) ? (sbyte)1 : (sbyte)-1;
        if (_value[v] != 0)
            return _value[v] == want; // uz prirazeno: ok kdyz to sedi, jinak konflikt

        _value[v] = want;
        _level[v] = DecisionLevel;
        _reason[v] = reason;
        _trail.Add(lit);
        if (reason != null)
            _stats.Propagations++; // pocitam jen odvozene literaly, ne rozhodnuti
        return true;
    }

    // Zaregistruje klauzuli do solveru. Trivialni pripady si resim rovnou tady:
    //   prazdna klauzule -> okamzity UNSAT
    //   unit klauzule    -> hned vynutim literal na levelu 0
    // Klauzule o aspon dvou literalech predam propagatoru.
    private void RegisterClause(Clause clause)
    {
        switch (clause.Size)
        {
            case 0:
                _trivialUnsat = true;                       // prazdna klauzule => nesplnitelne
                break;
            case 1:
                // unit klauzule => fakt na levelu 0. Kdyz uz je literal prirazeny opacne
                // (dve protichudne unit klauzule), je formule rovnou nesplnitelna.
                if (!Enqueue(clause.Literals[0], clause))
                    _trivialUnsat = true;
                break;
            default:
                _propagator.AddClause(clause);              // >=2 literaly => do propagacni struktury
                break;
        }
    }

    // Nove rozhodnuti: zvednu level a priradim vetvici literal.
    private void Decide(int lit)
    {
        _trailLim.Add(_trail.Count);
        _decisionFlipped.Add(false);
        if (DecisionLevel > _stats.MaxDecisionLevel)
            _stats.MaxDecisionLevel = DecisionLevel;
        _stats.Decisions++;
        Enqueue(lit, null);
    }

    // Backtrack na dany level: odpriradim vsechno nad nim (a po ceste si ulozim faze).
    internal void Backtrack(int level)
    {
        if (DecisionLevel <= level)
            return;

        int from = _trailLim[level];
        for (int i = _trail.Count - 1; i >= from; i--)
        {
            int v = Literal.Var(_trail[i]);
            if (_options.EnablePhaseSaving)
                _savedPhase[v] = _value[v]; // zapamatuj si posledni fazi
            _value[v] = 0;
            _reason[v] = null;
            _level[v] = -1;
            _heuristic.OnUnassign(v); // VSIDS si promennou vrati do haldy
        }
        _trail.RemoveRange(from, _trail.Count - from);
        _trailLim.RemoveRange(level, _trailLim.Count - level);
        _decisionFlipped.RemoveRange(level, _decisionFlipped.Count - level);
        _qhead = _trail.Count;
    }

    // Spusti hledani a vrati vysledek (status + model + statistiky).
    public SolverResult Solve()
    {
        _stopwatch.Restart();
        SatResult status = SearchLoop();
        _stopwatch.Stop();

        _stats.SolveTime = _stopwatch.Elapsed;
        _stats.ClausesChecked = _propagator.ClausesChecked;

        bool[]? model = status == SatResult.Satisfiable ? BuildModel() : null;
        return new SolverResult(status, model, _stats);
    }

    // ===== Hlavni smycka: propaguj -> (konflikt resi backtrack/uceni | jinak rozhodni) =====
    private SatResult SearchLoop()
    {
        if (_trivialUnsat)
            return SatResult.Unsatisfiable;

        while (true)
        {
            Clause? conflict = _propagator.Propagate();

            if (conflict != null)
            {
                _stats.Conflicts++;
                _conflictsSinceRestart++;

                // konflikt bez jedineho rozhodnuti => formule je nesplnitelna
                if (DecisionLevel == 0)
                    return SatResult.Unsatisfiable;

                bool useCdcl = _options.SearchMode == SearchMode.Cdcl && _options.EnableClauseLearning;
                if (useCdcl)
                {
                    // CDCL: naucim se klauzuli (1-UIP) a skocim zpatky na asertivni level.
                    (int[] learned, int backjump, int lbd) = Analyze(conflict);
                    _heuristic.OnConflict();
                    Backtrack(backjump);
                    AddLearnedClause(learned, lbd);

                    // kdyz uz je naucenych klauzuli moc, cast jich vyhodim
                    if (_options.EnableClauseDeletion && _learned.Count >= _maxLearned)
                    {
                        ReduceClauseDatabase();
                        _maxLearned += _maxLearned / 10; // limit musi v case rust, jinak bych mazal porad
                    }
                }
                else
                {
                    // DPLL: jen chronologicky backtracking (preklopim posledni nepreklopenou volbu)
                    if (!ChronologicalBacktrack())
                        return SatResult.Unsatisfiable;
                }
            }
            else
            {
                // zadny konflikt -> cas se rozhodnout (nebo pripadne restartovat)
                if (TimeLimitExceeded())
                    return SatResult.Unknown;

                if (_restart != null && _options.Restart != RestartKind.None
                    && _conflictsSinceRestart >= _restartThreshold)
                {
                    DoRestart();
                    continue;
                }

                int decision = NextDecisionLiteral();
                if (decision == 0)
                    return SatResult.Satisfiable;   // vsechno prirazene a zadny konflikt => mame model!

                Decide(decision);
            }
        }
    }

    // Vybere dalsi vetvici literal pres heuristiku. Vrati 0 kdyz uz je vsechno prirazene.
    private int NextDecisionLiteral()
    {
        int lit = _heuristic.PickBranchLiteral();
        if (lit == 0)
            return 0;

        // phase saving: kdyz mam ulozenou fazi, pouziju ji (prebije fazi z heuristiky)
        if (_options.EnablePhaseSaving)
        {
            int v = Literal.Var(lit);
            if (_savedPhase[v] != 0)
                lit = _savedPhase[v] == 1 ? v : -v;
        }
        return lit;
    }

    // DPLL chronologicky backtracking: najde posledni decision level co jeste nebyl
    // preklopeny, vrati se nad nej a priradi opacnou fazi vetviciho literalu. Vrati
    // false kdyz uz takovy level neni (cely strom projity => UNSAT).
    private bool ChronologicalBacktrack()
    {
        int d = DecisionLevel;
        while (d > 0 && _decisionFlipped[d - 1])
            d--;
        if (d == 0)
            return false;

        int decisionLit = _trail[_trailLim[d - 1]]; // vetvici literal levelu d
        Backtrack(d - 1);
        Decide(Literal.Negate(decisionLit));          // novy level d, opacna faze
        _decisionFlipped[DecisionLevel - 1] = true;    // tenhle level uz znova nepreklapet
        return true;
    }

    // Restart: zahodim trail (zpatky na level 0), ale naucene klauzule i ulozene faze
    // zustavaji - to je presne to co restartum dava smysl.
    private void DoRestart()
    {
        _stats.Restarts++;
        Backtrack(0);
        _conflictsSinceRestart = 0;
        _restartThreshold = _restart!.NextThreshold();
    }

    private bool TimeLimitExceeded() =>
        _options.TimeLimitSeconds > 0 && _stopwatch.Elapsed.TotalSeconds > _options.TimeLimitSeconds;

    // Sestavi model z aktualniho prirazeni. Neprirazene promenne nastavim na false
    // (na hodnote nezalezi, formule uz je splnena).
    private bool[] BuildModel()
    {
        var model = new bool[_varCount + 1];
        for (int v = 1; v <= _varCount; v++)
            model[v] = _value[v] == 1;
        return model;
    }
}
