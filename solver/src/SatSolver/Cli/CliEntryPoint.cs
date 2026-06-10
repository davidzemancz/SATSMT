namespace SatSolver.Cli;

// =====================================================================
//  CliEntryPoint - rozcestnik celeho CLI
// =====================================================================
// Prvni argument je prikaz, zbytek jsou jeho parametry:
//   formula2cnf  - Tseitinovo kodovani NNF -> DIMACS (ukol 1)
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
""");
    }
}
