using System.Diagnostics;
using System.Globalization;
using System.Text;
using SatSolver.Cnf;
using SatSolver.Encoding;
using SatSolver.Parsing;
using SatSolver.Solver;

namespace SatSolver.Cli;

// =====================================================================
//  BenchCommand - prikaz "bench" (davkove mereni do reportu)
// =====================================================================
// Dva rezimy:
//   * solver (vychozi)  - pusti matici (instance x konfigurace) a vyplivne
//                         statistiky reseni (DPLL/CDCL). Z toho delam cisla
//                         do reportu k ukolum 2-5.
//   * convert (--convert) - misto reseni meri Tseitinuv prevod .sat -> CNF
//                         (ukol 1): cas + kolik vznikne promennych a klauzuli.
//
// Pouziti:
//   bench <soubory/slozky...> --configs "label:flagy;label2:flagy2" [--timeout S] [--limit N] [--avg]
//   bench <soubory/slozky...> --convert [--configs "equiv:--equiv;impl:--implication"] [--limit N] [--avg]
// Priklad:
//   bench benchmarks/phole/hole6.cnf benchmarks/phole/hole7.cnf \
//         --configs "adj:--dpll --prop adj;watched:--dpll --prop watched"
//   bench benchmarks/task1 --convert
public static class BenchCommand
{
    // jedna solver konfigurace = nazev (do tabulky) + nastaveni solveru
    private sealed record Config(string Label, SolverOptions Options);

    // jeden radek solver vysledku = (instance, konfigurace, jak to dopadlo)
    private sealed record Row(string Instance, string Config, SolverResult Result);

    // convert konfigurace = nazev + zpusob kodovani hradel (equiv vs implikace)
    private sealed record ConvertConfig(string Label, bool UseEquivalences);

    // jeden radek convert vysledku = instance, konfigurace, spocitane velikosti + cas
    private sealed record ConvertRow(string Instance, string Config,
        int InputVars, int AuxVars, int TotalVars, int Clauses, TimeSpan Time);

    public static int Run(string[] args)
    {
        var instancePaths = new List<string>();
        string? rawConfigs = null;    // parsovani odlozim, lisi se podle rezimu
        double timeout = 10.0;        // default timeout na jeden beh (jen solver rezim)
        int limit = int.MaxValue;     // kolik souboru max vzit ze slozky
        bool average = false;         // --avg agreguje pres instance
        bool convert = false;         // --convert => merime prevod, ne reseni

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--configs":
                    rawConfigs = CommandLineOptions.Value(args, ref i);
                    break;
                case "--convert":
                    convert = true;
                    break;
                case "--timeout":
                    timeout = double.Parse(CommandLineOptions.Value(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--limit":
                    limit = int.Parse(CommandLineOptions.Value(args, ref i));
                    break;
                case "--avg":
                    average = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Neznamy prepinac '{arg}'.");
                    instancePaths.Add(arg);
                    break;
            }
        }

        List<string> instances = ExpandInstances(instancePaths, limit);
        if (instances.Count == 0)
            throw new ArgumentException("Nenasel jsem zadne instance.");

        return convert
            ? RunConvert(instances, rawConfigs, average)
            : RunSolve(instances, rawConfigs, timeout, average);
    }

    // --- solver rezim (ukoly 2-5) ---

    private static int RunSolve(List<string> instances, string? rawConfigs, double timeout, bool average)
    {
        if (rawConfigs == null)
            throw new ArgumentException("Zadej aspon jednu konfiguraci pres --configs.");
        var configs = ParseConfigs(rawConfigs).ToList();
        if (configs.Count == 0)
            throw new ArgumentException("Zadej aspon jednu konfiguraci pres --configs.");

        // timeout aplikuju na kazdy beh zvlast
        foreach (Config cfg in configs)
            cfg.Options.TimeLimitSeconds = timeout;

        var rows = new List<Row>();
        foreach (string path in instances)
        {
            CnfFormula original = LoadCnf(path);
            foreach (Config cfg in configs)
            {
                // engine prehazuje literaly v klauzulich -> kazdy beh dostane cistou kopii
                var engine = new SearchEngine(original.Clone(), cfg.Options);
                SolverResult result = engine.Solve();
                rows.Add(new Row(Path.GetFileName(path), cfg.Label, result));
            }
            Console.Error.WriteLine($"hotovo: {Path.GetFileName(path)}"); // progress na stderr at vidim ze to zije
        }

        Console.Write(average ? FormatAverages(rows) : FormatPerInstance(rows));
        return 0;
    }

