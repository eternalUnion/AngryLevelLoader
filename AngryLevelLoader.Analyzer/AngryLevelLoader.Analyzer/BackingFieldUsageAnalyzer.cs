using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AngryLevelLoader.Analyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class BackingFieldUsageAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "AL001";

		private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AL001Title), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AL001Message), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AL001Description), Resources.ResourceManager, typeof(Resources));
		private const string Category = "Design";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeBackingFieldUsage, SyntaxKind.IdentifierName);
		}

		private void AnalyzeBackingFieldUsage(SyntaxNodeAnalysisContext context)
		{
			string ToPropertyName(string fieldName)
			{
				if (!fieldName.StartsWith("_") || fieldName.Length < 2)
					return fieldName;

				var name = fieldName.Substring(1);
				return char.ToUpper(name[0]) + name.Substring(1);
			}

			var memberAccess = (IdentifierNameSyntax)context.Node;
			var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;

			if (!(symbol is IFieldSymbol fieldSymbol))
				return;

			if (!fieldSymbol.Name.StartsWith("_") || fieldSymbol.Name.Length < 2)
				return;

			string expectedPropertyName = ToPropertyName(fieldSymbol.Name);
			var propertyMember = context.ContainingSymbol.ContainingType.GetMembers()
				.OfType<IPropertySymbol>()
				.FirstOrDefault(p => p.Name == expectedPropertyName);

			if (propertyMember == null)
				return;

			// Find containing property
			var containingProperty = memberAccess.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

			if (containingProperty == null)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), fieldSymbol.Name));
				return;
			}

			var propertySymbol = context.SemanticModel.GetDeclaredSymbol(containingProperty);
			if (propertySymbol.Name != propertyMember.Name)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), fieldSymbol.Name));
				return;
			}
		}
	}
}
