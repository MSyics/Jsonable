using System.ComponentModel;
using Xunit.Abstractions;

namespace Msyics.Tests;

public class JsonTests
{
    readonly ITestOutputHelper testOutput;

    public JsonTests(ITestOutputHelper testOutput)
    {
        this.testOutput = testOutput;
    }

    string Serialize(object? value)
    {
        return JsonSerializer.Serialize(value);
    }

    [Fact]
    public void When_JSON読込_Expect_等値_シリアライズ()
    {
        using var stream = new FileStream("test.json", FileMode.Open);
        using var reader = new StreamReader(stream);
        var expected = reader.ReadToEnd();

        stream.Position = 0;
        string actual = JsonSerializer.Serialize(
            JsonDocument.Parse(stream).ToDynamic() as DynamicJsonObject,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void When_JSON読込_Expect_等値()
    {
        using var stream = new FileStream("test.json", FileMode.Open);
        dynamic actual = JsonDocument.Parse(stream).ToDynamic()!;

        Assert.Equal("string", actual._string_);
        Assert.Equal(0.1, actual._number_);
        Assert.Equal(true, actual._true_);
        Assert.Equal(false, actual._false_);
        Assert.Equal(null, actual._null_);

        Assert.Equal("0", actual._array_[0]);
        Assert.Equal(0, actual._array_[1]);
        Assert.Equal(true, actual._array_[2]);
        Assert.Equal(false, actual._array_[3]);
        Assert.Equal(null, actual._array_[4]);
        Assert.Equal(0, actual._array_[5][0]);
        Assert.Equal(1, actual._array_[5][1]);
        Assert.Equal(2, actual._array_[5][2]);

        Assert.Equal(0, actual._array_[6]._object_);
        Assert.Equal("0", actual["_object_"]["_array_"][0]);
    }

    [Fact]
    public void When_メンバー取得_不正キー_Expect_Null()
    {
        dynamic actual = Jsonable.CreateObject();

        Assert.Null(actual[""]);
        Assert.Null(actual[null]);
        Assert.Null(actual[" "]);
    }

    [Fact]
    public void When_メンバー取得_非存在_Expect_NotNull()
    {
        dynamic actual = Jsonable.CreateObject();

        Assert.NotNull(actual._0_);
        Assert.NotNull(actual["0"]);
    }

    [Theory]
    [InlineData("{}")]
    public void When_メンバー取得_非存在_Expect_(string json)
    {
        dynamic @object = Jsonable.CreateObject();
        _ = @object._0_;
        _ = @object._0_._0_;
        _ = @object["_0_"];
        _ = @object["_0_"]["_0_"];

        var actual = JsonSerializer.Serialize(@object);

        Assert.Equal(json, actual);
    }

    [Fact]
    public void When_メンバー設定_Expect_上書き()
    {
        dynamic actual = Jsonable.CreateObject();
        actual._0_ = 0;
        actual["0"] = 0;

        Assert.Equal(0, actual._0_);
        Assert.Equal(0, actual["0"]);
    }

    [Fact]
    public void When_メンバー設定_Expect_アクセスチェーン()
    {
        dynamic actual = Jsonable.CreateObject(x =>
        {
            x._0_._0_ = 0;
        });

        Assert.Equal(0, actual._0_._0_);
    }

    [Fact]
    public void When_メンバー設定_Expect_変更通知()
    {
        dynamic actual = Jsonable.CreateObject();

        Assert.PropertyChanged(actual as INotifyPropertyChanged, nameof(actual._0_), () => { actual._0_ = 0; });
    }

    [Fact]
    public void When_メンバー削除_Expect_メンバー件数()
    {
        dynamic actual = Jsonable.CreateObject();
        actual._0_ = 0;
        actual.Remove("_0_");

        Assert.Equal(0, actual.Members.Count);
    }

    [Fact]
    public void When_メンバー削除_すべて_Expect_メンバー件数()
    {
        dynamic actual = Jsonable.CreateObject();
        actual._0_ = 0;
        actual.Clear();

        Assert.Equal(0, actual.Members.Count);
    }

    [Theory]
    [InlineData("_0_", 0, "[_0_, 0]")]
    [InlineData(null, null, "")]
    public void When_ToString_Expect_(string? key, int? value, string expected)
    {
        dynamic actual = Jsonable.CreateObject();
        if (key is not null)
        {
            actual[key] = value;
        }

        Assert.Equal(expected, actual.ToString());
    }

    [Theory]
    [InlineData("{\"_0_\":{\"_0_\":0}}")]
    public void When_シリアライズ_Expect_(string json)
    {
        dynamic actual = Jsonable.CreateObject();

        actual._0_._0_ = 0;
        Assert.Equal(json, Serialize(actual));

        actual.Clear()._0_["_0_"] = 0;
        Assert.Equal(json, Serialize(actual));

        actual.Clear()["_0_"]["_0_"] = 0;
        Assert.Equal(json, Serialize(actual));

        actual.Clear()["_0_"]._0_ = 0;
        Assert.Equal(json, Serialize(actual));

        actual.Clear()._0_ = Jsonable.CreateObject(x => { x._0_ = 0; });
        Assert.Equal(json, Serialize(actual));

        actual.Clear()._0_ = Jsonable.CreateObject(x => { x["_0_"] = 0; });
        Assert.Equal(json, Serialize(actual));

        actual.Clear()["_0_"] = Jsonable.CreateObject(x => { x["_0_"] = 0; });
        Assert.Equal(json, Serialize(actual));

        actual.Clear()["_0_"] = Jsonable.CreateObject(x => { x._0_ = 0; });
        Assert.Equal(json, Serialize(actual));
    }

    [Theory]
    [InlineData(true, true, 0)]
    public void When_メンバー設定_Expect_(bool isEmpty, bool isNotNull, int memberCount)
    {
        dynamic actual = Jsonable.CreateObject();
        actual._0_._0_ = Jsonable.CreateObject(x => _ = x._0_);

        Assert.Equal(isEmpty, actual._0_.IsEmpty);
        Assert.Equal(isEmpty, actual._0_._0_.IsEmpty);
        Assert.Equal(isEmpty, actual._0_._0_._0_.IsEmpty);
        Assert.Equal(isEmpty, actual["_0_"].IsEmpty);

        Assert.Equal(isNotNull, actual._0_ is not null);
        Assert.Equal(isNotNull, actual._0_._0_ is not null);
        Assert.Equal(isNotNull, actual._0_._0_._0_ is not null);
        Assert.Equal(isNotNull, actual["_0_"] is not null);

        Assert.Equal(memberCount, actual._0_.Members.Count);
        Assert.Equal(memberCount, actual._0_._0_.Members.Count);
        Assert.Equal(memberCount, actual._0_._0_._0_.Members.Count);
        Assert.Equal(memberCount, actual["_0_"].Members.Count);
    }

    [Fact]
    public void When_IsNullOrEmpty()
    {
        Assert.True(Jsonable.IsNullOrEmpty(Jsonable.CreateObject()._0_._0_));
        Assert.True(Jsonable.IsNullOrEmpty(null));
        Assert.True(Jsonable.IsNullOrEmpty(""));
        Assert.True(Jsonable.IsNullOrEmpty(Enumerable.Empty<object>()));

        Assert.False(Jsonable.IsNullOrEmpty(new object()));
    }

    [Fact]
    public void When_列挙()
    {
        Assert.Empty(Jsonable.Enumerate(Jsonable.CreateObject()));
        Assert.Empty(Jsonable.Enumerate(0));
        Assert.Empty(Jsonable.Enumerate(null));
        Assert.Empty(Jsonable.Enumerate(Enumerable.Empty<object>()));
    }

    [Fact]
    public void When_JSON値種類()
    {
        Assert.Equal(JsonValueKind.Null, Jsonable.GetValueKind(null));
        Assert.Equal(JsonValueKind.Number, Jsonable.GetValueKind(0));
        Assert.Equal(JsonValueKind.Number, Jsonable.GetValueKind((object?)10));
        Assert.Equal(JsonValueKind.True, Jsonable.GetValueKind(true));
        Assert.Equal(JsonValueKind.False, Jsonable.GetValueKind(false));
        Assert.Equal(JsonValueKind.String, Jsonable.GetValueKind(""));
        Assert.Equal(JsonValueKind.Array, Jsonable.GetValueKind(Enumerable.Empty<string>()));
        Assert.Equal(JsonValueKind.Object, Jsonable.GetValueKind(new object()));
        Assert.Equal(JsonValueKind.Object, Jsonable.GetValueKind(new Dictionary<string, object>()));
        Assert.Equal(JsonValueKind.Object, Jsonable.GetValueKind(new Dictionary<Guid, object>()));
        Assert.Equal(JsonValueKind.Object, Jsonable.GetValueKind(new Dictionary<DateTimeOffset, object>()));
        Assert.Equal(JsonValueKind.Undefined, Jsonable.GetValueKind(new Dictionary<object, object>()));
    }
}