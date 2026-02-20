using System.Reflection;

using DistributedRendering.AME.Server.Lib.Interfaces;
using DistributedRendering.AME.Server.Lib.Models;

namespace DistributedRendering.AME.Server.Lib.Util;

internal static class Extensions
{
	internal static bool IsValid(this IRenderRequest request)
	{
		if (request.Guid == Guid.Empty) return false;
		if (string.IsNullOrWhiteSpace(request.ProjectPath)) return false;
		if (string.IsNullOrWhiteSpace(request.EncoderPresetPath)) return false;
		if (string.IsNullOrWhiteSpace(request.OutputPath)) return false;

		return true;
	}

	internal static DbRequest ConvertTo<TRequest>(this TRequest request)
		where TRequest : IRenderRequest
	{
		return new DbRequest
		{
			Guid = request.Guid,
			RequestDirectory = request.RequestDirectory,
			ProjectFile = new FileInfo(request.ProjectPath),
			EncoderPresetFile = new FileInfo(request.EncoderPresetPath),
			OutputPath = new FileInfo(request.OutputPath),
			TimelineLength = request.TimelineDuration,
			CreatedAt = request.CreatedAt,
			CompletedAt = request.CompletedAt
		};
	}

	internal static TRequest ConvertTo<TRequest>(this DbRequest request)
		where TRequest : class, IRenderRequest, new()
	{
		return new TRequest
		{
			Guid = request.Guid,
			RequestDirectory = request.RequestDirectory,
			ProjectPath = request.ProjectFile.FullName,
			EncoderPresetPath = request.EncoderPresetFile.FullName,
			OutputPath = request.OutputPath.FullName,
			TimelineDuration = request.TimelineLength,
			CreatedAt = request.CreatedAt,
			CompletedAt = request.CompletedAt ?? default
		};
	}

	extension(Type type)
	{
		internal string GetTableName()
		{
			var tableNameAttribute = Attribute.GetCustomAttribute(
				type,
				typeof(DatabaseTableAttribute)
			);

			if (tableNameAttribute is not DatabaseTableAttribute nameAttribute)
				throw new ArgumentException(
					$"The type '{type.FullName}' does not have an attribute of type '{nameof(DatabaseTableAttribute)}'."
				);

			return nameAttribute.TableName;
		}

		internal string GetColumnName(string memberName)
		{
			PropertyInfo propInfo = type.GetProperty(memberName)
			                        ?? throw new ArgumentException(
				                        $"The property name '{memberName}' does not exist on type '{type.FullName}'."
			                        );

			var attribute = Attribute.GetCustomAttribute(propInfo, typeof(DatabaseNameAttribute));

			if (attribute is not DatabaseNameAttribute databaseNameAttribute)
				throw new ArgumentException(
					$"The member '{memberName}' does not have an attribute of type '{nameof(DatabaseNameAttribute)}'."
				);

			return databaseNameAttribute.DatabaseColumnName;
		}

		public DatabaseColumn GetColumn(string memberName)
		{
			PropertyInfo propInfo = type.GetProperty(memberName)
			                        ?? throw new ArgumentException(
				                        $"The property name '{memberName}' does not exist on type '{type}'."
			                        );

			var attribute = Attribute.GetCustomAttribute(propInfo, typeof(DatabaseNameAttribute));

			if (attribute is not DatabaseNameAttribute databaseNameAttribute)
				throw new ArgumentException(
					$"The member '{memberName}' of type '{type.FullName}' does not have an attribute of type '{nameof(DatabaseNameAttribute)}'."
				);

			return new DatabaseColumn
				{ Name = databaseNameAttribute.DatabaseColumnName, Ordinal = databaseNameAttribute.Ordinal };
		}
	}
}