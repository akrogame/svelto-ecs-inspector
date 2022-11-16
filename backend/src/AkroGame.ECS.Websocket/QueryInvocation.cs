using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AkroGame.ECS.Websocket
{
    public struct QueryInvocation
    {
        public string ClassName { get; }
        public List<string> Components { get; }

        public QueryInvocation(string className, List<string> components)
        {
            ClassName = className;
            Components = components;
        }
    }

    public class EnginesMetadata
    {
        public List<QueryInvocation> QueryInvocations { get; }

        public EnginesMetadata(List<QueryInvocation> queryInvocations)
        {
            QueryInvocations = queryInvocations;
        }
    }

    public static class EnginesMetadataFactory
    {
        public static EnginesMetadata GetMeta()
        {
            var metas = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes().Where(IsEngineType))
                .Distinct();

            return metas.Aggregate(
                new EnginesMetadata(new List<QueryInvocation>()),
                (acc, meta) =>
                {
                    var field = meta.GetField(
                        "QueryInvocations",
                        BindingFlags.Public | BindingFlags.Static
                    );
                    if (field?.GetValue(null) is Dictionary<string, List<string>> queryInvocations)
                        foreach (var i in queryInvocations)
                            acc.QueryInvocations.Add(new QueryInvocation(i.Key, i.Value));
                    return acc;
                }
            );
        }

        private static bool IsEngineType(Type t) =>
            t.AssemblyQualifiedName?.StartsWith("Svelto.ECS.Meta")
            ?? false && t.Name == "EnginesMetadata" && t.IsClass && !t.IsAbstract;
    }
}
