using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Svelto.Common;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Hybrid;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Inspector
{
    public class InspectorService : IInspectorRoutes, IInspectorService
    {
        private record class Entity(uint EntityId);

        private record GrouppedEntities(uint Id, string Name, List<Entity> Entities);

        private record Group(string Name, List<Component> Components);

        private record Component(string Name);

        private record EntityComponentData(string Name, object Data);

        private record EntityComponents(List<EntityComponentData> Components);

        private record Engine(string Name);

        private record EngineWithQueries(string Name, List<List<string>> Components);

        private readonly EnginesRoot enginesRoot;
        private readonly EntitiesDB entitiesDb;
        private readonly List<QueryInvocation> queryInvocations;
        private readonly FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<RefWrapperType, ITypeSafeDictionary>
        > groupEntityComponentsDB;

        private List<Group> Groups;
        private List<GrouppedEntities> Entities;
        private List<Engine> Engines;

        private T GetPrivateField<T>(string name) where T : class
        {
            var field = typeof(EnginesRoot).GetField(
                name,
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (field == null)
                throw new ArgumentException($"{name} is not a valid private field of enginesRoot");
            var instanceField = field.GetValue(enginesRoot);
            if (instanceField is not T tmp)
                throw new ArgumentException(
                    $"{name} is not of type {typeof(T)}, it's {field.GetType()}"
                );
            return tmp;
        }

        public InspectorService(EnginesRoot enginesRoot, List<QueryInvocation> queryInvocations)
        {
            this.queryInvocations = queryInvocations;
            this.enginesRoot = enginesRoot;
            this.entitiesDb = GetPrivateField<EntitiesDB>("_entitiesDB");
            this.groupEntityComponentsDB = GetPrivateField<
                FasterDictionary<
                    ExclusiveGroupStruct,
                    FasterDictionary<RefWrapperType, ITypeSafeDictionary>
                >
            >("_groupEntityComponentsDB");
            Groups = new();
            Entities = new();
            Engines = new();
        }

        public void UpdateFromMainThread()
        {
            var _enginesSet = GetPrivateField<FasterList<IEngine>>("_enginesSet");
            List<Engine> debugEngines = new();
            foreach (var engine in _enginesSet)
            {
                debugEngines.Add(new(engine.GetType().Name));
            }

            var _entitiesDB = GetPrivateField<EntitiesDB>("_entitiesDB");
            List<Group> debugGroups = new();
            List<GrouppedEntities> debugGroupsForEntities = new();

            var _groupEntityComponentsDB = GetPrivateField<
                FasterDictionary<
                    ExclusiveGroupStruct,
                    FasterDictionary<RefWrapperType, ITypeSafeDictionary>
                >
            >("_groupEntityComponentsDB");
            foreach (var item in _groupEntityComponentsDB)
            {
                var debugComponents = new List<Component>();
                var debugEntitiesForGroup = new List<Entity>();
                var group = item.key;
                var components = item.value;
                foreach (var asd in components)
                {
                    Type component = asd.key;
                    var typeSafeDictionary = asd.value;
                    debugComponents.Add(new(component.Name.ToString()));
                    if (!debugEntitiesForGroup.Any())
                    {
                        typeSafeDictionary.KeysEvaluator(
                            x =>
                            {
                                debugEntitiesForGroup.Add(new(x));
                            }
                        );
                    }
                }
                var debugGroup = new Group(new(group.ToString()), debugComponents);
                debugGroups.Add(debugGroup);
                debugGroupsForEntities.Add(
                    new(group.ToIDAndBitmask(), group.ToString(), debugEntitiesForGroup)
                );
            }
            Groups = debugGroups;
            Entities = debugGroupsForEntities;
            Engines = debugEngines;
        }

        public async Task<IResult> GetGroups()
        {
            return await Task.FromResult(Results.Ok(Groups));
        }

        public async Task<IResult> GetEntities()
        {
            return await Task.FromResult(Results.Ok(Entities));
        }

        public async Task<IResult> GetEngines()
        {
            var groupped = queryInvocations
                .GroupBy(x => x.ClassName)
                .ToDictionary(x => x.Key, x => x.ToList());
            var engines = Engines.Select(
                engine =>
                {
                    if (groupped.ContainsKey(engine.Name))
                    {
                        var components = groupped[engine.Name].Distinct();
                        return new EngineWithQueries(
                            engine.Name,
                            components.Select(x => x.Components).ToList()
                        );
                    }
                    else
                    {
                        return new EngineWithQueries(engine.Name, new List<List<string>>());
                    }
                }
            );

            return await Task.FromResult(Results.Ok(engines));
        }

        public async Task<IResult> GetEntity(uint groupId, uint entityId)
        {
            var ctorInfo = typeof(ExclusiveGroupStruct)?.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(uint) },
                null
            );

            var obj = ctorInfo?.Invoke(new[] { (object)groupId });

            if (obj is not null)
            {
                var group = (ExclusiveGroupStruct)obj;
                MethodInfo? queryNativeComponent = typeof(EntityNativeDBExtensions).GetMethod(
                    "QueryEntity",
                    new[] { typeof(EntitiesDB), typeof(uint), typeof(ExclusiveGroupStruct) }
                );
                List<EntityComponentData> components = new();
                foreach (Type componentType in groupEntityComponentsDB[group].keys)
                {
                    if (
                        componentType.Name == "EntityInfoComponent"
                        || componentType.Name == "EntityReferenceComponent"
                        || componentType.Name == "EGIDComponent"
                    )
                        continue;
                    MethodInfo? queryMethod;
                    if (componentType.IsAssignableTo(typeof(IEntityViewComponent)))
                    {
                        // TODO: not sure about managed components, could be dangerous to serialize
                        continue;
                    }
                    else
                        queryMethod = queryNativeComponent;
                    MethodInfo? generic = queryMethod?.MakeGenericMethod(componentType);
                    if (generic is null)
                        continue;
                    var componentData = generic?.Invoke(
                        entitiesDb,
                        new object[] { entitiesDb, entityId, group }
                    );
                    if (componentData is null)
                        continue;
                    components.Add(new(componentType.Name, componentData));
                }

                return await Task.FromResult(Results.Ok(new EntityComponents(components)));
            }

            return await Task.FromResult(Results.NotFound());
        }

        private void SetComponent(
            EntitiesDB db,
            IntPtr array,
            Type t,
            object components,
            object component,
            uint index
        )
        {
            unsafe
            {
                MethodInfo? sizeOfMethod = typeof(MemoryUtilities).GetMethod(
                    "SizeOf"
                )?.MakeGenericMethod(new Type[] { t });

                var sizeB = sizeOfMethod?.Invoke(null, null);
                if (sizeB == null)
                    return;
                var size = (int)(sizeB);

                MethodInfo? copyMethod = typeof(Unsafe)
                    .GetMethods()
                    .Single(
                        x =>
                            x.Name == "Copy"
                            && x.GetParameters().First().ParameterType == typeof(void*)
                    )?.MakeGenericMethod(new Type[] { t });

                //Marshal.StructureToPtr(component, IntPtr.Add(array, (int)index), )
                copyMethod?.Invoke(
                    null,
                    new object[] { IntPtr.Add(array, (int)(size * index)), component }
                );
            }
        }

        public async Task<IResult> SetComponent(
            uint groupId,
            uint entityId,
            string componentName,
            JsonObject data
        )
        {
            var ctorInfo = typeof(ExclusiveGroupStruct)?.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(uint) },
                null
            );

            var obj = ctorInfo?.Invoke(new[] { (object)groupId });

            if (obj is not null)
            {
                var group = (ExclusiveGroupStruct)obj;
                MethodInfo? queryNativeComponent = typeof(EntityNativeDBExtensions).GetMethod(
                    "QueryEntitiesAndIndex",
                    new[]
                    {
                        typeof(EntitiesDB),
                        typeof(uint),
                        typeof(ExclusiveGroupStruct),
                        typeof(uint).MakeByRefType()
                    }
                );
                List<EntityComponentData> components = new();
                foreach (Type componentType in groupEntityComponentsDB[group].keys)
                {
                    if (componentType.Name != componentName)
                        continue;

                    MethodInfo? queryMethod;
                    if (componentType.IsAssignableTo(typeof(IEntityViewComponent)))
                    {
                        // TODO: not sure about managed components, could be dangerous to serialize
                        continue;
                    }
                    else
                        queryMethod = queryNativeComponent;
                    MethodInfo? generic = queryMethod?.MakeGenericMethod(componentType);
                    if (generic is null)
                        continue;
                    var queryParams = new object[] { entitiesDb, entityId, group, null };
                    var componentDatas = generic?.Invoke(entitiesDb, queryParams);
                    var index = (uint)queryParams[3];
                    var d1 = typeof(NB<>);
                    Type[] typeArgs = { componentType };
                    var NBT = d1.MakeGenericType(typeArgs);
                    var arrayB = NBT.GetMethod("ToNativeArray")?.Invoke(
                        componentDatas,
                        new object[] { null }
                    );
                    var nativeArrayParams = new object[] { null };
                    if (arrayB is null)
                        break;
                    var array = (IntPtr)(arrayB);

                    var componentData = JsonSerializer.Deserialize(
                        data,
                        componentType,
                        new JsonSerializerOptions()
                        {
                            IncludeFields = true,
                            PropertyNameCaseInsensitive = true
                        }
                    );
                    SetComponent(
                        entitiesDb,
                        array,
                        componentType,
                        componentDatas,
                        componentData,
                        index
                    );

                    // PropertyInfo indexProperty = NBT
                    // .GetProperties()
                    // .Single(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(uint));

                    // indexProperty.SetValue(componentDatas,componentData, new object[] { index });
                    break;
                }

                return await Task.FromResult(Results.Ok());
            }

            return await Task.FromResult(Results.NotFound());
        }
    }
}
