using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class UniqueIdGeneratorTests
{
    [Theory]
    [InlineData(UniqueIdGenerator.Shop)]
    [InlineData(UniqueIdGenerator.Group)]
    [InlineData(UniqueIdGenerator.City)]
    [InlineData(UniqueIdGenerator.State)]
    [InlineData(UniqueIdGenerator.Country)]
    public void Generate_MakesAPrefixedReadableIdentifier(string prefix)
    {
        var id = UniqueIdGenerator.Generate(prefix);

        Assert.Matches($"^{prefix}-[2-9A-HJKMNP-TV-Z]{{4}}-[2-9A-HJKMNP-TV-Z]{{4}}$", id);
        Assert.True(UniqueIdGenerator.IsValid(id, prefix));
    }

    [Fact]
    public void Generate_NeverUsesCharactersThatAreEasyToMisread()
    {
        var all = string.Concat(Enumerable.Range(0, 500).Select(_ => UniqueIdGenerator.Generate("SHP")[4..]));

        Assert.DoesNotContain('0', all);
        Assert.DoesNotContain('O', all);
        Assert.DoesNotContain('1', all);
        Assert.DoesNotContain('I', all);
        Assert.DoesNotContain('L', all);
        Assert.DoesNotContain('U', all);
    }

    [Fact]
    public void Generate_DoesNotRepeatInAThousandTries()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => UniqueIdGenerator.Generate("SHP")).ToHashSet();

        Assert.Equal(1000, ids.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SHP-7KQ2-9M4")]      // too short
    [InlineData("SHP-7KQ2-9M4D1")]    // too long
    [InlineData("GRP-7KQ2-9M4D")]     // another level
    [InlineData("SHP-7KQ2_9M4D")]     // wrong separator
    [InlineData("SHP-7KQ0-9M4D")]     // a 0
    public void IsValid_RejectsWhatIsNotAShopIdentifier(string? value)
    {
        Assert.False(UniqueIdGenerator.IsValid(value, UniqueIdGenerator.Shop));
    }
}
