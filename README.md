# Jsonable

[![Nuget](https://img.shields.io/nuget/v/Jsonable)](https://www.nuget.org/packages/Jsonable)

Dynamic json operations.

## Usage 

Convert from JsonDocument or JsonElement to dynamic.
```csharp
using System.Text.Json;

// ToDynamic
dynamic json = JsonDocument.Parse("json").ToDynamic()!
```

Using DynamicJsonObject
```csharp
using System.Text.Json;

// Create
dynamic json = Jsonable.CreateObject(x => 
{
    x.user.name = "name";
    x.user.age = 0;
});

// Add member
json.address = "address"; // or
json["address"] = "address";

// Delete member
json.Remove("address");
json.Clear();

// Chaining Member access
json.user.address = ""; // or
json["user"]["address"] = "";

JsonSerializer.Serialize(json) ...
```

## Example

Web API POST
```csharp
dynamic req = Jsonable.CreateObject(...
var message = await new HttpClient().PostAsJsonAsync("https://api.xxx.xxx/xxx", (object)req);
dynamic res = message.Content.ReadFromJsonToDynamicAsync();
```

Blazor WebAssembly 'Weather forecast'
```csharp
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.date</td>
                    <td>@forecast.temperatureC</td>
                    <td>@(32 + (int)(forecast.temperatureC / 0.5556))</td>
                    <td>@forecast.summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private dynamic? forecasts;

    protected override async Task OnInitializedAsync()
    {
        var message = await Http.GetAsync("sample-data/weather.json");
        forecasts = await message.Content.ReadFromJsonToDynamicAsync();
    }
}
```
