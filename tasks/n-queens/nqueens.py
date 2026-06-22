
class CnfBuilder:
    # skladam CNF po klauzulich, drzim si pocet prom. a klauzule
    def __init__(self, n_base_vars):
        self.num_vars = n_base_vars
        self.clauses = []

    def new_var(self):
        self.num_vars += 1
        return self.num_vars

    def add(self, clause):
        self.clauses.append(list(clause))

    def at_most_one_pairwise(self, lits):
        # zadna dvojice nesmi byt obe pravda 
        for i in range(len(lits)):
            for j in range(i + 1, len(lits)):
                self.add([-lits[i], -lits[j]])

    def at_most_one_seq(self, lits):
        # sekvencni kodovani pro at-most-one
        k = len(lits)
        if k <= 1:
            return
        if k == 2:                       # pro dve staci jedna klauzule
            self.add([-lits[0], -lits[1]])
            return
        s = [self.new_var() for _ in range(k - 1)]
        self.add([-lits[0], s[0]])
        self.add([-lits[k - 1], -s[k - 2]])
        for i in range(1, k - 1):
            self.add([-lits[i], s[i]])
            self.add([-s[i - 1], s[i]])
            self.add([-lits[i], -s[i - 1]])


def var(n, r, c):
    # cislo promenne pro pole (r,c). +1 protoze DIMACS cisluje od 1.
    return r * n + c + 1


def build_cnf(n, amo="pairwise"):
    # postavi CNF pro N dam.
    b = CnfBuilder(n * n)
    if amo == "pairwise":
        at_most_one = b.at_most_one_pairwise
    elif amo in ("seq", "seqcounter"):
        at_most_one = b.at_most_one_seq
    else:
        raise ValueError(f"nezname at-most-one kodovani: {amo}")

    # v kazdem radku aspon jedna dama
    for r in range(n):
        b.add([var(n, r, c) for c in range(n)])

    # v kazdem sloupci nejvyse jedna
    for c in range(n):
        at_most_one([var(n, r, c) for r in range(n)])

    # diagonaly si poskladam podle klice
    diag1 = {}
    diag2 = {}
    for r in range(n):
        for c in range(n):
            diag1.setdefault(r - c, []).append(var(n, r, c))
            diag2.setdefault(r + c, []).append(var(n, r, c))
    for cells in diag1.values():
        at_most_one(cells)
    for cells in diag2.values():
        at_most_one(cells)

    return b


def write_dimacs(path, b):
    # ulozi CNF do DIMACS souboru por dotnet solver
    with open(path, "w", encoding="ascii") as f:
        f.write(f"p cnf {b.num_vars} {len(b.clauses)}\n")
        for cl in b.clauses:
            f.write(" ".join(map(str, cl)) + " 0\n")


def decode_model(n, true_vars):
    # z mnoziny pravdivych promennych vytahne sloupec damy v kazdem radku.
    queens = [-1] * n
    for r in range(n):
        for c in range(n):
            if var(n, r, c) in true_vars:
                queens[r] = c
                break
    return queens


def verify(n, queens):
    # zkontroluje, ze rozmisteni je opravdu platne reseni N dam
    if len(queens) != n:
        return False, f"ocekavam {n} radku, mam {len(queens)}"
    cols = set()
    diag1 = set()   # r - c
    diag2 = set()   # r + c
    for r in range(n):
        c = queens[r]
        if c < 0 or c >= n:
            return False, f"radek {r} nema platnou damu (c={c})"
        if c in cols:
            return False, f"sloupec {c} ma vic dam"
        if (r - c) in diag1:
            return False, f"diagonala '\\' (r-c={r - c}) ma vic dam"
        if (r + c) in diag2:
            return False, f"diagonala '/' (r+c={r + c}) ma vic dam"
        cols.add(c)
        diag1.add(r - c)
        diag2.add(r + c)
    return True, "ok"


def board_str(n, queens):
    # vykresli sachovnici
    lines = []
    for r in range(n):
        row = ["."] * n
        if 0 <= queens[r] < n:
            row[queens[r]] = "Q"
        lines.append(" ".join(row))
    return "\n".join(lines)


def build_z3_model(n):
    # SMT verze: q[i] = sloupec damy v radku i. radky jsou ruzne uz tim, ze mam jednu prom. na radek, zbytek resi Distinct
    import z3
    q = [z3.Int(f"q_{i}") for i in range(n)]
    s = z3.Solver()
    for i in range(n):
        s.add(q[i] >= 0, q[i] < n)
    s.add(z3.Distinct(q))                                # ruzne sloupce
    s.add(z3.Distinct([q[i] - i for i in range(n)]))     # ruzne \ diagonaly
    s.add(z3.Distinct([q[i] + i for i in range(n)]))     # ruzne / diagonaly
    return s, q


def _demo():
    import argparse
    parser = argparse.ArgumentParser(description="N dam demo")
    parser.add_argument("n", type=int, help="pocet dam a velikost sachovnice")
    parser.add_argument("--amo", default="pairwise", choices=["pairwise", "seq"])
    parser.add_argument("--dimacs", help="jen zapis DIMACS do souboru a skonci")
    parser.add_argument("--board", action="store_true", help="vypsat sachovnici")
    args = parser.parse_args()

    b = build_cnf(args.n, amo=args.amo)
    print(f"N={args.n}  amo={args.amo}  promennych={b.num_vars}  klauzuli={len(b.clauses)}")

    if args.dimacs:
        write_dimacs(args.dimacs, b)
        print(f"zapsano do {args.dimacs}")
        return

    from pysat.solvers import Glucose4
    with Glucose4(bootstrap_with=b.clauses) as solver:
        if not solver.solve():
            print("UNSAT")
            return
        model = solver.get_model()
        true_vars = {v for v in model if v > 0}
    queens = decode_model(args.n, true_vars)
    ok, msg = verify(args.n, queens)
    print("SAT" if ok else f"SAT ale neplatne: {msg}")
    print("queens (sloupec po radcich):", queens)
    if args.board:
        print(board_str(args.n, queens))


if __name__ == "__main__":
    _demo()
