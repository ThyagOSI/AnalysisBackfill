# AnalysisBackfill
Programmatically backfill and recalculate AF Analyses

This utility backfills/recalculates analyses.  Generic syntax:
        UpdateFileAttribute.exe \\\\AFServer\AFDatabase\pathToElement\AFElement AnalysisNameFilter StartTime EndTime Mode

This utility supports two modes: backfill and recalc.  Backfill will fill in data gaps only.  Recalc will replace all values.  Examples:
        UpdateFileAttribute.exe \\\\AF1\TestDB\Plant1\Pump1 FlowRate_*Avg '\*-10d' '\*' recalc
        UpdateFileAttribute.exe \\\\AF1\TestDB\Plant1\Pump1 *Rollup '\*-10d' '\*' backfill
