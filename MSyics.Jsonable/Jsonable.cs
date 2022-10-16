using System.Collections;

namespace System.Text.Json;

public static partial class Jsonable
{
    public static bool HasProblem(this HttpResponseMessage httpResponse) =>
        httpResponse is { Content.Headers.ContentType.MediaType: "application/problem+json" };

    public static async Task<dynamic?> ReadFromJsonToDynamicAsync(
        this HttpContent httpContent, JsonDocumentOptions options = default, CancellationToken cancellationToken = default) =>
        JsonDocument.Parse(await httpContent.ReadAsStreamAsync(cancellationToken), options).ToDynamic();

    internal static object? GetValue(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => GetObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(x => x.GetValue()).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };

        static DynamicJsonObject GetObject(JsonElement json)
        {
            var @object = new DynamicJsonObject();
            foreach (var property in json.EnumerateObject())
            {
                @object.Add(property.Name, property.Value.GetValue());
            }

            return @object;
        }
    }

    public static dynamic? ToDynamic(this JsonElement element) => element.GetValue();

    public static dynamic? ToDynamic(this JsonDocument document) => document.RootElement.ToDynamic();
}

public static partial class Jsonable
{
    public static dynamic CreateObject(Action<dynamic>? action = null)
    {
        DynamicJsonObject value = new();
        action?.Invoke(value);
        return value;
    }

    public static IEnumerable<dynamic> Enumerate(object? value)
    {
        if (value is IEnumerable enumerable)
        {
            return enumerable.Cast<dynamic>();
        }
        return Enumerable.Empty<dynamic>();
    }

    public static bool IsNullOrEmpty(object? value)
    {
        if (value is null) return true;
        if (value is string @string) return string.IsNullOrEmpty(@string);
        if (value is DynamicJsonObject @object) return @object.IsEmpty;
        if (value is IEnumerable<object> @e) return !e.Any();

        return false;
    }

    public static JsonValueKind GetValueKind(object? value)
    {
        if (value is null) { return JsonValueKind.Null; }
        if (value is true) { return JsonValueKind.True; }
        if (value is false) { return JsonValueKind.False; }
        if (value is string) { return JsonValueKind.String; }
        if (IsNumber(value.GetType())) { return JsonValueKind.Number; }
        switch (IsDictionary(value.GetType()))
        {
            case false:
                break;
            case true:
                return JsonValueKind.Object;
            default:
                return JsonValueKind.Undefined;
        }
        if (value is IEnumerable) { return JsonValueKind.Array; }
        return JsonValueKind.Object;

        static bool IsNumber(Type? type)
        {
            if (type is null) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumber(Nullable.GetUnderlyingType(type));
                    }
                    return false;
                default:
                    return false;
            }
        }

        static bool? IsDictionary(Type type)
        {
            var key = type.GetInterface("IDictionary`2")?.GetGenericArguments()?[0];
            switch (Type.GetTypeCode(key))
            {
                case TypeCode.Empty:
                    return false;
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    if (key == typeof(DateTimeOffset) ||
                        key == typeof(Enum) ||
                        key == typeof(Guid))
                    {
                        return true;
                    }
                    return null;
            }
        }
    }
}