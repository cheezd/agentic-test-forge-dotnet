# Changelog

## 1.0.2

`forge check` leaves Stryker's `Ignored` mutants out of the mutation score. `--since` marks a mutant ignored when that line is outside the diff. Those mutants no longer count as inconclusive and no longer lower the kill rate. A file whose mutants are all ignored has no mutation finding. `NoCoverage`, `Timeout`, and `Survived` still fail the gate.

## 1.0.1

`forge check` selects the mutation project from the changed files when the diff is non-empty. A test project that references more than one `forge.json` path no longer exits 2 with `the test project references more than one project.` before Stryker runs, when the changed product files belong to one referenced project. A changed test file does not count as a second project. An empty diff still passes without selecting a project. A diff that spans two referenced projects still exits 2 with that same message.
