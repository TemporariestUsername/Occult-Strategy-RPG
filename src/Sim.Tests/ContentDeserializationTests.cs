using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Serialization;

namespace PaleCommunion.Sim.Tests;

public class ContentDeserializationTests
{
    [Fact]
    public void Effect_SnakeCaseFields_MapToProperties()
    {
        var e = JsonSerializer.Deserialize<Effect>(
            "{\"type\":\"gain_secret\",\"id\":\"magistrate_second_name\",\"about_scope\":\"actor\"}",
            SimJson.Content)!;

        Assert.Equal("gain_secret", e.Type);
        Assert.Equal("magistrate_second_name", e.Id);
        Assert.Equal("actor", e.AboutScope);
    }

    [Fact]
    public void Condition_Group_DeserializesRecursively()
    {
        string json = "{\"type\":\"all_of\",\"conditions\":[" +
            "{\"type\":\"member_count\",\"op\":\"gte\",\"value\":2}," +
            "{\"type\":\"veil\",\"op\":\"gte\",\"value\":20}]}";

        var c = JsonSerializer.Deserialize<Condition>(json, SimJson.Content)!;

        Assert.True(c.IsGroup);
        Assert.NotNull(c.Conditions);
        Assert.Equal(2, c.Conditions!.Count);
        Assert.Equal("member_count", c.Conditions[0].Type);
    }

    [Fact]
    public void Effect_MissingOptionalFields_AreNull()
    {
        var e = JsonSerializer.Deserialize<Effect>("{\"type\":\"veil\",\"amount\":5}", SimJson.Content)!;

        Assert.Equal("veil", e.Type);
        Assert.Equal(5, e.Amount);
        Assert.Null(e.Key);
        Assert.Null(e.Id);
        Assert.Null(e.TargetScope);
    }
}
