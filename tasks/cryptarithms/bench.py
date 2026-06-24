# Vytvoreno s pomoci AI

from __future__ import annotations

import argparse
import csv
import os
import re
import subprocess
import sys
import time
from dataclasses import dataclass
from typing import List, Optional

HERE = os.path.dirname(os.path.abspath(__file__))
CRYPT = os.path.join(HERE, "cryptarithms.py")


@dataclass
class Job:
    label: str
    inst: str            # jmeno souboru v instances/ (bez pripony) nebo "inline:<formule>"
    mode: str            # solve | unique | count
    base: int = 10
    allow_dup: bool = False


# Matice uloh. mode rika, co merime: najit jedno reseni / dokazat jednoznacnost /
# spocitat vsechna reseni.
JOBS_SOLVE: List[Job] = [
    Job("SEND+MORE=MONEY",        "sendmore",       "unique"),
    Job("COCA+COLA=OASIS",        "coca_cola",      "unique"),
    Job("FORTY+TEN+TEN=SIXTY",    "forty",          "unique"),
    Job("DONALD+GERALD=ROBERT",   "donald_gerald",  "unique"),
    Job("AB+CD=EF & BA+DC=FE",    "formula_and",    "unique"),
    Job("ABC+DEF=GHIJ (base16)",  "base16",         "unique", base=16),
]

JOBS_COUNT: List[Job] = [
    Job("ODD+ODD=EVEN",           "inline:ODD + ODD = EVEN", "count"),         # 2
    Job("SEND+MORE=MONEY (dup)",  "sendmore",       "count", allow_dup=True),  # 155
    Job("CP+IS+FUN=TRUE",         "cpisfun",        "count"),                  # 72
]

# Resime VYHRADNE vlastnim solverem; pysat/z3 jsou jen volitelna krizova kontrola
# (sem je nedavame). Spravnost reseni overuje nezavisla aritmetika v cryptarithms.py.
BACKENDS = ["mysolver"]

_SOLVE_RE = re.compile(r"cas ([\d.]+) ms")
_COUNT_RE = re.compile(r"pocet reseni: (\d+)")


@dataclass
class Result:
    label: str
    backend: str
    mode: str
    status: str          # SAT / UNSAT / TIMEOUT / ERR
    detail: str          # jednoznacnost / pocet reseni
    wall_s: float        # cely proces (vc. startu dotnet/pythonu)
    solver_ms: float     # cas hlaseny resicem (jen u jednoho volani)


def run_job(job: Job, backend: str, budget: float) -> Result:
    cmd = [sys.executable, CRYPT, "--base", str(job.base)]
    if job.inst.startswith("inline:"):
        cmd.append(job.inst[len("inline:"):])
    else:
        cmd += ["-f", os.path.join(HERE, "instances", job.inst + ".txt")]
    if job.allow_dup:
        cmd.append("--allow-dup")
    if job.mode == "unique":
        cmd.append("--unique")
    elif job.mode == "count":
        cmd.append("--count")

    t0 = time.perf_counter()
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=budget)
    except subprocess.TimeoutExpired:
        return Result(job.label, backend, job.mode, "TIMEOUT", f">{budget:.0f}s",
                      budget, float("nan"))
    wall = time.perf_counter() - t0
    out = proc.stdout

    if out.startswith("UNSAT"):
        status = "UNSAT"
    elif out.startswith("SAT"):
        status = "SAT"
    else:
        return Result(job.label, backend, job.mode, "ERR",
                      (proc.stderr.strip()[-80:] or "?"), wall, float("nan"))

    detail = ""
    up = out.upper()
    if "JEDNOZNACNE" in up:
        detail = "vic" if "NENI" in up else "unik."
    mc = _COUNT_RE.search(out)
    if mc:
        detail = mc.group(1)
    ms = _SOLVE_RE.search(out)
    solver_ms = float(ms.group(1)) if ms else float("nan")
    return Result(job.label, backend, job.mode, status, detail, wall, solver_ms)


def cmd_run(args) -> None:
    jobs = JOBS_SOLVE + JOBS_COUNT
    rows: List[Result] = []
    for job in jobs:
        for backend in BACKENDS:
            r = run_job(job, backend, args.budget)
            rows.append(r)
            print(f"  {r.label:30s} {r.backend:9s} {r.mode:7s} "
                  f"{r.status:8s} {r.detail:6s} wall={r.wall_s:7.2f}s "
                  f"solver={r.solver_ms:9.1f}ms", file=sys.stderr)

    os.makedirs(os.path.dirname(args.csv), exist_ok=True)
    with open(args.csv, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["label", "backend", "mode", "status", "detail", "wall_s", "solver_ms"])
        for r in rows:
            w.writerow([r.label, r.backend, r.mode, r.status, r.detail,
                        f"{r.wall_s:.3f}", f"{r.solver_ms:.1f}"])
    print(f"zapsano {args.csv} ({len(rows)} radku)", file=sys.stderr)
    print_tables(rows)


def _fmt(r: Optional[Result]) -> str:
    if r is None:
        return "-"
    if r.status == "TIMEOUT":
        return f"TIMEOUT (>{r.wall_s:.0f}s)"
    if r.status == "ERR":
        return "CHYBA"
    return f"{r.wall_s:.2f} s"


def print_tables(rows: List[Result]) -> None:
    def table(jobs: List[Job], title: str, value_col: str) -> None:
        print(f"\n### {title}\n")
        head = "| instance | " + " | ".join(BACKENDS) + f" | {value_col} |"
        sep = "|" + "---|" * (len(BACKENDS) + 2)
        print(head)
        print(sep)
        for job in jobs:
            cells = []
            detail = ""
            for b in BACKENDS:
                r = next((x for x in rows if x.label == job.label and x.backend == b), None)
                cells.append(_fmt(r))
                if r and r.detail and r.status not in ("TIMEOUT", "ERR"):
                    detail = r.detail
            print(f"| {job.label} | " + " | ".join(cells) + f" | {detail} |")

    table(JOBS_SOLVE, "Najit reseni a dokazat jednoznacnost (wall-clock)", "vysledek")
    table(JOBS_COUNT, "Spocitat vsechna reseni (wall-clock)", "pocet")


def cmd_table(args) -> None:
    rows: List[Result] = []
    with open(args.csv, newline="", encoding="utf-8") as f:
        for d in csv.DictReader(f):
            rows.append(Result(d["label"], d["backend"], d["mode"], d["status"],
                               d["detail"], float(d["wall_s"]), float(d["solver_ms"])))
    print_tables(rows)


def main() -> None:
    p = argparse.ArgumentParser(description="mereni casu reseni kryptaritmu")
    sub = p.add_subparsers(dest="cmd", required=True)
    pr = sub.add_parser("run", help="spust cely sweep")
    pr.add_argument("--budget", type=float, default=60.0, help="limit na ulohu (s)")
    pr.add_argument("--csv", default=os.path.join(HERE, "results", "timings.csv"))
    pr.set_defaults(func=cmd_run)
    pt = sub.add_parser("table", help="jen vypis tabulky z CSV")
    pt.add_argument("csv")
    pt.set_defaults(func=cmd_table)
    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
