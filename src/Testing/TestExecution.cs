// The seat a cast and its delivery roll on is a process-wide static (CombatRandom.Source,
// CastRandom.Source), and CombatRandomScope/CastRandomScope install a test's own generator into it for
// the length of one test. Class-level parallelism therefore lets one class hand another class's fight a
// counting spy or a fixed draw: the victim rolls nothing, its poison ticks for zero and its chances never
// land. The failure moves with the schedule, so it surfaces as unrelated tests breaking when any test is
// added anywhere. Until the roll seat is scoped rather than static, the suite runs one class at a time.
[assembly: DoNotParallelize]
