namespace Siftly.Core;

/// <summary>
/// Base class for filter descriptors
/// </summary>
public abstract class FilterDescriptorBase
{
    /// <summary>
    /// Whether the filter comparison should be case sensitive. 
    /// Defaults to true for better performance and alignment with typical database behaviors.
    /// </summary>
    public bool CaseSensitiveFilter { get; set; } = true;
}
