using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AngryLevelLoader.Analyzer
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskContinueWithAnalyzer)), Shared]
	public class TaskContinueWithAnalyzerCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(TaskContinueWithAnalyzer.DiagnosticId); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var diagnostic = context.Diagnostics.First();
			var node = root.FindNode(diagnostic.Location.SourceSpan);

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: CodeFixResources.AL002Title,
					createChangedDocument: c => UseTaskScheduler(context.Document, node, c),
					equivalenceKey: nameof(CodeFixResources.AL002Title)),
				diagnostic);
		}

		private async Task<Document> UseTaskScheduler(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

			var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
			if (invocation == null)
				return document;

			var args = invocation.ArgumentList.Arguments;
			var symbolInfo = editor.SemanticModel.GetSymbolInfo(invocation);
			var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
			var parameters = methodSymbol.Parameters;

			var schedulerExpr = SyntaxFactory.ParseExpression($"{nameof(TaskScheduler)}.{nameof(TaskScheduler.FromCurrentSynchronizationContext)}()");

			if (args.Count < 2)
			{
				var newArgs = invocation.ArgumentList.AddArguments(SyntaxFactory.Argument(schedulerExpr));
				editor.ReplaceNode(invocation.ArgumentList, newArgs);
			}
			else
			{
				var lastArg = args.Last().Expression;
				var lastArgType = parameters.Last().Type;

				if (lastArgType.Name == nameof(TaskScheduler))
				{
					// Replace last argument
					var newArg = SyntaxFactory.Argument(schedulerExpr);
					var newArgs = invocation.ArgumentList.WithArguments(args.Replace(args.Last(), newArg));
					editor.ReplaceNode(invocation.ArgumentList, newArgs);
				}
				else
				{
					// Overwrite last argument
					var newLastArg = SyntaxFactory.Argument(schedulerExpr);

					if (lastArgType.Name == nameof(CancellationToken))
						args = args.Add(SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"{nameof(TaskContinuationOptions)}.{nameof(TaskContinuationOptions.None)}")));
					args = args.Add(newLastArg);

					var newArgs = invocation.ArgumentList.WithArguments(args);
					editor.ReplaceNode(invocation.ArgumentList, newArgs);
				}
			}

			return editor.GetChangedDocument();
		}
	}
}
