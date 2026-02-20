namespace DistributedRendering.AME.Server.Lib;

[AttributeUsage(AttributeTargets.Property)]
internal class DatabaseNameAttribute(
	string databaseColumnName,
	short ordinal
) : Attribute
{
	internal string DatabaseColumnName => databaseColumnName;
	internal short Ordinal => ordinal;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class DatabaseTableAttribute(
	string tableName
) : Attribute
{
	internal string TableName => tableName;
}