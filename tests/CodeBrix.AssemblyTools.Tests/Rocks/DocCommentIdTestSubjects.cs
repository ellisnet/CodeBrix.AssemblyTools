// DECIDED (not a TODO): the types in this file deliberately live in a
// top-level `N` namespace (just the single letter) -- same as upstream
// Mono.Cecil 0.11.5. Keep it that way. Reasons:
//
//   1. Drop-in parity: any consumer that expects these subject types to
//      resolve in namespace `N` when using Mono.Cecil will find them in
//      namespace `N` when using CodeBrix.AssemblyTools too.
//   2. The doc-comment-ID strings asserted by DocCommentIdTests
//      (e.g. "T:N.X", "M:N.X.#ctor") bake the namespace name into the
//      expected literals. Renaming the namespace would require rewriting
//      roughly 40 expected strings and buy nothing.
//   3. These types live in the test assembly only (CodeBrix.AssemblyTools.Tests
//      is not distributed as a NuGet package), so namespace `N` is not visible
//      to consumers of the library under any circumstances.
//
// This file is ported verbatim from Mono.Cecil 0.11.5
// rocks/Test/Mono.Cecil.Tests/DocCommentIdTests.cs (lines 9-184, the two
// `namespace N { ... }` blocks merged into one file-scoped namespace).

using System;
using System.Collections.Generic;

namespace N; //was previously: N;

/// <summary>
/// ID string generated is "T:N.X".
/// </summary>
public class X : IX<KVP<string, int>> {
	/// <summary>
	/// ID string generated is "M:N.X.#ctor".
	/// </summary>
	public X () { }


	/// <summary>
	/// ID string generated is "M:N.X.#ctor(System.Int32)".
	/// </summary>
	/// <param name="i">Describe parameter.</param>
	public X (int i) { }


	/// <summary>
	/// ID string generated is "F:N.X.q".
	/// </summary>
	public string q;


	/// <summary>
	/// ID string generated is "F:N.X.PI".
	/// </summary>
	public const double PI = 3.14;


	/// <summary>
	/// ID string generated is "M:N.X.f".
	/// </summary>
	public int f () { return 1; }


	/// <summary>
	/// ID string generated is "M:N.X.bb(System.String,System.Int32@)".
	/// </summary>
	public int bb (string s, ref int y) { return 1; }


	/// <summary>
	/// ID string generated is "M:N.X.gg(System.Int16[],System.Int32[0:,0:])".
	/// </summary>
	public int gg (short [] array1, int [,] array) { return 0; }


	/// <summary>
	/// ID string generated is "M:N.X.op_Addition(N.X,N.X)".
	/// </summary>
	public static X operator + (X x, X xx) { return x; }


	/// <summary>
	/// ID string generated is "P:N.X.prop".
	/// </summary>
	public int prop { get { return 1; } set { } }


	/// <summary>
	/// ID string generated is "E:N.X.d".
	/// </summary>
#pragma warning disable 67
	public event D d;
#pragma warning restore 67


	/// <summary>
	/// ID string generated is "P:N.X.Item(System.String)".
	/// </summary>
	public int this [string s] { get { return 1; } }


	/// <summary>
	/// ID string generated is "T:N.X.Nested".
	/// </summary>
	public class Nested { }


	/// <summary>
	/// ID string generated is "T:N.X.D".
	/// </summary>
	public delegate void D (int i);


	/// <summary>
	/// ID string generated is "M:N.X.op_Explicit(N.X)~System.Int32".
	/// </summary>
	public static explicit operator int (X x) { return 1; }

	public static void Linq (IEnumerable<string> enumerable, Func<string> selector)
	{
	}

	/// <summary>
	/// ID string generated is "M:N.X.N#IX{N#KVP{System#String,System#Int32}}#IXA(N.KVP{System.String,System.Int32})"
	/// </summary>
	void IX<KVP<string, int>>.IXA (KVP<string, int> k) { }
}

public interface IX<K> {
	void IXA (K k);
}

public class KVP<K, T> { }

public class GenericMethod {
	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithNestedType``1(N.GenericType{``0}.NestedType)".
	/// </summary>
	public void WithNestedType<T> (GenericType<T>.NestedType nestedType) { }


	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithIntOfNestedType``1(N.GenericType{System.Int32}.NestedType)".
	/// </summary>
	public void WithIntOfNestedType<T> (GenericType<int>.NestedType nestedType) { }


	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithNestedGenericType``1(N.GenericType{``0}.NestedGenericType{``0}.NestedType)".
	/// </summary>
	public void WithNestedGenericType<T> (GenericType<T>.NestedGenericType<T>.NestedType nestedType) { }


	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithIntOfNestedGenericType``1(N.GenericType{System.Int32}.NestedGenericType{System.Int32}.NestedType)".
	/// </summary>
	public void WithIntOfNestedGenericType<T> (GenericType<int>.NestedGenericType<int>.NestedType nestedType) { }


	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithMultipleTypeParameterAndNestedGenericType``2(N.GenericType{``0}.NestedGenericType{``1}.NestedType)".
	/// </summary>
	public void WithMultipleTypeParameterAndNestedGenericType<T1, T2> (GenericType<T1>.NestedGenericType<T2>.NestedType nestedType) { }


	/// <summary>
	/// ID string generated is "M:N.GenericMethod.WithMultipleTypeParameterAndIntOfNestedGenericType``2(N.GenericType{System.Int32}.NestedGenericType{System.Int32}.NestedType)".
	/// </summary>
	public void WithMultipleTypeParameterAndIntOfNestedGenericType<T1, T2> (GenericType<int>.NestedGenericType<int>.NestedType nestedType) { }
}

public class GenericType<T> {
	public class NestedType { }

	public class NestedGenericType<TNested> {
		public class NestedType { }

		/// <summary>
		/// ID string generated is "M:N.GenericType`1.NestedGenericType`1.WithTypeParameterOfGenericMethod``1(System.Collections.Generic.List{``0})"
		/// </summary>
		public void WithTypeParameterOfGenericMethod<TMethod> (List<TMethod> list) { }


		/// <summary>
		/// ID string generated is "M:N.GenericType`1.NestedGenericType`1.WithTypeParameterOfGenericType(System.Collections.Generic.Dictionary{`0,`1})"
		/// </summary>
		public void WithTypeParameterOfGenericType (Dictionary<T, TNested> dict) { }


		/// <summary>
		/// ID string generated is "M:N.GenericType`1.NestedGenericType`1.WithTypeParameterOfGenericType``1(System.Collections.Generic.List{`1})"
		/// </summary>
		public void WithTypeParameterOfNestedGenericType<TMethod> (List<TNested> list) { }


		/// <summary>
		/// ID string generated is "M:N.GenericType`1.NestedGenericType`1.WithTypeParameterOfGenericTypeAndGenericMethod``1(System.Collections.Generic.Dictionary{`1,``0})"
		/// </summary>
		public void WithTypeParameterOfGenericTypeAndGenericMethod<TMethod> (Dictionary<TNested, TMethod> dict) { }
	}
}
