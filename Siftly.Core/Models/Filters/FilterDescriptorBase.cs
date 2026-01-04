namespace Siftly.Core;

/// <summary>
/// Base class for filter descriptors
/// </summary>
public abstract class FilterDescriptorBase
{
    /// <summary>
    /// Whether the filter comparison should be case sensitive
    /// </summary>
    public bool CaseSensitiveFilter { get; set; } = false;
}
