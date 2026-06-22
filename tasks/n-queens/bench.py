# Vytvoreno s pomoci AI

from __future__ import annotations
import argparse
import json
import os
import re
import subprocess
import sys
import time

import nqueens

SCRATCH = os.environ.get("NQ_SCRATCH", os.path.join(os.path.dirname(os.path.abspath(__file__)), "_cnf"))
DLL = os.environ.get(
    "NQ_DLL",
    r"C:\DEV\SAT\solver\src\SatSolver\bin\Release\net10.0\SatSolver.dll",
)

MAX_PAIRWISE_CLAUSES = 9_000_000


# ----------------------------------------------------------------------
#  WORKER - jedna uloha v samostatnem procesu
# ----------------------------------------------------------------------

def run_pysat(n: int, encoding: str, backend: str, budget: float) -> dict:
    from pysat.solvers import Solver
    from threading import Timer

    t0 = time.perf_counter()
    b = nqueens.build_cnf(n, encoding)
    t_encode = time.perf_counter() - t0
    res = {"vars": b.num_vars, "clauses": len(b.clauses), "t_encode": t_encode}
    if t_encode > budget:
        res.update(status="TIMEOUT", note="encode over budget", t_solve=0.0, t_total=t_encode)
        return res

    solver = Solver(name=backend, bootstrap_with=b.clauses, use_timer=True)
    timed_out = {"v": False}

    def interrupt(s):
        timed_out["v"] = True
        try:
            s.interrupt()
        except Exception:
            pass

    timer = Timer(max(0.01, budget - t_encode), interrupt, [solver])
    t1 = time.perf_counter()
    timer.start()
    try:
        sat = solver.solve_limited(expect_interrupt=True)
    except Exception:
        sat = solver.solve()  # backend bez interruptu - jede bez limitu
    timer.cancel()
    t_solve = time.perf_counter() - t1
    res["t_solve"] = t_solve
    res["t_total"] = t_encode + t_solve

    if sat is None or timed_out["v"]:
        res.update(status="TIMEOUT", note="solve interrupted")
        solver.delete()
        return res
    if not sat:
        res.update(status="UNSAT")
        solver.delete()
        return res
    model = solver.get_model()
    solver.delete()
    true_vars = {v for v in model if v > 0}
    queens = nqueens.decode_model(n, true_vars)
    ok, msg = nqueens.verify(n, queens)
    res.update(status="SAT", valid=ok, note=("" if ok else msg))
    return res


def run_z3(n: int, budget: float) -> dict:
    import z3
    t0 = time.perf_counter()
    s, q = nqueens.build_z3_model(n)
    s.set("timeout", int(budget * 1000))
    t_encode = time.perf_counter() - t0
    res = {"vars": n, "clauses": 0, "t_encode": t_encode}
    if t_encode > budget:
        res.update(status="TIMEOUT", note="encode over budget", t_solve=0.0, t_total=t_encode)
        return res
    t1 = time.perf_counter()
    chk = s.check()
    t_solve = time.perf_counter() - t1
    res["t_solve"] = t_solve
    res["t_total"] = t_encode + t_solve
    if chk == z3.unknown:
        res.update(status="TIMEOUT", note="z3 unknown/timeout")
        return res
    if chk == z3.unsat:
        res.update(status="UNSAT")
        return res
    m = s.model()
    queens = [m[q[i]].as_long() for i in range(n)]
    ok, msg = nqueens.verify(n, queens)
    res.update(status="SAT", valid=ok, note=("" if ok else msg))
    return res


