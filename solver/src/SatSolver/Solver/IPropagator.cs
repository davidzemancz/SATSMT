using SatSolver.Cnf;

namespace SatSolver.Solver;

// =====================================================================
//  IPropagator - datova struktura pro unit propagaci
// =====================================================================
// Cely vtip ukolu 3 je porovnat dve ruzne implementace propagace
// (adjacency lists vs watched literals). Engine zustava porad stejny,
// vymeni se jenom tahle vec za rozhrani - takze obe varianty muzu pustit
// na to same a koukat ktera dela min prace.
public interface IPropagator
{
    // Inicializace nad formuli: propagator si jen naalokuje svoje pole
    // (occurrence listy / watchlisty) podle poctu promennych. Samotne
    // klauzule mu pak dava engine pres AddClause - trivialni klauzule
    // (prazdne a unit) si engine resi sam, takze sem chodi jen klauzule
    // o aspon dvou literalech.
    void Initialize(SearchEngine engine, CnfFormula formula);

    // Zpracuje frontu nove prirazenych literalu (od engine.QHead) a odvodi
    // dusledky. Vrati konfliktni klauzuli kdyz nejaka klauzule behem
    // propagace zfalsovatela cela, jinak null.
    Clause? Propagate();

    // Zaregistruje klauzuli do propagacni struktury. Predpoklada aspon dva
    // literaly (trivialni resi engine). Vola se pri inicializaci i pro
    // naucene klauzule v CDCL.
    void AddClause(Clause clause);

    // Kolikrat jsme se museli kouknout na nejakou klauzuli - tohle je ta
    // metrika kterou porovnavam adjacency lists vs watched (ukol 3).
    long ClausesChecked { get; }
}
