using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AkroGame.ECS.Inspector
{
    internal static class InspectorRoutes
    {
        internal static void RegisterRoutes(
            IInspectorRoutes inspectorService,
            IEndpointRouteBuilder builder
        )
        {
            builder.MapGet("/debug/groups", inspectorService.GetGroups).AllowAnonymous();
            builder.MapGet("/debug/entities", inspectorService.GetEntities).AllowAnonymous();
            builder
                .MapGet("/debug/group/{groupId}/entity/{entityId}", inspectorService.GetEntity)
                .AllowAnonymous();
            builder.MapGet("/debug/engines", inspectorService.GetEngines).AllowAnonymous();
            builder.MapPut("/debug/group/{groupId}/entity/{entityId}/{componentName}", inspectorService.SetComponent).AllowAnonymous();
        }
    }
}
