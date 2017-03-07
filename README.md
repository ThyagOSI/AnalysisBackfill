# AnalysisBackfill
Programmatically backfill and recalculate AF Analyses for the OSIsoft PI System.  Supported only for PI AF 2.8.5+, PI Analysis Service 2.8.5+, and PI Data Archive 3.4.405+

This utility allows the user to backfill/recalculate analyses from the command line.  It allows the user to specify the element, a filter for the analysis name, the time range, and the mode.  This utility supports two modes: backfill and recalc.  Backfill will fill in data gaps only.  Recalc will delete all values in the time range and then calculate results.  

Example syntax:
	AnalysisBackfill.exe \\\\AF1\TestDB\Plant1\Pump1 FlowRate_\*Avg '\*-10d' '\*' recalc
	AnalysisBackfill.exe \\\\AF1\TestDB\Plant1 \*Rollup '\*-10d' '\*' backfill

Updated 2017.03.07
