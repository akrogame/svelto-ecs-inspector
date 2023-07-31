using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var components = new Dictionary<string, ComponentWithData>();
            if (!groupEntityComponentsDB.ContainsKey(context.groupID))
                return SocketUtil.Serialize(MakeEnvelope(components));
            foreach (var componentId in groupEntityComponentsDB[context.groupID].keys)
            {
                var componentType = ComponentTypeMap.FetchType(componentId);
                MethodInfo? queryMethod = typeof(EntityNativeDBExtensions)
                    .GetMethods()
                    .FirstOrDefault(x => x.Name == "TryGetEntity" && x.GetParameters().Length == 4);
                if (queryMethod is null)
                {
                    Svelto.Console.LogError(
                        "Could not find query method for component " + componentType.Name
                    );
                    continue;
                }
                queryMethod = queryMethod.MakeGenericMethod(componentType);
                if (queryMethod is null)
                {
                    Svelto.Console.LogError(
                        "Could not make generic query method for component " + componentType.Name
                    );
                    continue;
                }
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

                // Not all components are serializable, instead of trying to guess it just try to serialize it and see if it fails
                // (for example components with NativeDynamicArray fail serialization)
                try
                {
                    var @params = new object[]
                    {
                        entitiesDB,
                        context.entityID,
                        context.groupID,
                        null!
                    };
                    var found = queryMethod?.Invoke(entitiesDB, @params);
                    if (found is null || !(bool)found)
                        continue;
                    if (@params[3] is null)
                        continue;
                    var rawComponentData = JObject.FromObject(@params[3], serializer);
                    var componentWithData = new ComponentWithData(
                        componentType.Name,
                        rawComponentData
                    );
                    components.Add(
                        componentType.AssemblyQualifiedName.Replace(" ", ""),
                        componentWithData
                    );
                }
                catch (Exception ex)
                {
                    Svelto.Console.LogException(
                        ex,
                        "Failed to serialize component " + componentType.Name
                    );
                }
            }

            return SocketUtil.Serialize(MakeEnvelope(components));
        }
    }
}