def run_mysolver(n: int, encoding: str, budget: float) -> dict:
    os.makedirs(SCRATCH, exist_ok=True)
    t0 = time.perf_counter()
    b = nqueens.build_cnf(n, encoding)
    res = {"vars": b.num_vars, "clauses": len(b.clauses)}
    path = os.path.join(SCRATCH, f"q{n}_{encoding}.cnf")
    nqueens.write_dimacs(path, b)
    t_encode = time.perf_counter() - t0
    res["t_encode"] = t_encode
    if t_encode > budget:
        res.update(status="TIMEOUT", note="encode over budget", t_solve=0.0, t_total=t_encode)
        _safe_rm(path)
        return res

    remaining = budget - t_encode
    # dotnet ma vlastni --time-limit (ciste reseni); navic outer timeout jako pojistka
    tw = time.perf_counter()
    proc = subprocess.run(
        ["dotnet", DLL, "solve", path, "--time-limit", f"{remaining:.3f}"],
        capture_output=True, text=True, timeout=remaining + 20,
    )
    dotnet_wall = time.perf_counter() - tw
    _safe_rm(path)
    # self-report "cas (CPU) : 12,3 ms" -> sekundy (ciste reseni, bez parsovani DIMACS)
    m = re.search(r"cas \(CPU\)\s*:\s*([\d.,]+)\s*ms", proc.stderr)
    t_solve = (float(m.group(1).replace(",", ".")) / 1000.0) if m else float("nan")
    res["t_solve"] = t_solve
    # t_total = stavba+zapis DIMACS (python) + dotnet (nacteni DIMACS + reseni)
    res["t_total"] = t_encode + dotnet_wall

    out = proc.stdout
    if "UNSAT" in out:
        res.update(status="UNSAT")
        return res
    if "SAT" not in out or "UNKNOWN" in out:
        res.update(status="TIMEOUT", note="unknown/time-limit hit")
        return res
    vline = next((l for l in out.splitlines() if l.startswith("v ")), None)
    if vline is None:
        res.update(status="TIMEOUT", note="no model line")
        return res
    lits = [int(x) for x in vline[2:].split() if x != "0"]
    true_vars = {l for l in lits if l > 0}
    queens = nqueens.decode_model(n, true_vars)
    ok, msg = nqueens.verify(n, queens)
    res.update(status="SAT", valid=ok, note=("" if ok else msg))
    return res


def _safe_rm(path: str) -> None:
    try:
        os.remove(path)
    except OSError:
        pass


def worker_main(args) -> None:
    family = args.family
    n = args.n
    encoding = args.encoding
    budget = args.budget
    # pairwise pojistka proti OOM
    if encoding == "pairwise":
        approx = n * n * (n - 1)  # hruby horni odhad poctu klauzuli
        if approx > MAX_PAIRWISE_CLAUSES:
            print(json.dumps({"status": "SKIP", "note": "pairwise too big", "n": n}))
            return
    try:
        if family == "mysolver":
            res = run_mysolver(n, encoding, budget)
        elif family == "z3":
            res = run_z3(n, budget)
        else:
            res = run_pysat(n, encoding, args.backend, budget)
    except subprocess.TimeoutExpired:
        res = {"status": "TIMEOUT", "note": "outer subprocess timeout"}
    except MemoryError:
        res = {"status": "SKIP", "note": "MemoryError"}
    except Exception as e:  # at driver dostane aspon neco
        res = {"status": "ERROR", "note": f"{type(e).__name__}: {e}"}
    res.setdefault("n", n)
    print(json.dumps(res))


# ----------------------------------------------------------------------
#  DRIVER - rizeni matice config x N
# ----------------------------------------------------------------------

LADDER = [4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20, 24, 28, 32,
          40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256,
          320, 384, 448, 512, 640, 768, 896, 1024, 1280, 1536, 2048]

# label, family, encoding, backend, max_n
CONFIGS = [
    ("mysolver-pairwise", "mysolver", "pairwise", "",           256),
    ("mysolver-seq",      "mysolver", "seq",      "",           512),
    ("minisat22",         "pysat",    "pairwise", "minisat22",  256),
    ("glucose4",          "pysat",    "pairwise", "glucose4",   256),
    ("cadical153",        "pysat",    "pairwise", "cadical153", 256),
    ("cadical153-seq",    "pysat",    "seq",      "cadical153", 1024),
    ("minisat22-seq",     "pysat",    "seq",      "minisat22",  1024),
    ("glucose4-seq",      "pysat",    "seq",      "glucose4",   1024),
    ("z3-smt",            "z3",       "intmodel", "",           2048),
]


