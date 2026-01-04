using System.Text.Json;
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
    /// Extracts QueryFilterRequest from HttpContext query string
    /// Supports both simple query parameters and JSON-encoded complex filters
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="defaultPageSize">Default page size (default: 20)</param>
    /// <param name="maxPageSize">Maximum allowed page size (default: 100)</param>
    /// <returns>QueryFilterRequest populated from query string</returns>
    public static QueryFilterRequest GetQueryFilter(
        this HttpContext httpContext,
        int defaultPageSize = 20,
        int maxPageSize = 100)
    {
        var query = httpContext.Request.Query;
        var request = new QueryFilterRequest
        {
            Take = defaultPageSize,
            Skip = 0,
            IncludeCount = true
        };

        // Parse pagination parameters
        if (query.ContainsKey("take") && int.TryParse(query["take"], out var take))
        {
            request.Take = Math.Min(take > 0 ? take : defaultPageSize, maxPageSize);
        }
        else if (query.ContainsKey("pageSize") && int.TryParse(query["pageSize"], out var pageSize))
        {
            request.Take = Math.Min(pageSize > 0 ? pageSize : defaultPageSize, maxPageSize);
        }

        if (query.ContainsKey("skip") && int.TryParse(query["skip"], out var skip))
        {
            request.Skip = skip > 0 ? skip : 0;
        }
        else if (query.ContainsKey("pageNumber") && int.TryParse(query["pageNumber"], out var pageNumber))
        {
            // Convert page number to skip (pageNumber is 1-based)
            request.Skip = pageNumber > 1 ? (pageNumber - 1) * request.Take : 0;
        }

        // Parse includeCount
        if (query.ContainsKey("includeCount") && bool.TryParse(query["includeCount"], out var includeCount))
        {
            request.IncludeCount = includeCount;
        }

        // Parse sorting (JSON format)
        if (query.ContainsKey("sort") && !string.IsNullOrWhiteSpace(query["sort"]))
        {
            try
            {
                request.Sort = JsonSerializer.Deserialize<List<SortDescriptor>>(query["sort"]!, JsonOptions);
            }
            catch
            {
                // Fallback: Simple sort format "field:direction"
                var sortParts = query["sort"].ToString().Split(':');
                if (sortParts.Length > 0)
                {
                    request.Sort = new List<SortDescriptor>
                    {
                        new()
                        {
                            Field = sortParts[0],
                            Dir = sortParts.Length > 1 && sortParts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                                ? ListSortDirection.Descending
                                : ListSortDirection.Ascending
                        }
                    };
                }
            }
        }

        // Parse filter (JSON format)
        if (query.ContainsKey("filter") && !string.IsNullOrWhiteSpace(query["filter"]))
        {
            try
            {
                request.Filter = JsonSerializer.Deserialize<FilterCondition>(query["filter"]!, JsonOptions);
            }
            catch
            {
                // Ignore invalid filter JSON
            }
        }

        // Parse cursor (JSON format for keyset pagination)
        if (query.ContainsKey("cursor") && !string.IsNullOrWhiteSpace(query["cursor"]))
        {
            try
            {
                request.Cursor = JsonSerializer.Deserialize<FilterCondition>(query["cursor"]!, JsonOptions);
            }
            catch
            {
                // Ignore invalid cursor JSON
            }
        }

        return request;
    }

    /// <summary>
    /// Extracts QueryFilterRequest from HttpContext request body (POST/PUT)
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QueryFilterRequest from request body</returns>
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
