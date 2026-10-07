using AgenticTestForge.Crap;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class CrapMemberScorerTests
{
    [Fact]
    public void FoldsAsyncAndIteratorStateMachinesOntoTheSourceMember()
    {
        var coverage = CoberturaCoverage.Parse(
            """
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class name="Sample.Widget" filename="src/Widget.cs">
                      <methods>
                        <method name="Score">
                          <lines>
                            <line number="6" hits="1" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                    <class name="Sample.Widget/&lt;Fetch&gt;d__1" filename="src/Widget.cs">
                      <methods>
                        <method name="MoveNext">
                          <lines>
                            <line number="14" hits="2" />
                            <line number="15" hits="0" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                    <class name="Sample.Widget/&lt;Range&gt;d__2" filename="src/Widget.cs">
                      <methods>
                        <method name="MoveNext">
                          <lines>
                            <line number="20" hits="1" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """
        );

        var findings = CrapMemberScorer.Score(
            [
                Method("Score", 3d),
                Method("Fetch", 3d),
                Method("Range", 2d),
                Method("MoveNext", 6d, className: "Widget/<Fetch>d__1"),
            ],
            coverage,
            ceiling: 30d,
            repoRoot: @"C:\repo",
            roots: ["src"],
            scopedFiles: null
        );

        var fetch = Assert.Single(
            findings,
            finding => finding.QualifiedName.Contains("Fetch", StringComparison.Ordinal)
        );
        Assert.Equal(0.5d, fetch.Coverage);
        Assert.Equal(4.125d, fetch.CrapScore);
        Assert.False(fetch.AboveThreshold);
        Assert.DoesNotContain(
            findings,
            finding => finding.QualifiedName.Contains("MoveNext", StringComparison.Ordinal)
        );

        var range = Assert.Single(
            findings,
            finding => finding.QualifiedName.Contains("Range", StringComparison.Ordinal)
        );
        Assert.Equal(1d, range.Coverage);
        Assert.False(range.AboveThreshold);
    }

    [Fact]
    public void MissingCoverageIsNotApplicableAndFailsTheCeiling()
    {
        var coverage = CoberturaCoverage.Parse("<coverage></coverage>");
        var findings = CrapMemberScorer.Score(
            [Method("Missing", 1d)],
            coverage,
            ceiling: 30d,
            repoRoot: @"C:\repo",
            roots: ["src"],
            scopedFiles: null
        );

        var finding = Assert.Single(findings);
        Assert.Null(finding.Coverage);
        Assert.Null(finding.CrapScore);
        Assert.True(finding.AboveThreshold);
    }

    [Fact]
    public void BaseLimitsScoringToTheDiffAndAnEmptyDiffPasses()
    {
        var coverage = CoberturaCoverage.Parse(
            """
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class name="Sample.Widget" filename="src/Widget.cs">
                      <methods>
                        <method name="Score">
                          <lines><line number="1" hits="1" /></lines>
                        </method>
                        <method name="Other">
                          <lines><line number="2" hits="0" /></lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """
        );
        var methods = new[]
        {
            Method("Score", 1d, filePath: @"C:\repo\src\Widget.cs"),
            Method(
                "Other",
                8d,
                filePath: @"C:\repo\src\Other.cs",
                fullName: "Sample.Widget.Other()"
            ),
        };

        var onDiff = CrapMemberScorer.Score(
            methods,
            coverage,
            30d,
            @"C:\repo",
            ["src"],
            ["src/Widget.cs"]
        );
        var only = Assert.Single(onDiff);
        Assert.Contains("Score", only.QualifiedName, StringComparison.Ordinal);

        var none = CrapMemberScorer.Score(methods, coverage, 30d, @"C:\repo", ["src"], []);
        Assert.Empty(none);
    }

    [Fact]
    public void RealZeroCoverageStillScoresAndAScoreOnTheCeilingPasses()
    {
        var coverage = CoberturaCoverage.Parse(
            """
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class name="Sample.Widget" filename="src/Widget.cs">
                      <methods>
                        <method name="Untested">
                          <lines>
                            <line number="1" hits="0" />
                            <line number="2" hits="0" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """
        );

        var finding = Assert.Single(
            CrapMemberScorer.Score(
                [Method("Untested", 5d)],
                coverage,
                30d,
                @"C:\repo",
                ["src"],
                null
            )
        );
        Assert.Equal(0d, finding.Coverage);
        Assert.Equal(30d, finding.CrapScore);
        Assert.False(finding.AboveThreshold);
    }

    [Fact]
    public void MatchesNestedTypesAndConstructorsFromCoberturaNames()
    {
        var coverage = CoberturaCoverage.Parse(
            """
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class name="Sample.Widget" filename="src/Widget.cs">
                      <methods>
                        <method name=".ctor">
                          <lines>
                            <line number="3" hits="1" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                    <class name="AgenticTestForge.Config.ForgeConfigLoader/Document" filename="src/Loader.cs">
                      <methods>
                        <method name="BlankToNull">
                          <lines>
                            <line number="10" hits="2" />
                            <line number="11" hits="0" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """
        );

        var findings = CrapMemberScorer.Score(
            [
                Method("Widget", 1d, fullName: "Sample.Widget.Widget()"),
                new CrapToolMethod(
                    "AgenticTestForge.Config",
                    "ForgeConfigLoader.Document",
                    "BlankToNull",
                    "AgenticTestForge.Config.ForgeConfigLoader.Document.BlankToNull(string?)",
                    @"C:\repo\src\Loader.cs",
                    2d
                ),
            ],
            coverage,
            30d,
            @"C:\repo",
            ["src"],
            null
        );

        var constructor = Assert.Single(
            findings,
            finding => finding.QualifiedName.Contains("Widget()", StringComparison.Ordinal)
        );
        Assert.Equal(1d, constructor.Coverage);
        Assert.False(constructor.AboveThreshold);

        var nested = Assert.Single(
            findings,
            finding => finding.QualifiedName.Contains("BlankToNull", StringComparison.Ordinal)
        );
        Assert.Equal(0.5d, nested.Coverage);
        Assert.False(nested.AboveThreshold);
    }

    [Fact]
    public void MatchesPropertyGettersFromCoberturaNames()
    {
        var coverage = CoberturaCoverage.Parse(
            """
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class name="AgenticTestForge.Reporting.GherkinGateReport" filename="src/GherkinGateReport.cs">
                      <methods>
                        <method name="get_Name">
                          <lines>
                            <line number="8" hits="1" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """
        );

        var finding = Assert.Single(
            CrapMemberScorer.Score(
                [
                    new CrapToolMethod(
                        "AgenticTestForge.Reporting",
                        "GherkinGateReport",
                        "Name.get",
                        "AgenticTestForge.Reporting.GherkinGateReport.Name.get",
                        @"C:\repo\src\GherkinGateReport.cs",
                        1d
                    ),
                ],
                coverage,
                30d,
                @"C:\repo",
                ["src"],
                null
            )
        );
        Assert.Equal(1d, finding.Coverage);
        Assert.Equal(1d, finding.CrapScore);
        Assert.False(finding.AboveThreshold);
    }

    private static CrapToolMethod Method(
        string name,
        double complexity,
        string className = "Widget",
        string filePath = @"C:\repo\src\Widget.cs",
        string? fullName = null
    ) =>
        new("Sample", className, name, fullName ?? $"Sample.Widget.{name}()", filePath, complexity);
}