def run_one(family, encoding, backend, n, budget, outer_extra=30) -> dict:
    cmd = [sys.executable, __file__, "worker", "--family", family,
           "--n", str(n), "--encoding", encoding, "--budget", str(budget)]
    if backend:
        cmd += ["--backend", backend]
    t0 = time.perf_counter()
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=budget + outer_extra)
        wall = time.perf_counter() - t0
        line = next((l for l in proc.stdout.splitlines() if l.strip().startswith("{")), None)
        if line is None:
            return {"status": "ERROR", "note": "no json", "wall": wall,
                    "stderr": proc.stderr[-300:]}
        res = json.loads(line)
        res["wall"] = wall
        return res
    except subprocess.TimeoutExpired:
        return {"status": "TIMEOUT", "note": "driver hard kill", "wall": time.perf_counter() - t0}


def driver_main(args) -> None:
    budget = args.budget
    configs = CONFIGS
    if args.only:
        wanted = set(args.only.split(","))
        configs = [c for c in CONFIGS if c[0] in wanted]
    csv_dir = os.path.dirname(os.path.abspath(args.csv))
    os.makedirs(csv_dir, exist_ok=True)

    header = "label,family,encoding,n,status,t_encode,t_solve,t_total,wall,vars,clauses,valid,note\n"
    with open(args.csv, "w", encoding="utf-8") as f:
        f.write(header)

    summary = []
    for (label, family, encoding, backend, max_n) in configs:
        print(f"\n=== {label} (rodina={family}, kodovani={encoding}) ===", flush=True)
        last_ok = None
        for n in LADDER:
            if n > max_n:
                break
            res = run_one(family, encoding, backend, n, budget)
            st = res.get("status", "?")
            t_total = res.get("t_total", res.get("wall"))
            t_solve = res.get("t_solve")
            valid = res.get("valid", "")
            note = res.get("note", "")
            print(f"  N={n:5d}  {st:8s}  t_solve={_fmt(t_solve)}  "
                  f"t_total={_fmt(t_total)}  wall={_fmt(res.get('wall'))}  {note}", flush=True)
            row = [label, family, encoding, n, st,
                   _csv(res.get("t_encode")), _csv(t_solve), _csv(t_total),
                   _csv(res.get("wall")), res.get("vars", ""), res.get("clauses", ""),
                   valid, note.replace(",", ";")]
            with open(args.csv, "a", encoding="utf-8") as f:
                f.write(",".join(map(str, row)) + "\n")

            within = (st == "SAT" and t_total is not None and t_total == t_total and t_total <= budget)
            if within:
                last_ok = n
            else:
                if st in ("TIMEOUT", "SKIP", "ERROR") or (t_total and t_total > budget):
                    print(f"  -> stop ({st}); max N do {budget:.0f}s = {last_ok}", flush=True)
                    break
        else:
            print(f"  -> dosel zebrik; max N do {budget:.0f}s = {last_ok} (mozna jeste vic)", flush=True)
        summary.append((label, last_ok))

    print("\n=== shrnuti: max N do {:.0f}s ===".format(budget), flush=True)
    for label, n in summary:
        print(f"  {label:20s} {n}", flush=True)
    print(f"\nhotovo, vysledky v {args.csv}", flush=True)


def _fmt(x):
    if x is None:
        return "  -  "
    try:
        if x != x:
            return " nan "
        return f"{x:7.3f}s"
    except (TypeError, ValueError):
        return str(x)


def _csv(x):
    if x is None:
        return ""
    try:
        if x != x:
            return ""
        return f"{x:.4f}"
    except (TypeError, ValueError):
        return str(x)


# ----------------------------------------------------------------------

def main() -> None:
    p = argparse.ArgumentParser(description="benchmark N dam pres ruzne solvery")
    sub = p.add_subparsers(dest="cmd", required=True)

    w = sub.add_parser("worker")
    w.add_argument("--family", required=True, choices=["mysolver", "pysat", "z3"])
    w.add_argument("--n", type=int, required=True)
    w.add_argument("--encoding", default="pairwise")  # pairwise / seq / intmodel
    w.add_argument("--backend", default="")           # jmeno pysat backendu
    w.add_argument("--budget", type=float, default=60.0)

    d = sub.add_parser("run")
    d.add_argument("--budget", type=float, default=60.0)
    d.add_argument("--csv", default="results/scaling.csv")
    d.add_argument("--only", default="", help="carkou oddelene labely configu")

    args = p.parse_args()
    if args.cmd == "worker":
        worker_main(args)
    else:
        driver_main(args)


if __name__ == "__main__":
    main()
