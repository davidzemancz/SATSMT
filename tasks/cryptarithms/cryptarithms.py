import argparse
import os
import re
import subprocess
import sys
import tempfile


class Atom:
    def __init__(self, lhs, rhs):
        self.lhs = lhs
        self.rhs = rhs

    def terms(self):
        # pravou stranu vezmu se zapornym znamenkem
        return list(self.lhs) + [(-s, w) for s, w in self.rhs]

class Not:
    def __init__(self, child):
        self.child = child

class And:
    def __init__(self, children):
        self.children = children

class Or:
    def __init__(self, children):
        self.children = children


# --- parser (tokenizer + rekurzivni sestup) ---
# priorita: NOT > AND > OR

_TOKEN_RE = re.compile(r"""
      (?P<ws>\s+)
    | (?P<and>&&)
    | (?P<or>\|\|)
    | (?P<not>[!~])
    | (?P<lp>\()
    | (?P<rp>\))
    | (?P<plus>\+)
    | (?P<minus>-)
    | (?P<eq>=)
    | (?P<word>[A-Za-z][A-Za-z0-9_]*)
""", re.VERBOSE)

_KEYWORDS = {"AND": "and", "OR": "or", "NOT": "not"}

def tokenize(text):
    text = "\n".join(line.split("#", 1)[0] for line in text.splitlines())  # zahodim komentare
    tokens = []
    pos = 0
    while pos < len(text):
        m = _TOKEN_RE.match(text, pos)
        if not m:
            raise ValueError(f"neznamy znak u pozice {pos}: {text[pos:pos+20]!r}")
        pos = m.end()
        kind = m.lastgroup
        val = m.group()
        if kind == "ws":
            continue
        if kind == "word":
            kw = _KEYWORDS.get(val.upper())
            if kw is not None:
                tokens.append((kw, val))
                continue
        tokens.append((kind, val))
    tokens.append(("eof", ""))
    return tokens


class Parser:
    def __init__(self, tokens):
        self.toks = tokens
        self.i = 0

    def peek(self):
        return self.toks[self.i][0]

    def next(self):
        t = self.toks[self.i]
        self.i += 1
        return t

    def expect(self, kind):
        if self.peek() != kind:
            raise ValueError(f"ocekavano {kind}, prislo {self.toks[self.i]}")
        return self.next()

    def parse(self):
        node = self.parse_or()
        self.expect("eof")
        return node

    def parse_or(self):
        nodes = [self.parse_and()]
        while self.peek() == "or":
            self.next()
            nodes.append(self.parse_and())
        return nodes[0] if len(nodes) == 1 else Or(nodes)

    def parse_and(self):
        nodes = [self.parse_not()]
        while self.peek() == "and":
            self.next()
            nodes.append(self.parse_not())
        return nodes[0] if len(nodes) == 1 else And(nodes)

    def parse_not(self):
        if self.peek() == "not":
            self.next()
            return Not(self.parse_not())
        return self.parse_primary()

    def parse_primary(self):
        if self.peek() == "lp":
            self.next()
            node = self.parse_or()
            self.expect("rp")
            return node
        return self.parse_equation()

    # parsuju vzrayz
    _WORDLIKE = ("word", "and", "or", "not")

    def expect_word(self):
        if self.peek() in self._WORDLIKE:
            return self.next()[1]
        raise ValueError(f"neplatne slovo {self.toks[self.i]}")

    def parse_side(self):
        terms = []
        sign = 1
        if self.peek() in ("plus", "minus"):
            sign = 1 if self.next()[0] == "plus" else -1
        terms.append((sign, self.expect_word()))
        while self.peek() in ("plus", "minus"):
            sign = 1 if self.next()[0] == "plus" else -1
            terms.append((sign, self.expect_word()))
        return terms

    def parse_equation(self):
        lhs = self.parse_side()
        self.expect("eq")
        rhs = self.parse_side()
        return Atom(lhs, rhs)


