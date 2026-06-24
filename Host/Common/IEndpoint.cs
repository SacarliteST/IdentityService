namespace IdentityService.Host.Common;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
