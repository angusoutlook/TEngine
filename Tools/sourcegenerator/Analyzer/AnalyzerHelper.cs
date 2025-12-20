#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Analyzer;

public static class AnalyzerHelper
{
	public static T? GetFirstChild<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
	{
		foreach (SyntaxNode item in syntaxNode.ChildNodes())
		{
			if (((object)item).GetType() == typeof(T))
			{
				return (T)(object)((item is T) ? item : null);
			}
		}
		return default(T);
	}

	public static SyntaxNode? GetFirstChild(this SyntaxNode syntaxNode)
	{
		IEnumerable<SyntaxNode> source = syntaxNode.ChildNodes();
		if (source.Count() > 0)
		{
			return source.First();
		}
		return null;
	}

	public static T? GetLastChild<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
	{
		foreach (SyntaxNode item in syntaxNode.ChildNodes().Reverse())
		{
			if (((object)item).GetType() == typeof(T))
			{
				return (T)(object)((item is T) ? item : null);
			}
		}
		return default(T);
	}

	public static ClassDeclarationSyntax? GetParentClassDeclaration(this SyntaxNode syntaxNode)
	{
		for (SyntaxNode parent = syntaxNode.Parent; parent != null; parent = parent.Parent)
		{
			ClassDeclarationSyntax val = (ClassDeclarationSyntax)(object)((parent is ClassDeclarationSyntax) ? parent : null);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	public static bool HasAttribute(this ITypeSymbol typeSymbol, string AttributeName)
	{
		ImmutableArray<AttributeData>.Enumerator enumerator = ((ISymbol)typeSymbol).GetAttributes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			AttributeData current = enumerator.Current;
			if (((object)current.AttributeClass)?.ToString() == AttributeName)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasAttributeInTypeAndBaseTyes(this ITypeSymbol typeSymbol, string AttributeName)
	{
		if (typeSymbol.HasAttribute(AttributeName))
		{
			return true;
		}
		foreach (ITypeSymbol item in typeSymbol.BaseTypes())
		{
			if (item.HasAttribute(AttributeName))
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<ITypeSymbol> BaseTypes(this ITypeSymbol typeSymbol)
	{
		for (ITypeSymbol baseType = (ITypeSymbol)(object)typeSymbol.BaseType; baseType != null; baseType = (ITypeSymbol)(object)baseType.BaseType)
		{
			yield return baseType;
		}
	}

	public static bool HasBaseAttribute(this INamedTypeSymbol namedTypeSymbol, string AttributeName)
	{
		ImmutableArray<AttributeData>.Enumerator enumerator = ((ISymbol)namedTypeSymbol).GetAttributes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			AttributeData current = enumerator.Current;
			INamedTypeSymbol attributeClass = current.AttributeClass;
			for (INamedTypeSymbol val = ((attributeClass != null) ? ((ITypeSymbol)attributeClass).BaseType : null); val != null; val = ((ITypeSymbol)val).BaseType)
			{
				if (((object)val).ToString() == AttributeName)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static AttributeData? GetFirstAttribute(this INamedTypeSymbol namedTypeSymbol, string AttributeName)
	{
		ImmutableArray<AttributeData>.Enumerator enumerator = ((ISymbol)namedTypeSymbol).GetAttributes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			AttributeData current = enumerator.Current;
			if (((object)current.AttributeClass)?.ToString() == AttributeName)
			{
				return current;
			}
		}
		return null;
	}

	public static bool HasInterface(this INamedTypeSymbol namedTypeSymbol, string InterfaceName)
	{
		ImmutableArray<INamedTypeSymbol>.Enumerator enumerator = ((ITypeSymbol)namedTypeSymbol).AllInterfaces.GetEnumerator();
		while (enumerator.MoveNext())
		{
			INamedTypeSymbol current = enumerator.Current;
			if (current.IsInterface(InterfaceName))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsInterface(this INamedTypeSymbol namedTypeSymbol, string InterfaceName)
	{
		return namedTypeSymbol.GetNameSpace() + "." + ((ISymbol)namedTypeSymbol).Name == InterfaceName;
	}

	public static bool IsAssemblyNeedAnalyze(string? assemblyName, params string[] analyzeAssemblyNames)
	{
		if (assemblyName == null)
		{
			return false;
		}
		foreach (string text in analyzeAssemblyNames)
		{
			if (assemblyName == text)
			{
				return true;
			}
		}
		return false;
	}

	public static ITypeSymbol? GetMemberAccessSyntaxParentType(this MemberAccessExpressionSyntax memberAccessExpressionSyntax, SemanticModel semanticModel)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		SyntaxNode firstChild = ((SyntaxNode)(object)memberAccessExpressionSyntax).GetFirstChild();
		if (firstChild == null)
		{
			return null;
		}
		SymbolInfo symbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, firstChild, default(CancellationToken));
		ISymbol symbol = symbolInfo.Symbol;
		if (symbol == null)
		{
			return null;
		}
		ILocalSymbol val = (ILocalSymbol)(object)((symbol is ILocalSymbol) ? symbol : null);
		if (val != null)
		{
			return val.Type;
		}
		IParameterSymbol val2 = (IParameterSymbol)(object)((symbol is IParameterSymbol) ? symbol : null);
		if (val2 != null)
		{
			return val2.Type;
		}
		IPropertySymbol val3 = (IPropertySymbol)(object)((symbol is IPropertySymbol) ? symbol : null);
		if (val3 != null)
		{
			return val3.Type;
		}
		IMethodSymbol val4 = (IMethodSymbol)(object)((symbol is IMethodSymbol) ? symbol : null);
		if (val4 != null)
		{
			return val4.ReturnType;
		}
		IFieldSymbol val5 = (IFieldSymbol)(object)((symbol is IFieldSymbol) ? symbol : null);
		if (val5 != null)
		{
			return val5.Type;
		}
		IEventSymbol val6 = (IEventSymbol)(object)((symbol is IEventSymbol) ? symbol : null);
		if (val6 != null)
		{
			return val6.Type;
		}
		return null;
	}

	public static T? GetNeareastAncestor<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
	{
		foreach (SyntaxNode item in syntaxNode.Ancestors(true))
		{
			T val = (T)(object)((item is T) ? item : null);
			if (val != null)
			{
				return val;
			}
		}
		return default(T);
	}

	public static bool HasParameterType(this IMethodSymbol methodSymbol, string parameterType, out IParameterSymbol? cencelTokenSymbol)
	{
		ImmutableArray<IParameterSymbol>.Enumerator enumerator = methodSymbol.Parameters.GetEnumerator();
		while (enumerator.MoveNext())
		{
			IParameterSymbol current = enumerator.Current;
			if (((object)current.Type).ToString() == parameterType)
			{
				cencelTokenSymbol = current;
				return true;
			}
		}
		cencelTokenSymbol = null;
		return false;
	}

	public static IEnumerable<T> DescendantNodes<T>(this SyntaxNode syntaxNode) where T : SyntaxNode
	{
		foreach (SyntaxNode descendantNode in syntaxNode.DescendantNodes((Func<SyntaxNode, bool>)null, false))
		{
			T node = (T)(object)((descendantNode is T) ? descendantNode : null);
			if (node != null)
			{
				yield return node;
			}
		}
	}

	public static SyntaxNode? PreviousNode(this SyntaxNode syntaxNode)
	{
		if (syntaxNode.Parent == null)
		{
			return null;
		}
		int num = 0;
		foreach (SyntaxNode item in syntaxNode.Parent.ChildNodes())
		{
			if (item == syntaxNode)
			{
				break;
			}
			num++;
		}
		if (num == 0)
		{
			return null;
		}
		return syntaxNode.Parent.ChildNodes().ElementAt(num - 1);
	}

	public static SyntaxNode? NextNode(this SyntaxNode syntaxNode)
	{
		if (syntaxNode.Parent == null)
		{
			return null;
		}
		int num = 0;
		foreach (SyntaxNode item in syntaxNode.Parent.ChildNodes())
		{
			if (item == syntaxNode)
			{
				break;
			}
			num++;
		}
		if (num == syntaxNode.Parent.ChildNodes().Count() - 1)
		{
			return null;
		}
		return syntaxNode.Parent.ChildNodes().ElementAt(num + 1);
	}

	public static BasicBlock? GetAwaitStatementControlFlowBlock(StatementSyntax statementSyntax, AwaitExpressionSyntax awaitExpressionSyntax, SemanticModel semanticModel)
	{
		if (statementSyntax.IsKind(SyntaxKind.Block))
		{
			return null;
		}
		MethodDeclarationSyntax neareastAncestor = ((SyntaxNode)(object)statementSyntax).GetNeareastAncestor<MethodDeclarationSyntax>();
		if (neareastAncestor == null)
		{
			return null;
		}
		ControlFlowGraph val = ControlFlowGraph.Create((SyntaxNode)(object)neareastAncestor, semanticModel, default(CancellationToken));
		if (val == null)
		{
			return null;
		}
		if (statementSyntax is LocalDeclarationStatementSyntax)
		{
			return null;
		}
		return val.Blocks.FirstOrDefault((BasicBlock x) => x.Operations.Any((IOperation y) => y.Syntax.Contains((SyntaxNode)(object)statementSyntax)));
	}

	public static bool IsPartial(this ClassDeclarationSyntax classDeclaration)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		SyntaxTokenList modifiers = ((MemberDeclarationSyntax)classDeclaration).Modifiers;
		var enumerator = modifiers.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SyntaxToken current = enumerator.Current;
			if (current.IsKind(SyntaxKind.PartialKeyword))
			{
				return true;
			}
		}
		return false;
	}

	public static string? GetNameSpace(this INamedTypeSymbol namedTypeSymbol)
	{
		INamespaceSymbol containingNamespace = ((ISymbol)namedTypeSymbol).ContainingNamespace;
		string text = ((containingNamespace != null) ? ((ISymbol)containingNamespace).Name : null);
		while (((containingNamespace != null) ? ((ISymbol)containingNamespace).ContainingNamespace : null) != null)
		{
			containingNamespace = ((ISymbol)containingNamespace).ContainingNamespace;
			if (string.IsNullOrEmpty(((ISymbol)containingNamespace).Name))
			{
				break;
			}
			text = ((ISymbol)containingNamespace).Name + "." + text;
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		return text;
	}

	public static bool IsSemanticModelNeedAnalyze(SemanticModel semanticModel, params string[] filePaths)
	{
		foreach (string value in filePaths)
		{
			if (semanticModel.SyntaxTree.FilePath.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasMethodWithParams(this INamedTypeSymbol namedTypeSymbol, string methodName, params ITypeSymbol[] typeSymbols)
	{
		ImmutableArray<ISymbol>.Enumerator enumerator = ((INamespaceOrTypeSymbol)namedTypeSymbol).GetMembers().GetEnumerator();
		while (enumerator.MoveNext())
		{
			ISymbol current = enumerator.Current;
			IMethodSymbol val = (IMethodSymbol)(object)((current is IMethodSymbol) ? current : null);
			if (val == null || ((ISymbol)val).Name != methodName || typeSymbols.Length != val.Parameters.Length)
			{
				continue;
			}
			if (typeSymbols.Length == 0)
			{
				return true;
			}
			bool flag = true;
			for (int i = 0; i < typeSymbols.Length; i++)
			{
				if (((object)typeSymbols[i]).ToString() != ((object)val.Parameters[i].Type).ToString())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			return true;
		}
		return false;
	}

	public static bool HasMethodWithParams(this INamedTypeSymbol namedTypeSymbol, string methodName, params string[] typeSymbols)
	{
		ImmutableArray<ISymbol>.Enumerator enumerator = ((INamespaceOrTypeSymbol)namedTypeSymbol).GetMembers().GetEnumerator();
		while (enumerator.MoveNext())
		{
			ISymbol current = enumerator.Current;
			IMethodSymbol val = (IMethodSymbol)(object)((current is IMethodSymbol) ? current : null);
			if (val == null || ((ISymbol)val).Name != methodName || typeSymbols.Length != val.Parameters.Length)
			{
				continue;
			}
			if (typeSymbols.Length == 0)
			{
				return true;
			}
			bool flag = true;
			for (int i = 0; i < typeSymbols.Length; i++)
			{
				if (typeSymbols[i] != ((object)val.Parameters[i].Type).ToString())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			return true;
		}
		return false;
	}

	public static bool HasAttribute(this IMethodSymbol methodSymbol, string AttributeName)
	{
		ImmutableArray<AttributeData>.Enumerator enumerator = ((ISymbol)methodSymbol).GetAttributes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			AttributeData current = enumerator.Current;
			if (((current == null) ? null : ((object)current.AttributeClass)?.ToString()) == AttributeName)
			{
				return true;
			}
		}
		return false;
	}
}
