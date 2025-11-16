namespace MarketInsight.Shared.Options;

/// <summary>
/// Strongly-typed configuration for database access.
/// Bind from <c>"Database"</c> section in appsettings.json.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string to your SQL Server / Azure SQL / etc.
    /// Example: "Server=THOMASXPS15NEW;Database=MarketInsight;Trusted_Connection=True;TrustServerCertificate=True"
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
