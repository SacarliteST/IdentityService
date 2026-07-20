namespace IdentityService.Client;

public sealed class ApiProblem
{
    public int? Status { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Code { get; set; }
}
