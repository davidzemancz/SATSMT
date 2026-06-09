using SatSolver.Parsing;

namespace SatSolver.Parsing;

// =====================================================================
//  SmtLibParser - parser zjednoduseneho SMT-LIB formatu (.sat)
// =====================================================================
// Vstup je jedna formule v NNF podle tehle gramatiky (ukol 1):
//   formula ::= '(' 'and' formula formula ')'
//             | '(' 'or'  formula formula ')'
//             | '(' 'not' variable ')'
//             | variable
// kde promenna je posloupnost alfanumerickych znaku zacinajici pismenem.
// Mezery a newliny muzou byt vsude kde muze byt mezera.
//
// Napsal jsem to jako recursive descent (rekurzivni sestup) nad jednoduchym
// tokenizerem - resi se vlastne jen zavorky + identifikatory, takze zadna
// veda. Drzim si pozici v textu (_pos) a posouvam se po znacich.
public sealed class SmtLibParser
{
    private readonly string _input;
    private int _pos; // kde zrovna v textu jsem

    private SmtLibParser(string text)
    {
        _input = text;
    }

    // Hlavni vstupni bod - z retezce udela strom Formula.
    public static Formula Parse(string text)
    {
        var parser = new SmtLibParser(text);
        Formula f = parser.ParseFormula();
        parser.SkipWhitespace();
        // za formuli uz nesmi nic dalsiho byt (krome mezer)
        if (!parser.AtEnd)
            throw new FormatException($"Za formuli je jeste neco navic, pozice {parser._pos}.");
        return f;
    }

    private bool AtEnd => _pos >= _input.Length;

    // Preskoci vsechny mezery/newliny/taby.
    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(_input[_pos]))
            _pos++;
    }

    // Kouknu na aktualni znak ale neposunu se (nejdriv preskocim mezery).
    private char Peek()
    {
        SkipWhitespace();
        if (AtEnd)
            throw new FormatException("Necekany konec vstupu.");
        return _input[_pos];
    }

    // Precte identifikator = bud promennou, nebo klicove slovo and/or/not.
    private string ReadIdentifier()
    {
        SkipWhitespace();
        int start = _pos;
        if (AtEnd || !char.IsLetter(_input[_pos]))
            throw new FormatException($"Cekal jsem identifikator na pozici {_pos}.");
        _pos++; // prvni znak musi byt pismeno, dalsi uz muzou byt i cislice
        while (!AtEnd && char.IsLetterOrDigit(_input[_pos]))
            _pos++;
        return _input[start.._pos];
    }

    // Srdce parseru - jedno pravidlo gramatiky, rekurzivne se vola na podformule.
    private Formula ParseFormula()
    {
        if (Peek() == '(')
        {
            _pos++; // sezeru '('
            string keyword = ReadIdentifier();
            Formula result = keyword switch
            {
                "and" => new AndFormula(ParseFormula(), ParseFormula()),
                "or" => new OrFormula(ParseFormula(), ParseFormula()),
                // (not <variable>) - negace smi byt jen nad promennou, protoze vstup je v NNF
                "not" => new VarFormula(ReadIdentifier(), negated: true),
                _ => throw new FormatException($"Neznamy operator '{keyword}', cekal jsem and/or/not.")
            };
            Expect(')');
            return result;
        }

        // jinak je to proste hola promenna (kladny literal)
        return new VarFormula(ReadIdentifier(), negated: false);
    }

    // Pomocna: zkontroluje ze tady opravdu je dany znak a sezere ho.
    private void Expect(char c)
    {
        if (Peek() != c)
            throw new FormatException($"Cekal jsem znak '{c}' na pozici {_pos}.");
        _pos++;
    }
}
