# Nozzle mismatch diagnostics

New nozzle mismatch alarms store `NozzleMismatchDiagnostic` in `SystemEventData`.
The diagnostic contains the exact counter and selected-sale values copied during
the checker's fresh verification read, along with the event/nozzle/sale IDs,
sale timestamp, start/end totalizers, sale volume, invalid-sale flag, reference
totalizer/date, conversion multiplier, differences, and verification time.
The persistence coordinator's state is sampled after verification; it is an
observation, not a lock against subsequent sales. An absent state does not prove
that earlier sales were committed successfully.

The same text is written through the existing file logger, using the configured
`LogFile` (default `ASFuelControl.log`), when `LogLevel` is nonzero. Search for
`Nozzle total mismatch`. File entries say `commit pending` because they are
written before the existing database save. Correlate them using `EventId`.
The database diagnostic is saved with the alarm even when file logging is disabled.

Read the latest stored diagnostics with:

```sql
SELECT TOP (50) e.EventId, e.EventDate, e.NozzleId, d.Value AS Diagnostic
FROM SystemEvent AS e
INNER JOIN SystemEventData AS d ON d.SystemEventId = e.EventId
WHERE d.PropertyName = N'NozzleMismatchDiagnostic'
ORDER BY e.EventDate DESC;
```

For newly created alarms, `LastVolumeCounter` now stores the selected sale's end
totalizer used by the comparison, so `TotalVolumeCounter - LastVolumeCounter`
matches `TotalDifference` before display rounding. The former reference totalizer
is retained in the diagnostic. Existing alarm records are not rewritten.

This change collects evidence and corrects the recorded comparison baseline.
It does not establish or fix the cause of persistent/cumulative discrepancies.
Latest-sale ordering, mismatch tolerance, confirmation delay and persistence
deferral remain unchanged. No database schema migration is required.
