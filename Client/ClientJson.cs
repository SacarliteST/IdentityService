using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.Client;

public static class ClientJson
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
