# Final review: integration test performance exploration

Reviewed on 2026-10-06. This was a read-only review of the comparison report, the testing policy, the coverage map, the variant generator, the measurement harness, and retained result artifacts. I did not run builds, tests, or benchmarks.

## Verdict

No material evidence, correctness, or reporting issue blocks the final recommendation. The evidence supports recommending A alone as the next adoption candidate: it reduces the IntegrationTests median from 96.245 s to 53.435 s (44.48%), with paired reductions from 41.12% to 47.16%. Its warm build-plus-full median is 41.74% lower, and all five paired cycles are faster than their matched baselines.

The report correctly treats A+B as a successful prototype without claiming that B adds value over A. Each option has its own baseline series, and no direct A-versus-A+B pairs were collected. B alone reduces IntegrationTests by 11.98%, below the approved goal. The report also keeps the recommendation separate from adoption authorization.

## Evidence checks

- Recomputed the published medians, median reductions, and paired reduction bounds for all 15 candidate/mode summaries from `timings.csv` and `paired-deltas.csv`; the values match `summary.csv`.
- The timing evidence has 150 paired observations, 30 regular warm-ups, six clean-output observations, and two supplemental resume warm-ups. There are five baseline/candidate pairs for each of the 15 candidate/mode combinations, and no accepted timing row has a nonzero build or test exit code.
- Compared all 188 per-sample inventories against the canonical inventories. All 8,204 identity observations match, with no missing, extra, or duplicate identities.
- Rehashed all 714 entries in the four frozen-tree manifests; every recorded file is present and its SHA-256 matches. The working repository has no tracked `src` or `tests` diff.
- The A and A+B warm build-plus-full paired savings are positive in every pair. The single clean-output cycles also favor each candidate. Reported outliers remain in the evidence, and the report does not assign them unprofiled causes.

## Specification and quality

The exploration follows `docs/agents/testing.md`: separate frozen trees, untimed warm-ups, five alternating sequential pairs, the required test modes, a separate clean-output comparison, external wall-time measurements, exact test inventories, build and test timings, paired summaries, and shared-library-output checks. The coverage map accounts for the six renamed A/A+B histories and the 29 unchanged IntegrationTests identities. The generator records a matching normalized history hash, and no diagnostic compilations were grouped.

The report states the material limits clearly: results are from one Windows x64 machine and SDK, no CI performance was measured, outlier causes were not profiled, and B's restore-graph invalidation is scoped to the frozen consumer workflow. These limits do not weaken the A-alone recommendation. The evidence and report are complete for the authorized exploration and recommendation phase; adoption, commits, and pull requests remain outside this review.
