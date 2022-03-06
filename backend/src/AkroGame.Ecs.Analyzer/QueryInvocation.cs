using System.Collections.Generic;

namespace AkroGame.ECS.Analyzer
{
    internal struct QueryInvocation
    {
        public string ClassName { get; }
        public List<string> Components { get; }

        public QueryInvocation(string className, List<string> components)
        {
            ClassName = className;
            Components = components;
        }
    }
}