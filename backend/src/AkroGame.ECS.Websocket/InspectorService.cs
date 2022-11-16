using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Web;
using AkroGame.ECS.Websocket.Streams;
using Newtonsoft.Json;
using Svelto.DataStructures;
using Svelto.ECS;

namespace AkroGame.ECS.Websocket
{
    public class InspectorService
    {
        private readonly IWebSocket ws;
        private readonly ConcurrentQueue<Envelope<int, string[]>> messages;
        private readonly Dictionary<string, IInspectorDataStream> streams;
        private readonly EntitiesDB entitiesDB;
        private readonly List<QueryInvocation> queryInvocation;
        private readonly EntityComponentDataStream entityComponentDataStream;
        private readonly GroupsDataStream groupsDataStream;
        private readonly EntitySearchDataStream entitySearchDataStream;
        private readonly DashboardDataStream dashboardDataStream;

        private const string STREAM_ENTITY_DATA = "entity-data";
        private const string STREAM_ENTITIES = "entities";
        private const string STREAM_DASHBOARD = "dashboard";
        private const string STREAM_GROUPS = "groups";
        private readonly int maxMessagesPerFrame;

        public InspectorService(IWebSocket ws, EnginesRoot enginesRoot)
        {
            this.maxMessagesPerFrame = 500;
            this.entitiesDB = enginesRoot.GetEntitiesDB();
            var meta = EnginesMetadataFactory.GetMeta();
            this.queryInvocation = meta.QueryInvocations;
            this.ws = ws;
            this.messages = new ConcurrentQueue<Envelope<int, string[]>>();
            this.streams = new Dictionary<string, IInspectorDataStream>()
            {
                {
                    STREAM_ENTITY_DATA,
                    entityComponentDataStream = new EntityComponentDataStream(
                        STREAM_ENTITY_DATA,
                        enginesRoot
                    )
                },
                {
                    STREAM_GROUPS,
                    groupsDataStream = new GroupsDataStream(STREAM_GROUPS, enginesRoot)
                },
                {
                    STREAM_ENTITIES,
                    entitySearchDataStream = new EntitySearchDataStream(
                        STREAM_ENTITIES,
                        enginesRoot
                    )
                },
                {
                    STREAM_DASHBOARD,
                    dashboardDataStream = new DashboardDataStream(
                        STREAM_DASHBOARD,
                        meta,
                        enginesRoot
                    )
                }
            };

            this.ws.OnData += InspectorMessageReceived;
            this.ws.OnClose += InspectorClosed;
        }

        /// <summary>
        /// Call from the Main Svelto Thread of your application
        /// </summary>
        public void Update(TimeSpan deltaTime)
        {
            var c = maxMessagesPerFrame;
            while ((c-- >= 0 || maxMessagesPerFrame == 0) && messages.TryDequeue(out var envelope))
            {
                var id = envelope.Id;
                var command = envelope.Payload[0];
                var args = new Span<string>(envelope.Payload, 1, envelope.Payload.Length - 1);
                switch (command)
                {
                    case "sub":
                        Subscribe(id, args);
                        break;
                    case "un-sub":
                        UnSubscribe(id, args);
                        break;
                    case "update":
                        UpdateComponentData(id, args);
                        break;
                    case "get-engines":
                        GetEngines(id, args);
                        break;
                    default:
                        break;
                }
            }
            foreach (var stream in streams)
                stream.Value.PushAll(deltaTime, ws);
        }

        private void InspectorClosed(int id)
        {
            foreach (var stream in streams)
            {
                stream.Value.UnSubscribe(id);
            }
        }

        private void InspectorMessageReceived(Envelope<int, ArraySegment<byte>> data)
        {
            messages.Enqueue(
                new Envelope<int, string[]>(
                    data.Id,
                    Encoding.UTF8.GetString(data.Payload).Split(" ")
                )
            );
        }

        private void Subscribe(int id, Span<string> args)
        {
            var stream = args[0];
            switch (stream)
            {
                case STREAM_ENTITY_DATA:

                    var groupId = SveltoUtils.CreateExclusiveGroupStruct(uint.Parse(args[1]));
                    var entityId = uint.Parse(args[2]);

                    entityComponentDataStream.Subscribe(id, new Svelto.ECS.EGID(entityId, groupId));
                    break;
                case STREAM_GROUPS:
                    groupsDataStream.Subscribe(id, default);
                    break;
                case STREAM_ENTITIES:
                    string searchQuery = args.Length > 1 ? args[1] : "";
                    entitySearchDataStream.Subscribe(
                        id,
                        new SearchContext(HttpUtility.UrlDecode(searchQuery))
                    );
                    break;
                case STREAM_DASHBOARD:
                    dashboardDataStream.Subscribe(id, default);
                    break;
            }
        }

        private void UnSubscribe(int id, Span<string> args)
        {
            streams[args[0]].UnSubscribe(id);
        }

        private void UpdateComponentData(int _, Span<string> args)
        {
            var groupId = SveltoUtils.CreateExclusiveGroupStruct(uint.Parse(args[0]));
            var entityId = uint.Parse(args[1]);
            var componentName = args[2];

            // componentName is a fully qualified assembly name, so we should be able to find it here
            Type componentType = Type.GetType(componentName);

            // Knowing the type we can deserialize the json object
            var componentData = JsonConvert.DeserializeObject(args[3], componentType);
            if (componentData == null)
                return;

            // Get component data array and index of the component
            var queryEntitiesAndIndexParams = new object?[] { entitiesDB, entityId, groupId, null };
            var componentDataArray = typeof(EntityNativeDBExtensions).GetMethod(
                "QueryEntitiesAndIndex",
                new[]
                {
                    typeof(EntitiesDB),
                    typeof(uint),
                    typeof(ExclusiveGroupStruct),
                    typeof(uint).MakeByRefType()
                }
            )?.MakeGenericMethod(componentType)?.Invoke(entitiesDB, queryEntitiesAndIndexParams);
            var indexObject = queryEntitiesAndIndexParams[3];
            if (indexObject == null)
                return;

            // Get the underlying native array
            var nativeArrayObject = typeof(NB<>)
                .MakeGenericType(new Type[] { componentType })
                .GetMethod("ToNativeArray")?.Invoke(componentDataArray, new object?[] { null });
            if (nativeArrayObject is null)
                return;

            // Write the component data, into the native component array
            ReflectionUtil.WriteToUnsafeMemory(
                (IntPtr)nativeArrayObject,
                componentType,
                componentData,
                (uint)indexObject
            );
        }

        private void GetEngines(int id, Span<string> args)
        {
            ws.Send(
                id,
                SocketUtil.Serialize(
                    new Envelope<string, List<QueryInvocation>>("engines", queryInvocation)
                )
            );
        }
    }
}
