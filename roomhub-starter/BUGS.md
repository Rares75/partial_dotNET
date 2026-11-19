# BUGS

The starter contains behaviors that contradict `ASSIGNMENT.md`. Write here every discrepancy you found.
Fill it in yourself, in your own words. There is no required number: what counts is that they are real.

| Which rule from ASSIGNMENT.md is violated (R1–R8) | Where the problem was in the starter (file, approximate line) | How you discovered it (test, reading, AI) | What you changed |
|---|---|---|---|
|R5 - a booking cannot  start in the past | src/RoomHub.Api/Services/BookingService.cs, Create() method, around line 20 | reading the code | Inject TimeProvider into the booking creation path and reject a booking whose startUtc is before TimeProvider.GetUtcNow() after that returning 400. |

|R6 - maximum booking duration is 4hours and exactly 4 hours has to be allowed|BookingService.cs, in the Create() method around line 18|Reading| i added the check(endUtc -  startUtc)|

|R7 only two active booking conlficts and R8 a cancelled booking free its interval |BookingService.cs , line 24|AI|use a strict inequalities (existing.StartUTC < endUtc && startUtc < existing.EndUtc) |

|R8 cancelling an already-cancelled booking gives 409|BookingService.cs Cancel() method line62 | AI| returning 409 when the booking is already canceled|