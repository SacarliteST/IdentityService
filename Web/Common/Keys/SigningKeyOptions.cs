namespace IdentityService.Web.Common.Keys;

public sealed class SigningKeyOptions
{
    public const string SectionKey = "SigningKey";

    public string? PrivateKeyPem { get; init; }
    public string? KeyFilePath { get; init; }
    public string? Kid { get; init; }
}
