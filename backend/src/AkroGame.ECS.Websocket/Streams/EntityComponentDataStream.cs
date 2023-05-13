using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Svelto.Common.Internal;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Websocket.Streams
{
    public struct ComponentWithData
    {
        public ComponentWithData(string prettyName, JObject data)
        {
            PrettyName = prettyName;
            Data = data;
        }

        public string PrettyName { get; }
        public JObject Data { get; }
    }

    public class EntityComponentDataStream : InspectorDataStream<EGID>
    {
        private readonly EntitiesDB entitiesDB;
        private readonly FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<ComponentID, ITypeSafeDictionary>
        > groupEntityComponentsDB;
        private readonly JsonSerializer serializer;

        public EntityComponentDataStream(string key, EnginesRoot enginesRoot)
            : base(key, TimeSpan.FromSeconds(1.0 / 5))
        {
            serializer = new JsonSerializer();
            entitiesDB = enginesRoot.GetEntitiesDB();
            groupEntityComponentsDB = enginesRoot.GetGroupEntityComponentsDB();
        }

        protected override ArraySegment<byte> FetchData(EGID context)
        {
            // QueryEntity<T>(this EntitiesDB entitiesDb, EGID entityGID)
            MethodInfo? queryMethod = typeof(EntityNativeDBExtensions).GetMethod(
                "QueryEntity",
                new[] { typeof(EntitiesDB), typeof(uint), typeof(ExclusiveGroupStruct) }
            );
            var components = new Dictionary<string, ComponentWithData>();
            if (!groupEntityComponentsDB.ContainsKey(context.groupID))
                return SocketUtil.Serialize(MakeEnvelope(components));
            foreach (var componentId in groupEntityComponentsDB[context.groupID].keys)
            {
                var componentType = ComponentTypeMap.FetchType(componentId);
                // This is just a quick dirty check because we can't serialize these components for sure
                if (
                    componentType.Name == "EntityInfoComponent"
                    || componentType.Name == "EntityReferenceComponent"
                    || componentType.Name == "EGIDComponent"
                    || componentType
                        .GetFields()
                        .Any(x => x.FieldType.Name.StartsWith("NativeDynamicArray"))
                    || componentType
                        .GetProperties()
                        .Any(x => x.PropertyType.Name.StartsWith("NativeDynamicArray"))
                )
                    continue;

                MethodInfo? generic = queryMethod?.MakeGenericMethod(componentType);
                if (generic is null)
                    continue;

                // Not all components are serializable, instead of trying to guess it just try to serialize it and see if it fails
                // (for example components with NativeDynamicArray fail serialization)
                try
                {
                    var componentData = generic?.Invoke(
                        entitiesDB,
                        new object[] { entitiesDB, context.entityID, context.groupID }
                    );
                    if (componentData is null)
                        continue;
                    var rawComponentData = JObject.FromObject(componentData, serializer);
                    var componentWithData = new ComponentWithData(
                        componentType.Name,
                        rawComponentData
                    );
                    components.Add(
                        componentType.AssemblyQualifiedName.Replace(" ", ""),
                        componentWithData
                    );
                }
                catch (Exception) { }
            }

            return SocketUtil.Serialize(MakeEnvelope(components));
        }
    }
}