def parse_formula(text):
    if not text.strip():
        raise ValueError("prazdny vstup")
    return Parser(tokenize(text)).parse()


def all_atoms(node):
    # projde strom a vrati vsechny rovnice
    out = []
    if isinstance(node, Atom):
        out.append(node)
    elif isinstance(node, Not):
        out += all_atoms(node.child)
    elif isinstance(node, (And, Or)):
        for c in node.children:
            out += all_atoms(c)
    return out


def collect_letters(node):
    out = set()
    for atom in all_atoms(node):
        for _, w in atom.terms():
            out.update(w)
    return out


def collect_leading_letters(node):
    # prvni pismeno kazdeho slova (nesmi byt nula)
    out = set()
    for atom in all_atoms(node):
        for _, w in atom.terms():
            if w:
                out.add(w[0])
    return out


# --- stavba CNF (jednoducha hradla pres Tseitin + ripple scitani) ---
# - bity drzim jako literaly (nenulove inty). promenna 1 je porad true, takze TRUE = 1 a FALSE = -1. u hradel osetrim konstantni vstupy, at zbytecne nepribyvaji pomocne promenne (treba pri scitani kratsiho a delsiho cisla).

class CnfBuilder:
    def __init__(self):
        self.num_vars = 0
        self.clauses = []
        self.TRUE = self.new_var()
        self.FALSE = -self.TRUE
        self.add([self.TRUE])  # promenna 1 je vzdy true

    def new_var(self):
        self.num_vars += 1
        return self.num_vars

    def add(self, clause):
        self.clauses.append(list(clause))

    def g_and(self, a, b):
        if a == self.FALSE or b == self.FALSE:
            return self.FALSE
        if a == self.TRUE:
            return b
        if b == self.TRUE:
            return a
        c = self.new_var()
        self.add([-a, -b, c])
        self.add([a, -c])
        self.add([b, -c])
        return c

    def g_or(self, a, b):
        if a == self.TRUE or b == self.TRUE:
            return self.TRUE
        if a == self.FALSE:
            return b
        if b == self.FALSE:
            return a
        c = self.new_var()
        self.add([a, b, -c])
        self.add([-a, c])
        self.add([-b, c])
        return c

    def g_xor(self, a, b):
        if a == self.FALSE:
            return b
        if b == self.FALSE:
            return a
        if a == self.TRUE:
            return -b
        if b == self.TRUE:
            return -a
        c = self.new_var()
        self.add([-a, -b, -c])
        self.add([a, b, -c])
        self.add([a, -b, c])
        self.add([-a, b, c])
        return c

    def and_all(self, lits):
        acc = self.TRUE
        for x in lits:
            acc = self.g_and(acc, x)
        return acc

    def or_all(self, lits):
        acc = self.FALSE
        for x in lits:
            acc = self.g_or(acc, x)
        return acc

    # --- aritmetika nad bitvektory (LSB) ---
    def full_adder(self, a, b, cin):
        axb = self.g_xor(a, b)
        s = self.g_xor(axb, cin)
        cout = self.g_or(self.g_and(a, b), self.g_and(axb, cin))
        return s, cout

    def ripple_add(self, A, B):
        n = max(len(A), len(B))
        A = A + [self.FALSE] * (n - len(A))
        B = B + [self.FALSE] * (n - len(B))
        out = []
        carry = self.FALSE
        for i in range(n):
            s, carry = self.full_adder(A[i], B[i], carry)
            out.append(s)
        out.append(carry)
        return out

    def mul_const(self, A, c):
        # A * c jako soucet posunutych kopii A 
        acc = [self.FALSE]
        shift = 0
        while c > 0:
            if c & 1:
                acc = self.ripple_add(acc, [self.FALSE] * shift + A)
            c >>= 1
            shift += 1
        return acc

    def sum_all(self, vecs):
        acc = [self.FALSE]
        for v in vecs:
            acc = self.ripple_add(acc, v)
        return acc

    def equal(self, A, B):
        # vrati literal A = B
        n = max(len(A), len(B))
        A = A + [self.FALSE] * (n - len(A))
        B = B + [self.FALSE] * (n - len(B))
        return self.and_all([-self.g_xor(A[i], B[i]) for i in range(n)])

    def digit_below_base(self, bits, base):
        # zakaze, aby hodnota bitu (LSB) byla >= base. 
        # pro baze, co nejsou mocnina 2, proste zakazu kazdy "prebytecny" vzor bitu.
        k = len(bits)
        for v in range(base, 1 << k):
            clause = []
            for i in range(k):
                clause.append(-bits[i] if (v >> i) & 1 else bits[i])
            self.add(clause)


