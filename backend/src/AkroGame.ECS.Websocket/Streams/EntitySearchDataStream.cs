using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Websocket.Streams
{
    // TODO: ability to search for any field in any component
    public struct SearchContext
    {
        public string SearchTerm { get; }

        public SearchContext(string searchTerm)
        {
            SearchTerm = searchTerm;
        }
    }

    public struct GroupData
    {
        public GroupData(string name, uint[] entities)
        {
            Name = name;
            Entities = entities;
        }

        public string Name { get; }
        public uint[] Entities { get; }
    }

    public class EntitySearchDataStream : InspectorDataStream<SearchContext>
    {
        private readonly FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<ComponentID, ITypeSafeDictionary>
        > groupEntityComponentsDB;

        public EntitySearchDataStream(string key, EnginesRoot enginesRoot)
            : base(key, TimeSpan.FromSeconds(1.0 / 2))
        {
            groupEntityComponentsDB = enginesRoot.GetGroupEntityComponentsDB();
        }

        protected override ArraySegment<byte> FetchData(SearchContext context)
        {
            var groups = new Dictionary<uint, GroupData>();
            foreach (var componentsIt in groupEntityComponentsDB)
            {
                var group = componentsIt.key;
                var components = componentsIt.value;
                var entities = new HashSet<uint>();

                foreach (var componentEntityMappingIt in components)
                {
                    var componentEntityMapping = componentEntityMappingIt.value;
                    componentEntityMapping.KeysEvaluator(
                        x =>
                        {
                            if (
                                string.IsNullOrEmpty(context.SearchTerm)
                                || x.ToString().Contains(context.SearchTerm)
                            )
                                entities.Add(x);
                        }
                    );
                }
                groups[group.ToIDAndBitmask()] = new GroupData(
                    group.ToString(),
                    entities.OrderBy(x => x).Take(10).ToArray()
                );
            }
            return SocketUtil.Serialize(MakeEnvelope(groups));
        }
    }
}
