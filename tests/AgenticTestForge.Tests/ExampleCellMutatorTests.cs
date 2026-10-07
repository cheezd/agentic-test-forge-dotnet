using AgenticTestForge.Gherkin;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ExampleCellMutatorTests
{
    [Fact]
    public void NumbersMoveByOneAndToZero() =>
        Assert.Equal(["3", "1", "0"], ExampleCellMutator.Mutate("2"));

    [Fact]
    public void StringsBecomeEmptyOrSuffixed() =>
        Assert.Equal(["", "hello_mutated"], ExampleCellMutator.Mutate("hello"));

    [Fact]
    public void BooleansFlipInAdditionToTheStringChanges() =>
        Assert.Equal(["", "true_mutated", "false"], ExampleCellMutator.Mutate("true"));
}
