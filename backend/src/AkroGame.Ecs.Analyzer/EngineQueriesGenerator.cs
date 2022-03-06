using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Text;

namespace AkroGame.ECS.Analyzer
{
    [Generator]
    public class EngineQueriesGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(
                $"EngineNames.g.cs",
                GenerateEngineNames(FindQueryInvocations(context))
            );
        }

        private SourceText GenerateEngineNames(List<QueryInvocation> methods)
        {
            var ns = "AkroGame.ECS.Inspector";
            var classT = "EngineNames";
            var listT = "List";
            var stringT = "string";
            return SourceText.From(
                CSharpSyntaxTree
                    .ParseText(
                        $@"
using System.Collections.Generic;

namespace {ns}
{{
    public static class {classT}
    {{
        public record struct QueryInvocation({stringT} ClassName, {listT}<{stringT}> Components);
        public static {listT}<QueryInvocation> QueryInvocations = new {listT}<QueryInvocation>() {{
            {string.Join(",\n", methods.Select((invocation, i) => $@"
            new QueryInvocation(""{invocation.ClassName}"", new {listT}<{stringT}>() {{
                {string.Join(",", invocation.Components.Select(x => $@"""{x}"""))}
            }})
            
            "))}
        }};
    }}
}}".ToString()
                    )
                    .GetRoot()
                    .NormalizeWhitespace()
                    .ToFullString(),
                Encoding.UTF8
            );
        }

        private bool IsQueryCall(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var symbol = semanticModel?.GetSymbolInfo(invocation).Symbol;
            if (symbol == null)
                return false;
            else
                return symbol.Name == "QueryEntities" || symbol.Name == "QueryEntity";
        }

        private List<string> ExtractGenericParameters(InvocationExpressionSyntax invocation)
        {
            var genericArguments = invocation
                .DescendantNodes()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault();

            var components = new List<string>();
            if (genericArguments != null)
                foreach (var arg in genericArguments.TypeArgumentList.Arguments)
                    components.Add(arg.ToString());
            return components;
        }

        private List<QueryInvocation> FindQueryInvocations(GeneratorExecutionContext context)
        {
            var allTypes = context.Compilation.SyntaxTrees.SelectMany(
                st => st.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            );

            return allTypes
                .SelectMany(
                    t =>
                    {
                        var semanticModel = context.Compilation.GetSemanticModel(t.SyntaxTree);
                        return t.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .Where(_ => IsQueryCall(semanticModel, _))
                            .Select(
                                _ => new QueryInvocation(t.Identifier.ToString(), ExtractGenericParameters(_))
                            );
                    }
                )
                .ToList();
        }
    }
}
