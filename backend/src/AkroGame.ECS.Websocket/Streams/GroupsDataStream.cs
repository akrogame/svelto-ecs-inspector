using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Websocket.Streams
{
    public class GroupsDataStream : InspectorDataStream<EmptyContext>
    {
        private readonly FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<RefWrapperType, ITypeSafeDictionary>
        > groupEntityComponentsDB;

        public GroupsDataStream(string key, EnginesRoot enginesRoot)
            : base(key, TimeSpan.FromSeconds(1.0 / 1))
        {
            groupEntityComponentsDB = enginesRoot.GetGroupEntityComponentsDB();
        }

        protected override ArraySegment<byte> FetchData(EmptyContext inspector)
        {
            var groups = new Dictionary<string, List<string>>();
            foreach (var item in groupEntityComponentsDB)
            {
                var components = new List<string>();

                foreach (var componentEntry in item.value)
                {
                    Type componentType = componentEntry.key;
                    components.Add(componentType.Name.ToString());
                }
                groups[item.key.ToString()] = components;
            }
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(MakeEnvelope(groups)));
        }
    }
}
