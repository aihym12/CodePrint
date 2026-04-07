using System.Text.Json;
using System.Text.Json.Serialization;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// Provides deep-cloning for LabelElement instances using JSON serialization.
/// </summary>
public static class ElementCloneService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new LabelElementConverter() }
    };

    /// <summary>
    /// Creates a deep clone of a LabelElement, assigning a new Id.
    /// </summary>
    public static LabelElement Clone(LabelElement source)
    {
        var json = JsonSerializer.Serialize(source, source.GetType(), Options);
        var clone = JsonSerializer.Deserialize<LabelElement>(json, Options)
                    ?? throw new InvalidOperationException("Failed to clone element");
        clone.Id = Guid.NewGuid().ToString();
        return clone;
    }
}
