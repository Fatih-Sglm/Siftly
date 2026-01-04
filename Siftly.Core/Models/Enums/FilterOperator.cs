
using Siftly.Core.Infrastructure.Converters;

namespace Siftly.Core;

/// <summary>
/// Filter operators for query filtering (Kendo UI compatible)
/// </summary>
[JsonConverter(typeof(FilterOperatorJsonConverter))]
[ModelBinder(typeof(FilterOperatorModelBinder))]
public enum FilterOperator
{
    /// <summary>Less than</summary>
    IsLessThan,
    /// <summary>Less than or equal to</summary>
    IsLessThanOrEqualTo,
    /// <summary>Equal to</summary>
    IsEqualTo,
    /// <summary>Not equal to</summary>
    IsNotEqualTo,
    /// <summary>Greater than or equal to</summary>
    IsGreaterThanOrEqualTo,
    /// <summary>Greater than</summary>
    IsGreaterThan,
    /// <summary>Starts with (string)</summary>
    StartsWith,
    /// <summary>Ends with (string)</summary>
    EndsWith,
    /// <summary>Contains (string)</summary>
    Contains,
    /// <summary>Is contained in collection</summary>
    IsContainedIn,
    /// <summary>Does not contain (string)</summary>
    DoesNotContain,
    /// <summary>Is null</summary>
    IsNull,
    /// <summary>Is not null</summary>
    IsNotNull,
    /// <summary>Is empty (string)</summary>
    IsEmpty,
    /// <summary>Is not empty (string)</summary>
    IsNotEmpty,
    /// <summary>Is null or empty (string)</summary>
    IsNullOrEmpty,
    /// <summary>Is not null or empty (string)</summary>
    IsNotNullOrEmpty,
    /// <summary>In collection</summary>
    In
}
