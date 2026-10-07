using AgenticTestForge.Mutation;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class MemberLocatorTests
{
    private const string Source = """
        using System;

        namespace Sample;

        public sealed class Widget
        {
            public int Count;

            public Widget(int count) => Count = count;

            ~Widget() { }

            public int Value => Count;

            public int this[int index] => index;

            public event EventHandler Changed
            {
                add { }
                remove { }
            }

            public static Widget operator +(Widget left, Widget right) => new(left.Count + right.Count);

            public static implicit operator int(Widget widget) => widget.Count;

            public int Outer()
            {
                int Local() => 1;
                return Local();
            }
        }
        """;

    [Fact]
    public void NamesConstructorsPropertiesFieldsAndOtherMembers()
    {
        Assert.Equal("Sample.Widget.Widget", Find("public Widget(int count)"));
        Assert.Equal("Sample.Widget.Widget", Find("~Widget()"));
        Assert.Equal("Sample.Widget.Count", Find("public int Count;"));
        Assert.Equal("Sample.Widget.Value", Find("public int Value =>"));
        Assert.Equal("Sample.Widget.this", Find("public int this[int index]"));
        Assert.Equal("Sample.Widget.Changed", Find("public event EventHandler Changed"));
        Assert.Equal("Sample.Widget.+", Find("operator +(Widget left"));
        Assert.Equal("Sample.Widget.implicit", Find("implicit operator int"));
        Assert.Equal("Sample.Widget.Outer", Find("public int Outer()"));
        Assert.Equal("Sample.Widget.Local", Find("int Local()"));
    }

    private static string Find(string fragment)
    {
        var line = Source
            .Split('\n')
            .ToList()
            .FindIndex(text => text.Contains(fragment, StringComparison.Ordinal));
        Assert.True(line >= 0, fragment);
        return MemberLocator.Find(Source, line + 1);
    }
}
