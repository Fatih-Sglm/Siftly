namespace Siftly.Core;

/// <summary>
/// Constants used in the filtering library
/// </summary>
public static class FilterConstant
{
    public const string And = "and";
    public const string Or = "or";
    public const string OrderBy = "OrderBy";
    public const string OrderByDescending = "OrderByDescending";
    public const string ThenBy = "ThenBy";
    public const string ThenByDescending = "ThenByDescending";
    public const string Any = "Any";

    public static class Operators
    {
        public const string IsLessThan = "lt";
        public const string IsLessThanOrEqualTo = "lte";
        public const string IsEqualTo = "eq";
        public const string IsNotEqualTo = "neq";
        public const string IsGreaterThanOrEqualTo = "gte";
        public const string IsGreaterThan = "gt";
        public const string StartsWith = "startswith";
        public const string EndsWith = "endswith";
        public const string Contains = "contains";
        public const string IsContainedIn = "containedin";
        public const string DoesNotContain = "doesnotcontain";
        public const string IsNull = "isnull";
        public const string IsNotNull = "isnotnull";
        public const string IsEmpty = "isempty";
        public const string IsNotEmpty = "isnotempty";
        public const string IsNullOrEmpty = "isnullorempty";
        public const string IsNotNullOrEmpty = "isnotnullorempty";
        public const string In = "in";
    }

    public static class Directions
    {
        public const string Asc = "asc";
        public const string Desc = "desc";
    }

    public static class Prefixes
    {
        public const string Collection = "_collection_:";
        public const string ManyToMany = "_m2m_:";
    }
}
