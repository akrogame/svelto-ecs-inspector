using System;
using System.Collections.Generic;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Websocket.Streams
{
    public struct DashboardData
    {
        public DashboardData(Dictionary<string, int> groups)
        {
            Groups = groups;
        }

        public Dictionary<string, int> Groups { get; }
    }

    public class DashboardDataStream : InspectorDataStream<EmptyContext>
    {
        private readonly FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<ComponentID, ITypeSafeDictionary>
        > groupEntityComponentsDB;

        public DashboardDataStream(string key, EnginesMetadata meta, EnginesRoot enginesRoot)
            : base(key, TimeSpan.FromSeconds(1.0 / 3))
        {
            groupEntityComponentsDB = enginesRoot.GetGroupEntityComponentsDB();
        }

        protected override ArraySegment<byte> FetchData(EmptyContext context)
        {
            var groups = new Dictionary<string, int>();
            foreach (var componentsIt in groupEntityComponentsDB)
            {
                var group = componentsIt.key;
                var components = componentsIt.value;
                var entityCount = 0;

                foreach (var componentEntityMappingIt in components)
                {
                    var componentEntityMapping = componentEntityMappingIt.value;
                    entityCount = Math.Max(entityCount, componentEntityMapping.count);
                }
                groups[group.ToString()] = entityCount;
            }
            return SocketUtil.Serialize(MakeEnvelope(new DashboardData(groups)));
        }
    }
}