    // --- convert rezim (ukol 1): merime Tseitinuv prevod .sat -> CNF, ne reseni ---

    private static int RunConvert(List<string> instances, string? rawConfigs, bool average)
    {
        // bez --configs jedu jednu variantu (plne ekvivalence). Jinak si z configu
        // vytahnu jen jestli jde o --equiv nebo --implication (heuristiky tu nedavaji smysl).
        var configs = rawConfigs != null
            ? ParseConvertConfigs(rawConfigs).ToList()
            : new List<ConvertConfig> { new("equiv", true) };

        var rows = new List<ConvertRow>();
        foreach (string path in instances)
        {
            if (InputLoader.DetectFromPath(path) != InputFormat.SmtLib)
                throw new ArgumentException($"--convert ceka .sat vstup, ale '{Path.GetFileName(path)}' vypada na DIMACS.");

            // formuli naparsuju jednou, merim az samotne kodovani (parsovani neni soucast Tseitina)
            Formula formula = SmtLibParser.Parse(File.ReadAllText(path));
            foreach (ConvertConfig cfg in configs)
            {
                var sw = Stopwatch.StartNew();
                CnfFormula cnf = new TseitinEncoder(cfg.UseEquivalences).Encode(formula);
                sw.Stop();
                rows.Add(new ConvertRow(Path.GetFileName(path), cfg.Label,
                    cnf.VariableNames.Count, cnf.AuxiliaryVariables.Count, cnf.VariableCount, cnf.ClauseCount, sw.Elapsed));
            }
            Console.Error.WriteLine($"hotovo: {Path.GetFileName(path)}");
        }

        Console.Write(average ? FormatConvertAverages(rows) : FormatConvertPerInstance(rows));
        return 0;
    }

    // --- parsovani solver konfiguraci ze stringu "label:flagy;label2:flagy2" ---

    private static IEnumerable<Config> ParseConfigs(string spec)
    {
        foreach (string part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = part.IndexOf(':');
            string label = colon >= 0 ? part[..colon].Trim() : part.Trim();
            string flags = colon >= 0 ? part[(colon + 1)..].Trim() : part.Trim();
            yield return new Config(label, CommandLineOptions.ParseFlags(flags));
        }
    }

    // --- parsovani convert konfiguraci: z flagu beru jen zpusob kodovani hradel ---

    private static IEnumerable<ConvertConfig> ParseConvertConfigs(string spec)
    {
        foreach (string part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = part.IndexOf(':');
            string label = colon >= 0 ? part[..colon].Trim() : part.Trim();
            string flags = colon >= 0 ? part[(colon + 1)..].Trim() : part.Trim();
            // implikace (Plaisted-Greenbaum) kdyz je v flagu --implication/--impl, jinak plne ekvivalence
            bool equiv = !(flags.Contains("--implication") || flags.Contains("--impl"));
            yield return new ConvertConfig(label, equiv);
        }
    }

    // --- nacitani instanci (soubor nebo cela slozka *.cnf / *.sat) ---

    private static List<string> ExpandInstances(List<string> paths, int limit)
    {
        var result = new List<string>();
        foreach (string p in paths)
        {
            if (Directory.Exists(p))
                // ze slozky beru .cnf i .sat (.sat si LoadCnf stejne prevede pres InputLoader)
                result.AddRange(Directory.GetFiles(p, "*.cnf").Concat(Directory.GetFiles(p, "*.sat"))
                    .OrderBy(x => x, StringComparer.Ordinal).Take(limit));
            else if (File.Exists(p))
                result.Add(p);
            else
                throw new FileNotFoundException($"Cesta '{p}' neexistuje.");
        }
        return result;
    }

