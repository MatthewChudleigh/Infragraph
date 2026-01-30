namespace Infragraph.Server.Endpoints;

using Infragraph.Common.Configuration;

/// <summary>
/// API endpoints for resource type information.
/// </summary>
public static class ResourceEndpoints
{
    /// <summary>
    /// Maps resource-related endpoints.
    /// </summary>
    public static RouteGroupBuilder MapResourceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/resources/types", GetSupportedTypes)
            .WithName("GetSupportedResourceTypes")
            .WithDescription("Returns a list of supported AWS resource types")
            .Produces<IEnumerable<ResourceTypeInfo>>(StatusCodes.Status200OK);

        group.MapGet("/resources/types/{type}", GetResourceType)
            .WithName("GetResourceType")
            .WithDescription("Returns information about a specific resource type")
            .Produces<ResourceTypeInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/resources/categories", GetCategories)
            .WithName("GetResourceCategories")
            .WithDescription("Returns a list of resource categories")
            .Produces<IEnumerable<CategoryInfo>>(StatusCodes.Status200OK);

        return group;
    }

    private static IResult GetSupportedTypes()
    {
        return Results.Ok(SupportedResourceTypes.All.Values.OrderBy(t => t.DisplayName));
    }

    private static IResult GetResourceType(string type)
    {
        if (SupportedResourceTypes.All.TryGetValue(type, out var info))
        {
            return Results.Ok(info);
        }

        return Results.NotFound(new { error = $"Resource type '{type}' not found" });
    }

    private static IResult GetCategories()
    {
        var categories = SupportedResourceTypes.Categories
            .Select(c => new CategoryInfo
            {
                Name = c,
                ResourceTypes = SupportedResourceTypes.GetByCategory(c)
                    .Select(t => t.Type)
                    .ToList()
            })
            .ToList();

        return Results.Ok(categories);
    }
}

/// <summary>
/// Information about a resource category.
/// </summary>
public sealed class CategoryInfo
{
    public required string Name { get; init; }
    public List<string> ResourceTypes { get; init; } = [];
}
