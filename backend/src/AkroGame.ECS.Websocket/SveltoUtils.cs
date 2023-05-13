using System.Reflection;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AkroGame.ECS.Websocket
{
    public static class SveltoUtils
    {
        public static ExclusiveGroupStruct CreateExclusiveGroupStruct(uint groupId)
        {
            var ctorInfo = typeof(ExclusiveGroupStruct)?.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(uint) },
                null
            );

            var obj = ctorInfo?.Invoke(new object[] { groupId });

            if (obj == null)
                throw new System.Exception(
                    $"Could not construct ExclusiveGroupStruct with groupId {groupId}"
                );

            return (ExclusiveGroupStruct)obj;
        }

        public static EntitiesDB GetEntitiesDB(this EnginesRoot enginesRoot) =>
            enginesRoot.GetPrivateField<EnginesRoot, EntitiesDB>("_entitiesDB");

        public static FasterDictionary<
            ExclusiveGroupStruct,
            FasterDictionary<ComponentID, ITypeSafeDictionary>
        > GetGroupEntityComponentsDB(this EnginesRoot enginesRoot) =>
            enginesRoot.GetPrivateField<
                EnginesRoot,
                FasterDictionary<
                    ExclusiveGroupStruct,
                    FasterDictionary<ComponentID, ITypeSafeDictionary>
                >
            >("_groupEntityComponentsDB");
    }
}
