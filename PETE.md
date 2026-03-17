# Commit Walkthrough

This repo contains three distinct phases of work on the alias generator challenge, each demonstrating a different working mode.

## Commit 1: `feat: implement alias generator CLI tool`

**Mode: AI implementation with human direction**

I gave Claude the challenge brief and made the key architectural decisions upfront: light service layer with an interface (not full DI, not bare static methods), xUnit tests with thorough coverage, CLI args as the README suggests, and throw on invalid account names rather than silently producing bad data. Claude then implemented end-to-end from those decisions — the spec interpretation, algorithm, test cases, and code were its one-shot pass. It works, passes 21 tests, handles the planted duplicate trap in the data — but within the guardrails I set.

## Commit 2: `refactor: three-phase pipeline with pre-computation and parallel support`

**Mode: AI-directed algorithmic review**

I asked Claude to review its own work through the lens of an algorithm expert — concurrency, resource efficiency, processing order. It found a genuine bug (redundant `.ToList()` copying the entire result set), identified ~630 redundant string operations from repeated per-mapping work, and restructured into a three-phase pipeline with adaptive parallelism. The parallel path uses `Parallel.For` above a threshold where it actually helps, and sequential below where thread-pool overhead dominates. This is what you get when AI reviews its own output with specific direction.

## Commit 3: `refactor: manual review — extract responsibilities and tighten CLI`

**Mode: Human review with AI execution**

My manual pass through the code. This is where experience and taste come in — things AI doesn't naturally prioritise:

- Removed the hardcoded `data.json` fallback — no magic defaults in a CLI tool
- Replaced `Console.Error.WriteLine` + exit codes with proper exceptions (`ArgumentException`, `FileNotFoundException`)
- Enforced consistent brace style throughout
- Used C# list pattern (`args is not [var path, ..]`) instead of length checks
- Replaced null-coalescing throw with explicit null check
- Extracted `DataValidator`, `FragmentPreComputer`, and `AliasResolver` into their own files — the service was doing too much
- Switched output loop from per-line `Console.WriteLine` with string interpolation to buffered `StreamWriter` with direct writes

The difference between commits 2 and 3 is the difference between "technically optimal" and "code I'd want to maintain." AI gets the algorithm right but doesn't have opinions about code organisation, brace style, or what feels wrong when you read it. That's the human layer.

## Commit 4: `refactor: replace hardcoded parallel threshold with PLINQ`

**Mode: Human-initiated research with AI execution**

During my review I noticed the hardcoded `ParallelThreshold = 1_000` constant that switched between `Parallel.For` and a sequential loop. It didn't sit right — it felt like a magic number, not portable across machines, and duplicated the loop body in two code paths. I asked Claude to research whether .NET has an idiomatic solution for this. Turns out PLINQ is exactly that: its runtime heuristics analyse query shape and decide whether to parallelise automatically, falling back to sequential for small datasets without any threshold. Replaced the entire `BuildBaseAliases` method and its magic constant with a single PLINQ expression. The service went from ~90 lines to ~50.

This is the kind of thing where knowing something feels wrong is the human skill, but digging into the ecosystem to find the right answer is where AI research shines.

## Commit 5: `fix: final idiomatic review — stderr, immutability, validation`

**Mode: AI review with human judgment on trade-offs**

Ran a full idiomatic .NET review for performance, security, and correctness. The review flagged several genuine issues:

- **Diagnostics on stdout corrupted piped output** — `Console.WriteLine` for timing and "Loaded..." messages mixed with CSV data. Moved all diagnostics to `Console.Error` so `> results.csv` captures only data. Standard Unix convention.
- **Duplicate name stripping** — `DataValidator` stripped account names for validation, then `FragmentPreComputer` stripped them again for fragment computation. If someone changed the logic in one place but not the other, validation and generation would silently diverge. Merged validation into fragment computation — stripping now happens exactly once per account.
- **`List<T>` in `DataStore` record** — records imply immutability but `List<T>` is mutable. Changed to `IReadOnlyList<T>` across the model and all consuming methods.
- **No `AccountNumber` format validation** — the `AsSpan` slice would throw with no domain context if an account number was too short. Added validation during fragment computation.
- **Residual `.ToList()` in `SolverTests`** — leftover from before we changed the return type.

The review also flagged PLINQ as overhead at 208 items (~5-17ms vs ~1ms sequential). This is a deliberate trade-off: at this dataset size, a plain loop is faster. But the PLINQ approach is architecturally correct for production — the runtime owns the parallelism decision rather than a hardcoded magic number. For a tech test, demonstrating that you understand the trade-off and can articulate why you made the choice matters more than shaving milliseconds off a sub-second operation.
