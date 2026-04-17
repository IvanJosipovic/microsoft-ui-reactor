#!/bin/bash
# PerfBench Benchmark Suite
# Runs all 10 experiments × 3 variants (Direct, Bound, Reactor) headless.
# Each run: --headless --duration 10
# Collects report files and prints summary.

set -e

DURATION=10
BASE="C:/Users/andersonch/Code/patch/tests/perf_bench"
TF="net9.0-windows10.0.22621.0"
OUTFILE="${BASE}/perfbench_results.txt"

> "$OUTFILE"

run_app() {
    local exp="$1"
    local var="$2"
    local extra_args="${3:-}"
    local proj_dir="${BASE}/PerfBench.${exp}/${exp}.${var}"
    local exe_dir="${proj_dir}/bin/ARM64/Release/${TF}"
    local exe="${exe_dir}/${exp}.${var}.exe"

    if [ ! -f "$exe" ]; then
        echo "  SKIP (no exe): ${exp}.${var}" | tee -a "$OUTFILE"
        return
    fi

    # Delete old report files
    find "$exe_dir" -maxdepth 1 -iname "*.report.txt" -delete 2>/dev/null || true

    echo "  Running ${exp}.${var}..."
    "$exe" --headless --duration "$DURATION" $extra_args 2>/dev/null || true

    # Find report file
    local report
    report=$(find "$exe_dir" -maxdepth 1 -iname "*.report.txt" -type f 2>/dev/null | head -1)
    if [ -n "$report" ] && [ -f "$report" ]; then
        cat "$report" >> "$OUTFILE"
        echo "" >> "$OUTFILE"
    else
        echo "  NO REPORT: ${exp}.${var}" | tee -a "$OUTFILE"
    fi
}

echo "=== PerfBench Benchmark Suite ===" | tee -a "$OUTFILE"
echo "Duration per run: ${DURATION}s" | tee -a "$OUTFILE"
echo "" | tee -a "$OUTFILE"

EXPERIMENTS=(DirtyTracking PropertyDiff StructuralSharing OffThread Journal InteractivePool TimeSlice Priorities DeferredMount Allocation)

for exp in "${EXPERIMENTS[@]}"; do
    echo "--- EXP: $exp ---" | tee -a "$OUTFILE"
    for var in Direct Bound Reactor; do
        run_app "$exp" "$var"
    done
    echo "" | tee -a "$OUTFILE"
done

echo "=== Done ===" | tee -a "$OUTFILE"
echo "Results: $OUTFILE"
