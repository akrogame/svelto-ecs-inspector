using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AkroGame.ECS.Inspector
{
    internal interface IInspectorRoutes
    {
        Task<IResult> GetEngines();
        Task<IResult> GetEntities();
        Task<IResult> GetEntity(uint groupId, uint entityId);
        Task<IResult> GetGroups();
        void UpdateFromMainThread();
    }
}
