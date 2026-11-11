# Changelog

## 1.0.1

`forge check` selects the mutation project from the changed files when the diff is non-empty. A test project that references more than one `forge.json` path no longer exits 2 with `the test project references more than one project.` before Stryker runs, when the changed product files belong to one referenced project. A changed test file does not count as a second project. An empty diff still passes without selecting a project. A diff that spans two referenced projects still exits 2 with that same message.
