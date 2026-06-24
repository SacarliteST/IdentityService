namespace IdentityService.Client;

public sealed class IdentityClientOptions
{
    public const string SectionKey = "IdentityService";
    public required Uri BaseAddress { get; set; }
}
