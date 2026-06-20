namespace SatSolver.Cli;

// =====================================================================
//  CliEntryPoint - rozcestnik celeho CLI
// =====================================================================
// Prvni argument je prikaz, zbytek jsou jeho parametry:
//   formula2cnf  - Tseitinovo kodovani NNF -> DIMACS (ukol 1)
//   solve        - obecne reseni (DPLL/CDCL, ukoly 2-5)
//   dpll         - zkratka pro zakladni DPLL (ukol 2)
//   bench        - davkove mereni do reportu
//   help         - napoveda
public static class CliEntryPoint
{
    public static int Run(string[] args)
    {
        // vystup chci v UTF-8 (kvuli pripadnym znakum v modelech atd.)
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* kdyz je output presmerovany, obcas to hodi vyjimku - kaslu na to */ }

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string command = args[0];
        string[] rest = args[1..]; // zbytek args bez prvniho (to je prikaz)

        // vlastni rozcestnik. Vyjimky chytam tady at uzivatel nevidi cely stacktrace.
        try
        {
            return command switch
            {
                "formula2cnf" => Formula2CnfCommand.Run(rest),
                "solve" => SolveCommand.Run(rest),
                "dpll" => SolveCommand.Run(PrependDpllDefaults(rest)),
                "bench" => BenchCommand.Run(rest),
                "help" or "--help" or "-h" => PrintUsageAndOk(),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Chyba: {ex.Message}");
            return 2;
        }
    }

    // "dpll" je jen solve s predvyplnenymi flagy ze zakladniho DPLL (ukol 2).
    private static string[] PrependDpllDefaults(string[] rest)
    {
        var defaults = new[] { "--dpll", "--prop", "adj", "--heuristic", "first" };
        return defaults.Concat(rest).ToArray();
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Neznamy prikaz '{command}'.");
        PrintUsage();
        return 1;
    }

    private static int PrintUsageAndOk()
    {
        PrintUsage();
        return 0;
    }

    private static void PrintUsage()
    {
        // napoveda jde na stderr, at neprekazi kdyz nekdo pipuje stdout
        Console.Error.WriteLine(
"""
SAT solver - pouziti:

  formula2cnf [vstup [vystup]] [--equiv | --implication]
      Prevede formuli v NNF (SMT-LIB) na CNF v DIMACS pomoci Tseitina.
      Bez souboru cte stdin a pise na stdout.
      --equiv        definice hradel jako ekvivalence (vychozi)
      --implication  jen implikace zleva doprava (Plaisted-Greenbaum)

  solve [vstup] [prepinace]
      Vyresi SAT. Format se pozna podle pripony (.cnf = DIMACS, .sat = SMT-LIB)
      nebo prepinacem --format cnf|sat.
      --dpll | --cdcl              rezim hledani (vychozi --cdcl)
      --prop adj | watched         propagace (vychozi watched)
      --heuristic first|random|jw|vsids   rozhodovaci heuristika (vychozi vsids)
      --restart none|geom|luby     restarty (vychozi luby v CDCL)
      --no-learn                   vypnout uceni klauzuli (i v CDCL rezimu)
      --no-delete                  nemazat naucene klauzule
      --phase-saving | --no-phase-saving
      --assume "1 -3 5"            predpoklady (literaly) jako rozhodnuti
      --seed N                     seed pro nahodnou heuristiku
      --format cnf|sat             vynutit format vstupu

  dpll [vstup]
      Zkratka pro zakladni DPLL (= solve --dpll --prop adj --heuristic first).

  bench [prepinace]
      Davkove mereni na benchmarkach (viz bench --help).
""");
    }
}
