using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace DistributedRendering.AME.Server.Lib.Util;

/// <summary>
///     A schema filter that removes unnecessary properties from OpenAPI schemas.
///     Specifically, it removes the "declaredType" and "urlHelper" properties if present.
///     This only serves to speed up the Swagger UI as it becomes unusably slow when
///     controller methods are properly annotated.
/// </summary>
internal class SchemasFilter : ISchemaFilter
{
	/// <summary>
	///     Applies the filter to the given <see cref="OpenApiSchema" />, removing
	///     the "declaredType" and "urlHelper" properties if they exist.
	/// </summary>
	/// <param name="schema">The OpenAPI schema to modify.</param>
	/// <param name="context">The context for the schema filter.</param>
	public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
	{
		schema.Properties?.Remove("declaredType");
		schema.Properties?.Remove("urlHelper");
	}
}