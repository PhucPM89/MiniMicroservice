using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Messaging;

public static class MessagingJsonDefaults
{
    public static readonly JsonSerializerOptions Default = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
