# agentic-test-forge-dotnet

C# quality gate for agentic development (CRAP, mutation, Gherkin). Same `forge check` contract as agentic-test-forge.

## Local tool

The command name is `forge`. Install it from a local tool manifest in the consumer repo.

```bash
dotnet new tool-manifest
dotnet tool install AgenticTestForge --local --add-source <directory-containing-the-nupkg>
dotnet tool run forge
```

## forge.json

Put this file at the consumer repo root. Omitted thresholds default to 30, 80, and 80. A missing `test_project` is exit 2. `acceptance_project` is optional.

```json
{
  "paths": ["src/App"],
  "test_project": "tests/App.Tests/App.Tests.csproj",
  "acceptance_project": null,
  "crap_threshold": 30,
  "mutation_threshold": 80,
  "gherkin_threshold": 80
}
```

## Usage

```bash
dotnet tool run forge -- check --base main --json report.json
```

`forge check` scores the git diff against `--base`. `--json` writes the report, and a one-line status still prints. `forge --help` lists the flags.

Exit 0 when every hard gate that ran passed. Exit 1 when CRAP is over the ceiling or mutation is under the floor. Exit 2 when the test project cannot be resolved, coverage cannot be produced, or a required engine is missing. A tool error outranks a gate failure. The JSON `status` is `pass`, `fail`, or `error` to match that exit code.

The test project must reference `coverlet.collector`. This tool installs Crap4DotNet 0.1.1 and Stryker.NET 5.0.0 on first use.

`--threshold` applies to `crap`, `mutate`, and `mutate-gherkin`. `forge check` uses `crap_threshold` and `mutation_threshold` from `forge.json`. A CRAP score equal to the ceiling passes. A kill rate equal to the floor passes.

With no `acceptance_project`, `forge check` records Gherkin as skipped. With one set, `mutate-gherkin` mutates Reqnroll Examples cells and fails when the kill rate is under the floor or a step binding is missing. `forge dry` stays advisory and does not change the exit code. `dry_sources` chooses the duplication sources (default `["sonar", "jaccard"]`). Each source is replaceable. Sonar runs when `sonar-scanner` is on `PATH` and `SONAR_HOST_URL`, `SONAR_TOKEN`, and `SONAR_PROJECT_KEY` are set. A missing scanner skips that source. Jaccard fingerprints normalized Roslyn methods and reports pairs at or above similarity 0.82. When no selected source runs, DRY is skipped. Sonar adds the duplications condition and issue keys to the JSON. Those keys are what a later step can send to `sonar remediate`. Sonar counts a duplicate at about 100 successive tokens over 10 lines.