# --- enkoder kryptaritmu ---

class Encoding:
    def __init__(self, cnf, letters, bits):
        self.cnf = cnf
        self.letters = letters
        self.bits = bits          # pismeno -> bity ... LSB


def encode(node, base=10, all_different=True):
    cnf = CnfBuilder()
    letters = sorted(collect_letters(node))
    if not letters:
        raise ValueError("formule neobsahuje zadna pismena")
    k = max(1, (base - 1).bit_length())   # bitu na cislici
    bits = {L: [cnf.new_var() for _ in range(k)] for L in letters}

    # cislice < base
    for L in letters:
        cnf.digit_below_base(bits[L], base)

    # vedouci pismeno nesmi byt nula -> aspon jeden bit je true
    for L in collect_leading_letters(node):
        cnf.add(bits[L])

    # all-different: kazde dve pismena se lisi aspon v jednom bitu
    if all_different:
        for i in range(len(letters)):
            for j in range(i + 1, len(letters)):
                a, b = bits[letters[i]], bits[letters[j]]
                cnf.add([cnf.g_xor(a[t], b[t]) for t in range(k)])

    def word_value(word):
        acc = list(bits[word[0]])
        for ch in word[1:]:
            acc = cnf.ripple_add(cnf.mul_const(acc, base), bits[ch])
        return acc

    def atom_lit(atom):
        pos = [word_value(w) for s, w in atom.terms() if s > 0]
        neg = [word_value(w) for s, w in atom.terms() if s < 0]
        return cnf.equal(cnf.sum_all(pos), cnf.sum_all(neg))

    def encode_node(n):
        if isinstance(n, Atom):
            return atom_lit(n)
        if isinstance(n, Not):
            return -encode_node(n.child)
        if isinstance(n, And):
            return cnf.and_all([encode_node(c) for c in n.children])
        if isinstance(n, Or):
            return cnf.or_all([encode_node(c) for c in n.children])
        raise TypeError(f"neznamy uzel {n!r}")

    cnf.add([encode_node(node)])  # cela formule musi platit
    return Encoding(cnf, letters, bits)


def decode_assignment(enc, true_vars):
    out = {}
    for L in enc.letters:
        v = 0
        for i, lit in enumerate(enc.bits[L]):
            if lit in true_vars:
                v |= (1 << i)
        out[L] = v
    return out


def blocking_clause(enc, assign):
    clause = []
    for L in enc.letters:
        v = assign[L]
        for i, lit in enumerate(enc.bits[L]):
            clause.append(-lit if (v >> i) & 1 else lit)
    return clause


# --- muj C# CDCL, pres DIMACS ---

def _solver_dll():
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.normpath(os.path.join(
        here, "..", "..", "solver", "src", "SatSolver",
        "bin", "Release", "net10.0", "SatSolver.dll"))


def dimacs_text(num_vars, clauses):
    parts = [f"p cnf {num_vars} {len(clauses)}\n"]
    for cl in clauses:
        parts.append(" ".join(map(str, cl)) + " 0\n")
    return "".join(parts)


_CPU_RE = re.compile(r"cas \(CPU\)\s*:\s*([\d.,]+)\s*ms")


