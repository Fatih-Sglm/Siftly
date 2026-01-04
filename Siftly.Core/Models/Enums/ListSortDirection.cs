using Siftly.Core.Infrastructure.Converters;

namespace Siftly.Core;

/// <summary>
/// Sort direction for query sorting
/// </summary>
[JsonConverter(typeof(ListSortDirectionJsonConverter))]
[ModelBinder(typeof(ListSortDirectionModelBinder))]
public enum ListSortDirection
{
    /// <summary>Ascending sort</summary>
    Ascending,
    /// <summary>Descending sort</summary>
    Descending
}
