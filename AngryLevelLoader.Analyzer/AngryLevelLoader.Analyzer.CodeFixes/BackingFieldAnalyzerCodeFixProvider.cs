using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BackingFieldAnalyzerCodeFixProvider)), Shared]
	public class BackingFieldAnalyzerCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(BackingFieldUsageAnalyzer.DiagnosticId); }
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
					title: CodeFixResources.AL001Title,
					createChangedDocument: c => UsePropertyAsync(context.Document, node, c),
					equivalenceKey: nameof(CodeFixResources.AL001Title)),
				diagnostic);
		}

		private async Task<Document> UsePropertyAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			string ToPropertyName(string fieldName)
			{
				if (!fieldName.StartsWith("_") || fieldName.Length < 2)
					return fieldName;

				var name = fieldName.Substring(1);
				return char.ToUpper(name[0]) + name.Substring(1);
			}

			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			if (!(node is IdentifierNameSyntax))
			{
				node = node.ChildNodes().Where(n => n is IdentifierNameSyntax).FirstOrDefault();
				if (!(node is IdentifierNameSyntax))
					return document;
			}

			var symbol = semanticModel.GetSymbolInfo(node).Symbol as IFieldSymbol;
			if (symbol == null)
				return document;

			// Convert backing field name to property name
			var propertyName = ToPropertyName(symbol.Name);

			// Look for matching property in containing type
			var containingType = symbol.ContainingType;
			var property = containingType.GetMembers()
				.OfType<IPropertySymbol>()
				.FirstOrDefault(p => p.Name == propertyName);

			if (property == null)
				return document; // No such property exists

			// Replace backing field with property name
			var newExpression = SyntaxFactory.IdentifierName(property.Name)
				.WithTriviaFrom(node);

			var newRoot = root.ReplaceNode(node, newExpression);

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