def run_mysolver(num_vars, clauses):
    # vrati (sat, mnozina true literalu)
    dll = _solver_dll()
    if not os.path.exists(dll):
        raise FileNotFoundError(f"nenalezen solver: {dll}\n")
    with tempfile.NamedTemporaryFile("w", suffix=".cnf", delete=False, encoding="ascii") as f:
        f.write(dimacs_text(num_vars, clauses))
        path = f.name
    try:
        proc = subprocess.run(["dotnet", dll, "solve", path], capture_output=True, text=True)
        sat = proc.returncode == 10 or proc.stdout.strip().startswith("SAT")
        true_vars = set()
        if sat:
            for line in proc.stdout.splitlines():
                if line.startswith("v "):
                    for tok in line[2:].split():
                        lit = int(tok)
                        if lit > 0:
                            true_vars.add(lit)
        m = _CPU_RE.search(proc.stderr)
        solve_t = float(m.group(1).replace(",", ".")) / 1000.0 if m else 0.0
        return sat, true_vars, solve_t
    finally:
        try:
            os.unlink(path)
        except OSError:
            pass


def solve(enc, limit=None):
    # opakovane resim a kazde nalezene prirazeni zakazu blokujici klauzuli, dokud reseni nedojdou (nebo nenarazim na limit). vrati (reseni, cas)
    clauses = [list(c) for c in enc.cnf.clauses]
    sols = []
    total_t = 0.0
    while True:
        sat, true_vars, t = run_mysolver(enc.cnf.num_vars, clauses)
        total_t += t
        if not sat:
            break
        sols.append(decode_assignment(enc, true_vars))
        if limit is not None and len(sols) >= limit:
            break
        clauses.append(blocking_clause(enc, sols[-1]))  # zakaz a hledej dalsi
    return sols, total_t


# --- CLI ---

# cislice -> znak (pro baze > 10, napr. 15 -> 'f')
_DIGITS = "0123456789abcdefghijklmnopqrstuvwxyz"

def _cli():
    p = argparse.ArgumentParser(description="Reseni kryptaritmu pres muj SAT solver")
    p.add_argument("formula", nargs="?", help="formule, napr. \"SEND + MORE = MONEY\"")
    p.add_argument("-f", "--file", help="cti formuli ze souboru")
    p.add_argument("--base", type=int, default=10, help="ciselna baze (vychozi 10)")
    p.add_argument("--allow-dup", action="store_true",
                   help="povol dvema pismenum stejnou cislici (vypne all-different)")
    p.add_argument("--count", action="store_true", help="spocitej vsechna reseni")
    p.add_argument("--unique", action="store_true", help="je reseni jednoznacne?")
    p.add_argument("--dimacs", help="jen zapis CNF do souboru a skonci")
    args = p.parse_args()

    if args.file:
        text = open(args.file, encoding="utf-8").read()
    else:
        text = args.formula or sys.stdin.read()
    node = parse_formula(text)
    enc = encode(node, base=args.base, all_different=not args.allow_dup)

    if args.dimacs:
        with open(args.dimacs, "w", encoding="ascii") as f:
            f.write(dimacs_text(enc.cnf.num_vars, enc.cnf.clauses))
        print(f"zapsano {args.dimacs}: {enc.cnf.num_vars} promennych, {len(enc.cnf.clauses)} klauzuli")
        return 0

    print(f"CNF: {enc.cnf.num_vars} promennych, {len(enc.cnf.clauses)} klauzuli", file=sys.stderr)

    # unique potrebuje dve reseni, count vsechna, jinak staci jedno
    limit = 2 if args.unique else (None if args.count else 1)
    sols, solve_time = solve(enc, limit)

    if not sols:
        print("UNSAT (zadne reseni)")
        return 20

    print(f"SAT   (cas {solve_time*1000:.1f} ms)")
    print(", ".join(f"{L}={_DIGITS[d]}" for L, d in sorted(sols[0].items())))

    if args.unique:
        print("reseni je JEDNOZNACNE" if len(sols) < 2 else "reseni NENI jednoznacne (existuji aspon dve)")
    elif args.count:
        print(f"pocet reseni: {len(sols)}")

    return 10


if __name__ == "__main__":
    sys.exit(_cli())
