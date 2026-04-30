using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AngryLevelLoader.Analyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TaskContinueWithAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "AL002";

		private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AL002Title), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AL002Message), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AL002Description), Resources.ResourceManager, typeof(Resources));
		private const string Category = "Design";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(ContinueWithAnalyzer, SyntaxKind.InvocationExpression);
		}

		private void ContinueWithAnalyzer(SyntaxNodeAnalysisContext context)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			
			if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
				return;

			if (memberAccess.Name.Identifier.Text != "ContinueWith")
				return;

			var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
			var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

			if (methodSymbol == null)
				return;

			if (methodSymbol.ContainingType == null || methodSymbol.ContainingType.Name != nameof(Task))
				return;

			if (methodSymbol.ContainingNamespace == null || methodSymbol.ContainingNamespace.Name != nameof(System.Threading.Tasks))
				return;

			var args = invocation.ArgumentList.Arguments;

			if (args.Count < 2)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
				return;
			}
			
			var lastArg = args.Last().Expression;

			var typeInfo = methodSymbol.Parameters.Last();
			if (typeInfo.Type == null || typeInfo.Type.Name != nameof(TaskScheduler))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
				return;
			}

			if (lastArg is InvocationExpressionSyntax invocationExpr)
			{
				if (invocationExpr.Expression is MemberAccessExpressionSyntax ma &&
					ma.Name.Identifier.Text == nameof(TaskScheduler.FromCurrentSynchronizationContext))
				{
					return;
				}
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, lastArg.GetLocation()));
		}
	}
}
