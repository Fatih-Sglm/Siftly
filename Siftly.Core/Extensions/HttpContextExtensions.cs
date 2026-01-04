using Microsoft.AspNetCore.Http;

namespace Siftly.Core;

/// <summary>
/// Extension methods for HttpContext to extract QueryFilterRequest from query string and request body
/// </summary>
public static class HttpContextExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Extracts QueryFilterRequest from HttpContext query string (GET)
    /// </summary>
    public static QueryFilterRequest GetQueryFilters(
        this HttpContext httpContext,
        int? defaultPageSize = null,
        int? maxPageSize = null)
    {
        var query = httpContext.Request.Query;
        var effectiveMaxPageSize = maxPageSize ?? QueryFilter.Options.MaxPageSize;

        var request = new QueryFilterRequest
        {
            PageNumber = 0,
            PageSize = defaultPageSize ?? QueryFilter.Options.MaxPageSize,
            IncludeCount = true
        };

        // PAGE SIZE (take or pageSize)
        if (query.TryGetValue("take", out var takeVal) && int.TryParse(takeVal, out var take) && take > 0)
        {
            request.PageSize = Math.Min(take, effectiveMaxPageSize);
        }
        else if (query.TryGetValue("pageSize", out var pageSizeVal) && int.TryParse(pageSizeVal, out var pageSize) && pageSize > 0)
        {
            request.PageSize = Math.Min(pageSize, effectiveMaxPageSize);
        }

        // PAGE NUMBER (skip or pageNumber)
        if (query.TryGetValue("skip", out var skipVal) && int.TryParse(skipVal, out var skip) && skip > 0)
        {
            request.PageNumber = skip;
        }
        else if (query.TryGetValue("pageNumber", out var pageNumberVal) && int.TryParse(pageNumberVal, out var pageNumber) && pageNumber > 1)
        {
            request.PageNumber = (pageNumber - 1) * request.PageSize;
        }

        // INCLUDE COUNT
        if (query.TryGetValue("includeCount", out var includeCountVal) && bool.TryParse(includeCountVal, out var includeCount))
        {
            request.IncludeCount = includeCount;
        }

        // SORT
        ParseSort(query, request);

        // FILTER
        ParseFilter(query, request);

        return request;
    }

    private static void ParseSort(IQueryCollection query, QueryFilterRequest request)
    {
        if (query.TryGetValue("sort", out var sortVal) && !string.IsNullOrWhiteSpace(sortVal))
        {
            // Try JSON first, then fallback to field:dir format
            try
            {
                request.Sort = JsonSerializer.Deserialize<List<SortDescriptor>>(sortVal!, JsonOptions);
            }
            catch
            {
                var parts = sortVal.ToString().Split(':');
                if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                {
                    request.Sort =
                    [
                        new SortDescriptor
                        {
                            Field = parts[0],
                            Dir = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                                ? ListSortDirection.Descending
                                : ListSortDirection.Ascending
                        }
                    ];
                }
            }
            return;
        }

        // Bracket notation: sort[0][field], sort[0][dir]
        var sortList = new List<SortDescriptor>();
        foreach (var key in query.Keys.Where(k => k.StartsWith("sort[") && k.EndsWith("][field]")))
        {
            var index = key.Split('[', ']')[1];
            var field = query[key].ToString();
            var dirKey = $"sort[{index}][dir]";

            var dir = ListSortDirection.Ascending;
            if (query.TryGetValue(dirKey, out var dirVal) && dirVal.ToString().Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                dir = ListSortDirection.Descending;
            }

            sortList.Add(new SortDescriptor { Field = field, Dir = dir });
        }

        if (sortList.Count > 0)
        {
            request.Sort = sortList;
        }
    }

    private static void ParseFilter(IQueryCollection query, QueryFilterRequest request)
    {
        if (query.TryGetValue("filter", out var filterVal) && !string.IsNullOrWhiteSpace(filterVal))
        {
            // Try JSON parse
            try
            {
                request.Filter = JsonSerializer.Deserialize<FilterCondition>(filterVal!, JsonOptions);
            }
            catch
            {
                // Ignore invalid JSON
            }
            return;
        }

        // Bracket notation: filter[field], filter[operator], filter[value]
        if (query.TryGetValue("filter[field]", out var field) &&
            query.TryGetValue("filter[operator]", out var op) &&
            query.TryGetValue("filter[value]", out var value) &&
            Enum.TryParse<FilterOperator>(op, true, out var filterOperator))
        {
            request.Filter = new FilterCondition
            {
                Field = field.ToString(),
                Operator = filterOperator,
                Value = value.ToString()
            };
        }
    }

    /// <summary>
    /// Extracts QueryFilterRequest from HttpContext request body (POST/PUT)
    /// </summary>
    public static async Task<QueryFilterRequest?> GetQueryFilterFromBodyAsync(
        this HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<QueryFilterRequest>(
                httpContext.Request.Body,
                JsonOptions,
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
