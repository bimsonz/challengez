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

## Commit 6: `perf: revert to sequential — benchmarks prove parallelism loses at every scale`

**Mode: Data-driven decision**

Generated a 10K-mapping dataset (1,000 accounts, 30 counterparties, 41 planted duplicate collisions) and benchmarked every version of the code against both the original 208-mapping dataset and the 10K dataset. Then did a cross-sectional analysis isolating each change to find the optimal combination.

### Benchmark Results

| Version | 208 mappings | 10K mappings |
|---|---|---|
| Commit 1: naive sequential loop | ~1ms | ~11ms |
| Commit 2: pre-computation + Parallel.For (threshold 1K) | ~1.4ms | ~15ms |
| Commit 4: PLINQ + pre-computation | ~5-17ms | ~28-50ms |
| **All fixes + plain sequential loop** | **~1.2ms** | **~13ms** |

### What the Data Shows

Parallelism loses at every scale we tested. The per-item work (two dictionary lookups + one `string.Concat`) is measured in nanoseconds — too cheap for any parallel dispatch overhead to recoup. PLINQ's `.AsOrdered()` constraint adds a merge step that further negates any benefit. Even `Parallel.For` with a threshold only added overhead at 10K.

The pre-computation itself is roughly neutral: at 10K mappings, building fragment dictionaries for 1,000 accounts costs ~2ms upfront, and the per-mapping savings (one concat vs full strip+slice+interpolation) barely offset that because .NET 10's `DefaultInterpolatedStringHandler` is already highly optimised for small strings.

### The Decision

Reverted Phase 2 to a plain sequential `for` loop. Kept the pre-computation and all other architectural improvements (extracted services, merged validation, IReadOnlyList, stderr separation, buffered output) because they are genuinely better code regardless of performance — the pre-computation eliminates a correctness risk (duplicate stripping logic), and the code reads more clearly.

The parallelism exploration was valuable: it proved through measurement that the naive approach was correct for this workload shape, and it demonstrated understanding of when parallelism helps (CPU-expensive per-item work) vs when it hurts (trivial per-item work where scheduling overhead dominates). Knowing when not to optimise is the point.

## Commit 7: `refactor: LINQ review — ToDictionary and merge phases`

**Mode: Targeted LINQ review**

Having discovered PLINQ during the parallelism exploration, I wanted to make sure we were using LINQ properly across the whole solution — not just for parallelism, but for expressiveness and idiomatic style.

Deep review found the codebase was mostly clean, with two improvements:

- **`ComputeCounterpartyPrefixes`** was reinventing `.ToDictionary()` — a 5-line foreach replaced with a single expression. Bonus: `.ToDictionary()` throws on duplicate keys rather than silently overwriting, which is better fail-fast behaviour.
- **Phases 2 and 3 in `AliasGeneratorService`** were separate sequential loops with an intermediate `baseAliases[]` array that was consumed immediately and never reused. Merged into a single `foreach` — eliminates the array allocation and reads more naturally.

Deliberately did NOT convert the merged loop to LINQ `.Select()` because `AliasResolver.Resolve` mutates `aliasCounts` — side effects inside `Select` is a LINQ anti-pattern that would silently break if someone later added `.AsParallel()`.

### Benchmark Impact

| Version | 208 mappings | 10K mappings |
|---|---|---|
| Commit 6: pre-computation + plain sequential | ~1.2ms | ~13ms |
| This: merged loop + ToDictionary | ~1.6ms | ~14ms |

Roughly a wash — the merge eliminates an array allocation but `.ToDictionary()` loses the capacity pre-hint. The value here is readability and correctness, not speed.

After the initial LINQ changes, I questioned whether the `ComputeAccountFragments` foreach loop — which we'd left alone because the body was "too complex for LINQ" — was actually a missed PLINQ candidate. The per-account work is independent and stateless, so parallelism is technically valid. But the same fundamental problem applies: per-account work is nanoseconds (two `.Replace()` calls, a length check, a switch, one `string.Concat`). At 100–1000 accounts, total computation is 5–100μs. PLINQ's thread-pool overhead would dwarf it. There's also a practical issue: PLINQ wraps exceptions in `AggregateException`, which would break our test assertions for `InvalidOperationException`.

What we did instead: extracted the per-account logic into a `ComputeAccountFragment` method and used plain `.ToDictionary()` — consistent with how `ComputeCounterpartyPrefixes` is written. The extracted method is documented as a pure function, safe to call in parallel if the workload ever justified it. Both dictionary-building methods now use the same pattern.