    private static CnfFormula LoadCnf(string path)
    {
        string text = File.ReadAllText(path);
        InputFormat format = InputLoader.DetectFromPath(path);
        return InputLoader.LoadAsCnf(text, format);
    }

    // --- formatovani vystupu (Markdown tabulky, bez diakritiky at se to hezky kopiruje) ---

    private static string Status(SolverResult r) => r.Status switch
    {
        SatResult.Satisfiable => "SAT",
        SatResult.Unsatisfiable => "UNSAT",
        _ => "TIMEOUT"
    };

    // maly helper na formatovani doublu na 1 desetinne misto (porad invariant culture)
    private static string F(double x) => x.ToString("F1", CultureInfo.InvariantCulture);

    private static string FormatPerInstance(List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| instance | konfigurace | vysledek | cas [ms] | rozhodnuti | propagace | konflikty | prohlednuto | restarty |");
        sb.AppendLine("|---|---|---|--:|--:|--:|--:|--:|--:|");
        foreach (Row r in rows)
        {
            SolverStatistics s = r.Result.Statistics;
            sb.AppendLine($"| {r.Instance} | {r.Config} | {Status(r.Result)} | {F(s.SolveTime.TotalMilliseconds)} | " +
                          $"{s.Decisions} | {s.Propagations} | {s.Conflicts} | {s.ClausesChecked} | {s.Restarts} |");
        }
        return sb.ToString();
    }

    private static string FormatAverages(List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| konfigurace | #instanci | SAT | UNSAT | TIMEOUT | avg cas [ms] | avg rozhodnuti | avg propagace | avg konflikty | avg prohlednuto |");
        sb.AppendLine("|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|");

        // seskupim radky podle konfigurace a spocitam prumery
        foreach (var group in rows.GroupBy(r => r.Config))
        {
            var list = group.ToList();
            int sat = list.Count(r => r.Result.Status == SatResult.Satisfiable);
            int unsat = list.Count(r => r.Result.Status == SatResult.Unsatisfiable);
            int to = list.Count(r => r.Result.Status == SatResult.Unknown); // Unknown = doslo na timeout
            double avgTime = list.Average(r => r.Result.Statistics.SolveTime.TotalMilliseconds);
            double avgDec = list.Average(r => (double)r.Result.Statistics.Decisions);
            double avgProp = list.Average(r => (double)r.Result.Statistics.Propagations);
            double avgConf = list.Average(r => (double)r.Result.Statistics.Conflicts);
            double avgChecked = list.Average(r => (double)r.Result.Statistics.ClausesChecked);

            sb.AppendLine($"| {group.Key} | {list.Count} | {sat} | {unsat} | {to} | {F(avgTime)} | " +
                          $"{F(avgDec)} | {F(avgProp)} | {F(avgConf)} | {F(avgChecked)} |");
        }
        return sb.ToString();
    }

    private static string FormatConvertPerInstance(List<ConvertRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| instance | konfigurace | cas [ms] | vstupni prom. | pomocne prom. | celkem prom. | klauzule |");
        sb.AppendLine("|---|---|--:|--:|--:|--:|--:|");
        foreach (ConvertRow r in rows)
            sb.AppendLine($"| {r.Instance} | {r.Config} | {F(r.Time.TotalMilliseconds)} | " +
                          $"{r.InputVars} | {r.AuxVars} | {r.TotalVars} | {r.Clauses} |");
        return sb.ToString();
    }

    private static string FormatConvertAverages(List<ConvertRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| konfigurace | #instanci | avg cas [ms] | avg vstupni | avg pomocne | avg celkem | avg klauzule |");
        sb.AppendLine("|---|--:|--:|--:|--:|--:|--:|");
        foreach (var group in rows.GroupBy(r => r.Config))
        {
            var list = group.ToList();
            sb.AppendLine($"| {group.Key} | {list.Count} | {F(list.Average(r => r.Time.TotalMilliseconds))} | " +
                          $"{F(list.Average(r => (double)r.InputVars))} | {F(list.Average(r => (double)r.AuxVars))} | " +
                          $"{F(list.Average(r => (double)r.TotalVars))} | {F(list.Average(r => (double)r.Clauses))} |");
        }
        return sb.ToString();
    }
}
