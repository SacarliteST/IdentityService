namespace IdentityService.Data;

public sealed class ConnectionOptions
{
    public const string SectionKey = "ConnectionStrings";
    public string? ConnectionString { get; set; }
}
