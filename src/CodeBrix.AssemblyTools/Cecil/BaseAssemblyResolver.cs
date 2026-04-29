//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using CodeBrix.AssemblyTools.Collections.Generic;

namespace CodeBrix.AssemblyTools; //was previously: Mono.Cecil;


public delegate AssemblyDefinition AssemblyResolveEventHandler (object sender, AssemblyNameReference reference);

public sealed class AssemblyResolveEventArgs : EventArgs {

	readonly AssemblyNameReference reference;

	public AssemblyNameReference AssemblyReference {
		get { return reference; }
	}

	public AssemblyResolveEventArgs (AssemblyNameReference reference)
	{
		this.reference = reference;
	}
}

public sealed class AssemblyResolutionException : FileNotFoundException {

	readonly AssemblyNameReference reference;

	public AssemblyNameReference AssemblyReference {
		get { return reference; }
	}

	public AssemblyResolutionException (AssemblyNameReference reference)
		: this (reference, null)
	{
	}

	public AssemblyResolutionException (AssemblyNameReference reference, Exception innerException)
		: base (string.Format ("Failed to resolve assembly: '{0}'", reference), innerException)
	{
		this.reference = reference;
	}

}

public abstract class BaseAssemblyResolver : IAssemblyResolver {

	static readonly bool on_mono = Type.GetType ("Mono.Runtime") != null;

	readonly Collection<string> directories;

	// Maps file names of available trusted platform assemblies to their full paths.
	// Internal for testing.
	internal static readonly Lazy<Dictionary<string, string>> TrustedPlatformAssemblies = new Lazy<Dictionary<string, string>> (CreateTrustedPlatformAssemblyMap);

	public void AddSearchDirectory (string directory)
	{
		directories.Add (directory);
	}

	public void RemoveSearchDirectory (string directory)
	{
		directories.Remove (directory);
	}

	public string [] GetSearchDirectories ()
	{
		var directories = new string [this.directories.size];
		Array.Copy (this.directories.items, directories, directories.Length);
		return directories;
	}

	public event AssemblyResolveEventHandler ResolveFailure;

	protected BaseAssemblyResolver ()
	{
		directories = new Collection<string> (2) { ".", "bin" };
	}

	AssemblyDefinition GetAssembly (string file, ReaderParameters parameters)
	{
		if (parameters.AssemblyResolver == null)
			parameters.AssemblyResolver = this;

		return ModuleDefinition.ReadModule (file, parameters).Assembly;
	}

	public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
	{
		return Resolve (name, new ReaderParameters ());
	}

	public virtual AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
	{
		Mixin.CheckName (name);
		Mixin.CheckParameters (parameters);

		var assembly = SearchDirectory (name, directories, parameters);
		if (assembly != null)
			return assembly;

		if (name.IsRetargetable) {
			// if the reference is retargetable, zero it
			name = new AssemblyNameReference (name.Name, Mixin.ZeroVersion) {
				PublicKeyToken = Empty<byte>.Array,
			};
		}

		assembly = SearchTrustedPlatformAssemblies (name, parameters);
		if (assembly != null)
			return assembly;
		if (ResolveFailure != null) {
			assembly = ResolveFailure (this, name);
			if (assembly != null)
				return assembly;
		}

		throw new AssemblyResolutionException (name);
	}

	AssemblyDefinition SearchTrustedPlatformAssemblies (AssemblyNameReference name, ReaderParameters parameters)
	{
		if (name.IsWindowsRuntime)
			return null;

		if (TrustedPlatformAssemblies.Value.TryGetValue (name.Name, out string path))
			return GetAssembly (path, parameters);

		return null;
	}

	static Dictionary<string, string> CreateTrustedPlatformAssemblyMap ()
	{
		var result = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);

		string paths;

		try {
			paths = (string) AppDomain.CurrentDomain.GetData ("TRUSTED_PLATFORM_ASSEMBLIES");
		} catch {
			paths = null;
		}

		if (paths == null)
			return result;

		foreach (var path in paths.Split (Path.PathSeparator))
			if (string.Equals (Path.GetExtension (path), ".dll", StringComparison.OrdinalIgnoreCase))
				result [Path.GetFileNameWithoutExtension (path)] = path;

		return result;
	}

	protected virtual AssemblyDefinition SearchDirectory (AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
	{
		var extensions = name.IsWindowsRuntime ? new [] { ".winmd", ".dll" } : new [] { ".dll", ".exe" };
		foreach (var directory in directories) {
			foreach (var extension in extensions) {
				string file = Path.Combine (directory, name.Name + extension);
				if (!File.Exists (file))
					continue;
				try {
					return GetAssembly (file, parameters);
				} catch (System.BadImageFormatException) {
					continue;
				}
			}
		}

		return null;
	}

	static bool IsZero (Version version)
	{
		return version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0;
	}

	public void Dispose ()
	{
		Dispose (true);
		GC.SuppressFinalize (this);
	}

	protected virtual void Dispose (bool disposing)
	{
	}
}
