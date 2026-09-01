namespace DesktopScroll.Tests;

public sealed class LabelGeneratorTests
{
    [Fact]
    public void GenerateLabels_UsesUniqueTwoCharacterLabelsWithinCapacity()
    {
        var labels = LabelGenerator.GenerateLabels(676);

        Assert.Equal(676, labels.Length);
        Assert.All(labels, label => Assert.Equal(2, label.Length));
        Assert.Equal(676, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("aa", labels[0]);
        Assert.Equal("zz", labels[^1]);
    }

    [Fact]
    public void GenerateLabels_UsesThreeCharactersWhenTwoAreInsufficient()
    {
        var labels = LabelGenerator.GenerateLabels(677);

        Assert.Equal(677, labels.Length);
        Assert.All(labels, label => Assert.Equal(3, label.Length));
        Assert.Equal("aaa", labels[0]);
        Assert.Equal("baa", labels[676]);
    }

    [Fact]
    public void GenerateLabels_RejectsCellCountPastMaximumCapacity()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LabelGenerator.GenerateLabels(17_577));

        Assert.Contains("max supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateLabels_ReturnsNoLabelsForNonPositiveCellCount(int cellCount)
    {
        Assert.Empty(LabelGenerator.GenerateLabels(cellCount));
    }
}