================================================================================
AGENT-README: CodeBrix.AssemblyTools
A Guide for AI Coding Agents — CONSUMING the CodeBrix.AssemblyTools.MitLicenseForever NuGet package
================================================================================


OVERVIEW
========

CodeBrix.AssemblyTools is a fully managed library for reading, writing and
rewriting .NET assemblies. It gives you an object model over every part of a
managed PE file: the assembly and module manifests, types, methods, fields,
properties, events, generic parameters, custom attributes, security
declarations, P/Invoke and marshalling metadata, embedded/linked resources,
exported (forwarded) types, the IL of every method body, and the debug
symbols that describe that IL (portable PDB, embedded portable PDB, native
Windows PDB, and Mono MDB). You can open an existing assembly, inspect or
change anything in it, and write the result back out; or build a brand-new
assembly from nothing.

Target framework: .NET 10 or later. Everything is managed code; there are
no native dependencies. (Two operations are Windows-only at run time -- see
INSTALLATION.)

Provenance: CodeBrix.AssemblyTools is a port of Mono.Cecil 0.11.6 with all
four upstream libraries (core, Rocks, Pdb, Mdb) merged into ONE assembly and
re-namespaced. The public API is otherwise the upstream API one-to-one, so
knowledge of the upstream library transfers directly -- but the namespaces
do not. Map them as follows and never write `using Mono.Cecil...` in a
consumer of this package:

    upstream namespace                      -> CodeBrix.AssemblyTools namespace
    Mono.Cecil                              -> CodeBrix.AssemblyTools
    Mono.Cecil.Cil                          -> CodeBrix.AssemblyTools.Cil
    Mono.Cecil.Metadata                     -> CodeBrix.AssemblyTools.Metadata
    Mono.Cecil.Rocks                        -> CodeBrix.AssemblyTools.Rocks
    Mono.Cecil.Pdb                          -> CodeBrix.AssemblyTools.Pdb
    Mono.Cecil.Mdb                          -> CodeBrix.AssemblyTools.Mdb
    Mono.Collections.Generic                -> CodeBrix.AssemblyTools.Collections.Generic
    Mono.CompilerServices.SymbolWriter      -> CodeBrix.AssemblyTools.Mdb.SymbolWriter

A project that used the upstream NuGet package migrates by replacing that
package reference with this one and doing a find/replace of the namespace
prefixes above; no code changes are needed.

Behaviours this port carries that are worth knowing as a consumer (these are
present-tense facts about the package, not release notes):

  * The default assembly resolver tries `<Name>.dll` before `<Name>.exe` in
    each search directory, so a modern app layout with an unmanaged
    `Foo.exe` apphost beside the managed `Foo.dll` resolves correctly.
  * `ImportReference(System.Reflection.*)` preserves required/optional
    custom modifiers (`in`, `ref readonly`, `init`, `required`, unmanaged
    constraints) instead of dropping them.
  * `GenericParameter.AllowByRefLikeConstraint` exposes the C# 13
    `where T : allows ref struct` flag.
  * Type-level custom debug information (e.g. NullableContext entries on
    compiler-generated types) survives a read/write round trip.
  * `Instruction.GetPrototype()` clones an instruction (opcode + operand).
  * Strong-name signing hashes with `SHA1.Create()`, which is FIPS-compliant
    on FIPS-enforced hosts.
  * Negative integer constants in custom-attribute blobs and marshal
    descriptors are encoded correctly (compressed-integer encoding).


INSTALLATION
============

NuGet package id:
    CodeBrix.AssemblyTools.MitLicenseForever

dotnet CLI:
    dotnet add package CodeBrix.AssemblyTools.MitLicenseForever

NuGet dependencies:   none (only the .NET runtime).
License:              MIT
Target framework:     .NET 10 or later.
Assembly name:        CodeBrix.AssemblyTools (one merged assembly).

Platform requirements:
  * Reading assemblies, IL and every symbol format (portable PDB, embedded
    portable PDB, native Windows PDB, Mono MDB) is pure managed code and
    works on Linux, macOS and Windows.
  * WRITING native Windows PDBs (NativePdbWriterProvider, or PdbWriterProvider
    when the input symbols were native) uses the COM SymWriter via
    `ole32.dll` and therefore only works on Windows. Portable PDB, embedded
    portable PDB and MDB writing are cross-platform.
  * Strong-name signing with `WriterParameters.StrongNameKeyContainer` uses a
    Windows CSP key container (Windows-only). Signing with
    `StrongNameKeyBlob` (the raw bytes of a .snk file) is pure managed and
    cross-platform.


KEY NAMESPACES / USINGS
=======================

    using CodeBrix.AssemblyTools;                     // assemblies, modules, the
                                                      // metadata model, resolvers,
                                                      // Reader/WriterParameters
    using CodeBrix.AssemblyTools.Cil;                 // MethodBody, ILProcessor,
                                                      // Instruction, OpCodes,
                                                      // debug-info model, symbol
                                                      // reader/writer providers
    using CodeBrix.AssemblyTools.Metadata;            // MetadataToken, TokenType
    using CodeBrix.AssemblyTools.Collections.Generic; // Collection<T> (the list
                                                      // type used by every
                                                      // "Xxxs" property)
    using CodeBrix.AssemblyTools.Rocks;               // extension methods:
                                                      // GetAllTypes, SimplifyMacros,
                                                      // MakeGenericInstanceType...
    using CodeBrix.AssemblyTools.Pdb;                 // native-PDB providers
    using CodeBrix.AssemblyTools.Mdb;                 // Mono MDB providers
    using CodeBrix.AssemblyTools.Mdb.SymbolWriter;    // low-level MDB file model
                                                      // (rarely needed)

`CodeBrix.AssemblyTools.Collections.Generic.Collection<T>` implements
`IList<T>`, so LINQ (`.First`, `.Where`, `.ToList`) works on every
collection property in the model.

The namespaces CodeBrix.AssemblyTools.PE, .Security.Cryptography, .Internal
and .Pdb.Cci exist in the assembly but contain NO public types; do not add
usings for them.


CORE API REFERENCE
==================

Conventions used below: "Xxx {get;set;}" is a read/write property, "Xxx"
alone is read-only. Every `Collection<T>` property is live and mutable
(Add/Insert/Remove edits the model) and has a matching `HasXxx` boolean that
is cheap to test and does not force the collection to be materialised.


READING ASSEMBLIES AND MODULES
------------------------------

The two entry points. An AssemblyDefinition owns one or more
ModuleDefinitions; in practice nearly everything lives on `MainModule`.

    // CodeBrix.AssemblyTools.AssemblyDefinition
    public static AssemblyDefinition ReadAssembly (string fileName)
    public static AssemblyDefinition ReadAssembly (string fileName, ReaderParameters parameters)
    public static AssemblyDefinition ReadAssembly (Stream stream)
    public static AssemblyDefinition ReadAssembly (Stream stream, ReaderParameters parameters)

    // CodeBrix.AssemblyTools.ModuleDefinition
    public static ModuleDefinition ReadModule (string fileName)
    public static ModuleDefinition ReadModule (string fileName, ReaderParameters parameters)
    public static ModuleDefinition ReadModule (Stream stream)
    public static ModuleDefinition ReadModule (Stream stream, ReaderParameters parameters)

Rules that follow from the implementation:
  * The `(string fileName)` overloads open the file themselves with
    FileAccess.Read / FileShare.Read (or FileAccess.ReadWrite when
    `ReaderParameters.ReadWrite` is true) and keep it OPEN until you Dispose
    the module/assembly. With `InMemory = true` the file is copied into a
    MemoryStream and closed immediately.
  * The `(Stream stream)` overloads do NOT take ownership of your stream;
    the stream must be readable and seekable. You dispose it.
  * The overloads without ReaderParameters use `ReadingMode.Deferred`.
  * `ReadAssembly` throws ArgumentException if the file is a netmodule
    (a module without an assembly manifest); use `ReadModule` for those.
  * Both types implement IDisposable. Always `using` them.

AssemblyDefinition members:

    public AssemblyNameDefinition Name {get;set;}
    public string FullName
    public MetadataToken MetadataToken {get;set;}
    public Collection<ModuleDefinition> Modules
    public ModuleDefinition MainModule
    public MethodDefinition EntryPoint {get;set;}      // forwards to MainModule
    public bool HasCustomAttributes
    public Collection<CustomAttribute> CustomAttributes
    public bool HasSecurityDeclarations
    public Collection<SecurityDeclaration> SecurityDeclarations
    public void Dispose ()
    public override string ToString ()                  // == FullName

ModuleDefinition members (it derives from ModuleReference, so it also has
`string Name {get;set;}` and `MetadataToken MetadataToken`):

    // identity and image facts
    public bool IsMain
    public ModuleKind Kind {get;set;}                   // Dll, Console, Windows, NetModule
    public MetadataKind MetadataKind {get;set;}
    public TargetRuntime Runtime {get;set;}             // Net_1_0, Net_1_1, Net_2_0, Net_4_0
    public string RuntimeVersion {get;set;}             // e.g. "v4.0.30319"
    public TargetArchitecture Architecture {get;set;}   // I386, AMD64, IA64, ARM, ARMv7, ARM64
    public ModuleAttributes Attributes {get;set;}
    public ModuleCharacteristics Characteristics {get;set;}
    public string FullyQualifiedName
    public string FileName                              // null when read from a stream
    public Guid Mvid {get;set;}
    public ReadingMode ReadingMode {get;set;}
    public bool HasSymbols
    public ISymbolReader SymbolReader
    public bool HasDebugHeader
    public ImageDebugHeader GetDebugHeader ()
    public AssemblyDefinition Assembly                  // null for a netmodule

    // services
    public IAssemblyResolver AssemblyResolver           // lazily creates a
                                                        //   DefaultAssemblyResolver
    public IMetadataResolver MetadataResolver
    public TypeSystem TypeSystem

    // manifest tables
    public bool HasAssemblyReferences
    public Collection<AssemblyNameReference> AssemblyReferences
    public bool HasModuleReferences
    public Collection<ModuleReference> ModuleReferences
    public bool HasResources
    public Collection<Resource> Resources
    public bool HasCustomAttributes
    public Collection<CustomAttribute> CustomAttributes
    public bool HasTypes
    public Collection<TypeDefinition> Types             // TOP-LEVEL types only
    public bool HasExportedTypes
    public Collection<ExportedType> ExportedTypes       // type forwarders etc.
    public MethodDefinition EntryPoint {get;set;}
    public bool HasCustomDebugInformations
    public Collection<CustomDebugInformation> CustomDebugInformations

    // lookups
    public TypeDefinition GetType (string fullName)     // "Ns.Outer/Nested" for nested
    public TypeDefinition GetType (string @namespace, string name)
    public TypeReference GetType (string fullName, bool runtimeName)
                                                        // runtimeName=true parses a
                                                        //   reflection-style name
                                                        //   ("List`1[[System.Int32,...]]")
    public IEnumerable<TypeDefinition> GetTypes ()      // all types INCLUDING nested,
                                                        //   depth-first
    public bool HasTypeReference (string fullName)
    public bool HasTypeReference (string scope, string fullName)
    public bool TryGetTypeReference (string fullName, out TypeReference type)
    public bool TryGetTypeReference (string scope, string fullName, out TypeReference type)
    public IEnumerable<TypeReference> GetTypeReferences ()
    public IEnumerable<MemberReference> GetMemberReferences ()
    public IEnumerable<CustomAttribute> GetCustomAttributes ()
    public IMetadataTokenProvider LookupToken (int token)
    public IMetadataTokenProvider LookupToken (MetadataToken token)

    // control
    public void ImmediateRead ()                        // switch a Deferred module to
                                                        //   fully-read
    public void ReadSymbols ()                          // DefaultSymbolReaderProvider,
                                                        //   throws if none found
    public void ReadSymbols (ISymbolReader reader)
    public void ReadSymbols (ISymbolReader reader, bool throwIfSymbolsAreNotMaching)
    public void Dispose ()                              // closes the image stream, the
                                                        //   symbol reader, and an
                                                        //   assembly resolver the
                                                        //   module created itself

`GetType(string)` returns null when the type is not defined in this module
(it does NOT follow type forwarders or references).


READER PARAMETERS
-----------------

    public enum ReadingMode { Immediate = 1, Deferred = 2 }

    public sealed class ReaderParameters {
        public ReaderParameters ()                         // ReadingMode.Deferred
        public ReaderParameters (ReadingMode readingMode)
        public ReadingMode ReadingMode {get;set;}
        public bool InMemory {get;set;}                    // copy file to memory, release
                                                           //   the file handle at once
        public bool ReadWrite {get;set;}                   // open the file for writing so
                                                           //   Write() can save in place
        public bool ReadSymbols {get;set;}                 // use DefaultSymbolReaderProvider
                                                           //   when SymbolReaderProvider is null
        public ISymbolReaderProvider SymbolReaderProvider {get;set;}
        public Stream SymbolStream {get;set;}              // explicit symbol stream instead
                                                           //   of a sidecar file
        public bool ThrowIfSymbolsAreNotMatching {get;set;}  // default TRUE
        public IAssemblyResolver AssemblyResolver {get;set;}
        public IMetadataResolver MetadataResolver {get;set;}
        public IMetadataImporterProvider MetadataImporterProvider {get;set;}
        public IReflectionImporterProvider ReflectionImporterProvider {get;set;}
        public bool ApplyWindowsRuntimeProjections {get;set;}  // for .winmd files
    }

Reading-mode semantics:
  * Deferred (default): the manifest is read; types, members, bodies,
    attributes and symbols are materialised lazily on first access, each
    holding a reference to the still-open image. Fastest when you touch a
    small part of a large assembly.
  * Immediate: every table is decoded up front (types, members, custom
    attributes, security declarations, symbols) and the internal metadata
    caches are released. Use it when you will visit everything anyway, or
    when you must dispose the input stream early.
  * Write() on a Deferred module first performs the equivalent of an
    immediate read, so a "read, tweak one method, write" pipeline still
    reads the whole image once.

Symbol reading is triggered when EITHER `ReadSymbols == true` OR
`SymbolReaderProvider != null`. A module whose image already contains
portable-PDB tables (a PDB read as a module) is handled automatically.


ASSEMBLY RESOLUTION
-------------------

Resolution turns an AssemblyNameReference (a "reference to Foo, 1.0.0.0,
token ...") into a loaded AssemblyDefinition. It is used by every
`Resolve()` call on a reference whose scope is another assembly.

    public interface IAssemblyResolver : IDisposable {
        AssemblyDefinition Resolve (AssemblyNameReference name);
        AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters);
    }

    public abstract class BaseAssemblyResolver : IAssemblyResolver {
        public void AddSearchDirectory (string directory)
        public void RemoveSearchDirectory (string directory)
        public string [] GetSearchDirectories ()
        public event AssemblyResolveEventHandler ResolveFailure;
        public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
        public virtual AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
        protected virtual AssemblyDefinition SearchDirectory (AssemblyNameReference name,
                                                              IEnumerable<string> directories,
                                                              ReaderParameters parameters)
        public void Dispose ()
    }

    public delegate AssemblyDefinition AssemblyResolveEventHandler (object sender,
                                                                   AssemblyNameReference reference);

    public class DefaultAssemblyResolver : BaseAssemblyResolver {
        public DefaultAssemblyResolver ()
        public override AssemblyDefinition Resolve (AssemblyNameReference name)  // cached by FullName
        protected void RegisterAssembly (AssemblyDefinition assembly)            // pre-seed the cache
    }

    public sealed class AssemblyResolutionException : FileNotFoundException {
        public AssemblyNameReference AssemblyReference
    }

Search order of BaseAssemblyResolver.Resolve:
  1. Each search directory in order (the initial list is "." and "bin",
     i.e. RELATIVE to the current working directory, not to the assembly
     being rewritten), trying `<Name>.dll` then `<Name>.exe` (`.winmd` then
     `.dll` for Windows Runtime references); files that are not managed
     images are skipped.
  2. The trusted-platform-assembly list of the RUNNING process (the
     framework assemblies of the runtime executing your tool -- not
     necessarily the framework the target assembly was built against).
  3. The `ResolveFailure` event, if anything is subscribed.
  4. Otherwise throws AssemblyResolutionException.

DefaultAssemblyResolver additionally caches every result by full name and
disposes all cached assemblies when it is disposed. A module disposes the
resolver only if it created the resolver itself; a resolver you passed in
via ReaderParameters is yours to dispose.

Typical wiring:

    var resolver = new DefaultAssemblyResolver ();
    resolver.AddSearchDirectory (Path.GetDirectoryName (inputPath));
    resolver.AddSearchDirectory ("/path/to/target/framework/ref/pack");
    var rp = new ReaderParameters { AssemblyResolver = resolver };
    using var asm = AssemblyDefinition.ReadAssembly (inputPath, rp);

Metadata resolution (reference -> definition, once the assembly is found):

    public interface IMetadataResolver {
        TypeDefinition Resolve (TypeReference type);
        FieldDefinition Resolve (FieldReference field);
        MethodDefinition Resolve (MethodReference method);
    }
    public class MetadataResolver : IMetadataResolver     // the default; takes an
                                                          //   IAssemblyResolver in its ctor
    public sealed class ResolutionException : Exception {
        public MemberReference Member
        public IMetadataScope Scope
    }


DEBUG SYMBOLS: READING
----------------------

    public interface ISymbolReaderProvider {
        ISymbolReader GetSymbolReader (ModuleDefinition module, string fileName);
        ISymbolReader GetSymbolReader (ModuleDefinition module, Stream symbolStream);
    }
    public interface ISymbolReader : IDisposable {
        ISymbolWriterProvider GetWriterProvider ();
        bool ProcessDebugHeader (ImageDebugHeader header);
        MethodDebugInformation Read (MethodDefinition method);
        Collection<CustomDebugInformation> Read (ICustomDebugInformationProvider provider);
    }

Providers (all in CodeBrix.AssemblyTools.Cil unless noted):

    DefaultSymbolReaderProvider ()                       // == (throwIfNoSymbol: true)
    DefaultSymbolReaderProvider (bool throwIfNoSymbol)
        Probes, in order: an embedded portable PDB inside the image; a
        `<assembly-name>.pdb` beside the file (portable or native, detected
        by magic); a `<assembly-file>.mdb` beside the file. When nothing is
        found it throws SymbolsNotFoundException (or returns null when
        constructed with throwIfNoSymbol: false). For the Stream overload
        it sniffs the stream header instead.
    PortablePdbReaderProvider                            // sidecar .pdb, portable only
    EmbeddedPortablePdbReaderProvider                    // PDB embedded in the image
    CodeBrix.AssemblyTools.Pdb.PdbReaderProvider         // embedded -> portable ->
                                                         //   native, by detection
    CodeBrix.AssemblyTools.Pdb.NativePdbReaderProvider   // native Windows PDB only
                                                         //   (managed reader, any OS)
    CodeBrix.AssemblyTools.Mdb.MdbReaderProvider         // Mono .mdb

Related readers you may see typed: PortablePdbReader, EmbeddedPortablePdbReader,
Pdb.NativePdbReader, Mdb.MdbReader (all implement ISymbolReader).

Exceptions:

    public sealed class SymbolsNotFoundException : FileNotFoundException
    public sealed class SymbolsNotMatchingException : InvalidOperationException
        // thrown when the PDB's GUID/age does not match the image and
        // ThrowIfSymbolsAreNotMatching is true; set it false to silently
        // continue without symbols

Sidecar naming used by the providers: the PDB is
`Path.ChangeExtension(assemblyPath, ".pdb")`; the MDB is
`assemblyPath + ".mdb"`.


THE METADATA MODEL: TYPES
-------------------------

Everything member-like derives from MemberReference:

    public abstract class MemberReference : IMetadataTokenProvider {
        public virtual string Name {get;set;}
        public abstract string FullName
        public virtual TypeReference DeclaringType {get;set;}
        public MetadataToken MetadataToken {get;set;}
        public virtual ModuleDefinition Module
        public virtual bool IsDefinition
        public virtual bool ContainsGenericParameter
        public IMemberDefinition Resolve ()
    }

"Reference" classes describe a member as seen from some module (possibly
another assembly's member); "Definition" classes are the actual declaration
and derive from the corresponding Reference class. `Resolve()` walks from a
reference to its definition (see RESOLVING REFERENCES below).

    public class TypeReference : MemberReference, IGenericParameterProvider {
        public TypeReference (string @namespace, string name, ModuleDefinition module,
                              IMetadataScope scope)
        public TypeReference (string @namespace, string name, ModuleDefinition module,
                              IMetadataScope scope, bool valueType)
        public virtual string Namespace {get;set;}
        public virtual bool IsValueType {get;set;}
        public virtual IMetadataScope Scope {get;set;}   // AssemblyNameReference,
                                                        //   ModuleDefinition or
                                                        //   ModuleReference
        public virtual bool HasGenericParameters
        public virtual Collection<GenericParameter> GenericParameters
        public virtual MetadataType MetadataType
        public virtual bool IsByReference / IsPointer / IsSentinel / IsArray /
                            IsGenericParameter / IsGenericInstance /
                            IsRequiredModifier / IsOptionalModifier /
                            IsPinned / IsFunctionPointer / IsPrimitive
        public virtual TypeReference GetElementType ()  // strips array/pointer/
                                                        //   byref/generic-instance
                                                        //   wrappers
        public new virtual TypeDefinition Resolve ()    // null if not found
    }

    public sealed class TypeDefinition : TypeReference, IMemberDefinition,
                                         ISecurityDeclarationProvider,
                                         ICustomDebugInformationProvider {
        public TypeDefinition (string @namespace, string name, TypeAttributes attributes)
        public TypeDefinition (string @namespace, string name, TypeAttributes attributes,
                               TypeReference baseType)
        public TypeAttributes Attributes {get;set;}
        public TypeReference BaseType {get;set;}         // null for interfaces/<Module>
        public short PackingSize {get;set;}
        public int ClassSize {get;set;}
        public bool HasLayoutInfo
        public bool HasInterfaces
        public Collection<InterfaceImplementation> Interfaces
        public bool HasNestedTypes
        public Collection<TypeDefinition> NestedTypes
        public bool HasMethods
        public Collection<MethodDefinition> Methods
        public bool HasFields
        public Collection<FieldDefinition> Fields
        public bool HasEvents
        public Collection<EventDefinition> Events
        public bool HasProperties
        public Collection<PropertyDefinition> Properties
        public bool HasSecurityDeclarations
        public Collection<SecurityDeclaration> SecurityDeclarations
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public override Collection<GenericParameter> GenericParameters
        public bool HasCustomDebugInformations
        public Collection<CustomDebugInformation> CustomDebugInformations
        public new TypeDefinition DeclaringType {get;set;}
        public bool IsNotPublic / IsPublic / IsNestedPublic / IsNestedPrivate /
                    IsNestedFamily / IsNestedAssembly / IsNestedFamilyAndAssembly /
                    IsNestedFamilyOrAssembly / IsAutoLayout / IsSequentialLayout /
                    IsExplicitLayout / IsClass / IsInterface / IsAbstract /
                    IsSealed / IsSpecialName / IsImport / IsSerializable /
                    IsWindowsRuntime / IsAnsiClass / IsUnicodeClass / IsAutoClass /
                    IsBeforeFieldInit / IsRuntimeSpecialName / IsEnum
                    {get;set;}                           // each toggles Attributes bits
        public override bool IsValueType / IsPrimitive
        public override TypeDefinition Resolve ()        // returns this
    }

    public sealed class InterfaceImplementation : ICustomAttributeProvider {
        public InterfaceImplementation (TypeReference interfaceType)
        public TypeReference InterfaceType {get;set;}
        public Collection<CustomAttribute> CustomAttributes
    }

Composed types (all derive from TypeSpecification, which exposes
`TypeReference ElementType {get;set;}`):

    public sealed class GenericInstanceType : TypeSpecification, IGenericInstance {
        public GenericInstanceType (TypeReference type)  // the open generic type
        public bool HasGenericArguments
        public Collection<TypeReference> GenericArguments
    }
    public sealed class ArrayType : TypeSpecification {
        public ArrayType (TypeReference type)            // vector (T[])
        public ArrayType (TypeReference type, int rank)  // T[,] etc.
        public Collection<ArrayDimension> Dimensions
        public int Rank
        public bool IsVector
        public bool IsSized
    }
    public struct ArrayDimension {
        public ArrayDimension (int? lowerBound, int? upperBound)
        public int? LowerBound {get;set;}
        public int? UpperBound {get;set;}
    }
    public sealed class PointerType : TypeSpecification         // T*
        public PointerType (TypeReference type)
    public sealed class ByReferenceType : TypeSpecification     // T& (ref/out/in)
        public ByReferenceType (TypeReference type)
    public sealed class PinnedType : TypeSpecification          // pinned locals
        public PinnedType (TypeReference type)
    public sealed class SentinelType : TypeSpecification        // vararg sentinel
    public sealed class OptionalModifierType : TypeSpecification, IModifierType
        public OptionalModifierType (TypeReference modifierType, TypeReference type)
        public TypeReference ModifierType {get;set;}
    public sealed class RequiredModifierType : TypeSpecification, IModifierType
        public RequiredModifierType (TypeReference modifierType, TypeReference type)
        public TypeReference ModifierType {get;set;}
    public sealed class FunctionPointerType : TypeSpecification, IMethodSignature {
        public FunctionPointerType ()
        public bool HasThis / ExplicitThis {get;set;}
        public MethodCallingConvention CallingConvention {get;set;}
        public Collection<ParameterDefinition> Parameters
        public TypeReference ReturnType {get;set;}
        public MethodReturnType MethodReturnType {get;set;}
    }

Generic parameters:

    public sealed class GenericParameter : TypeReference, ICustomAttributeProvider {
        public GenericParameter (IGenericParameterProvider owner)
        public GenericParameter (string name, IGenericParameterProvider owner)
        public GenericParameterAttributes Attributes {get;set;}
        public int Position
        public GenericParameterType Type                 // Type or Method
        public IGenericParameterProvider Owner
        public MethodReference DeclaringMethod
        public Collection<GenericParameterConstraint> Constraints
        public Collection<CustomAttribute> CustomAttributes
        public bool AllowByRefLikeConstraint {get;set;}
        public override TypeDefinition Resolve ()        // null: a generic
                                                         //   parameter is not a type
    }
    public sealed class GenericParameterConstraint : ICustomAttributeProvider {
        public GenericParameterConstraint (TypeReference constraintType)
        public TypeReference ConstraintType {get;set;}
        public Collection<CustomAttribute> CustomAttributes
    }
    public interface IGenericParameterProvider : IMetadataTokenProvider {
        bool HasGenericParameters { get; }
        bool IsDefinition { get; }
        ModuleDefinition Module { get; }
        Collection<GenericParameter> GenericParameters { get; }
        GenericParameterType GenericParameterType { get; }
    }
    public interface IGenericInstance : IMetadataTokenProvider {
        bool HasGenericArguments { get; }
        Collection<TypeReference> GenericArguments { get; }
    }

Both TypeDefinition/TypeReference and MethodDefinition/MethodReference are
IGenericParameterProvider, so `context` arguments to ImportReference accept
either.


THE METADATA MODEL: METHODS, FIELDS, PROPERTIES, EVENTS
-------------------------------------------------------

    public class MethodReference : MemberReference, IMethodSignature,
                                   IGenericParameterProvider {
        public MethodReference (string name, TypeReference returnType)
        public MethodReference (string name, TypeReference returnType,
                                TypeReference declaringType)
        public virtual bool HasThis {get;set;}           // instance method
        public virtual bool ExplicitThis {get;set;}
        public virtual MethodCallingConvention CallingConvention {get;set;}
        public virtual bool HasParameters
        public virtual Collection<ParameterDefinition> Parameters
        public virtual bool HasGenericParameters
        public virtual Collection<GenericParameter> GenericParameters
        public TypeReference ReturnType {get;set;}
        public virtual MethodReturnType MethodReturnType {get;set;}
        public virtual bool IsGenericInstance
        public override string FullName                  // "RetType Ns.Type::Name(Args)"
        public virtual MethodReference GetElementMethod ()  // strips a generic instance
        public new virtual MethodDefinition Resolve ()   // null if not found
    }

    public sealed class MethodDefinition : MethodReference, IMemberDefinition,
                                           ISecurityDeclarationProvider,
                                           ICustomDebugInformationProvider {
        public MethodDefinition (string name, MethodAttributes attributes,
                                 TypeReference returnType)
        public MethodAttributes Attributes {get;set;}
        public MethodImplAttributes ImplAttributes {get;set;}
        public MethodSemanticsAttributes SemanticsAttributes {get;set;}
        public int RVA
        public bool HasBody                              // false for abstract, P/Invoke,
                                                         //   internalcall, native, runtime
        public MethodBody Body {get;set;}                // created on first access when
                                                         //   HasBody and the method is new
        public MethodDebugInformation DebugInformation {get;set;}  // created on first
                                                                   //   access
        public bool HasPInvokeInfo
        public PInvokeInfo PInvokeInfo {get;set;}
        public bool HasOverrides
        public Collection<MethodReference> Overrides      // explicit interface impls
        public bool HasSecurityDeclarations
        public Collection<SecurityDeclaration> SecurityDeclarations
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public bool HasCustomDebugInformations
        public Collection<CustomDebugInformation> CustomDebugInformations
        public bool NoInlining / NoOptimization / AggressiveInlining /
                    AggressiveOptimization {get;set;}
        public new TypeDefinition DeclaringType {get;set;}
        public bool IsCompilerControlled / IsPrivate / IsFamilyAndAssembly /
                    IsAssembly / IsFamily / IsFamilyOrAssembly / IsPublic / IsStatic /
                    IsFinal / IsVirtual / IsHideBySig / IsReuseSlot / IsNewSlot /
                    IsCheckAccessOnOverride / IsAbstract / IsSpecialName /
                    IsPInvokeImpl / IsUnmanagedExport / IsRuntimeSpecialName /
                    IsIL / IsNative / IsRuntime / IsUnmanaged / IsManaged /
                    IsForwardRef / IsPreserveSig / IsInternalCall / IsSynchronized /
                    IsSetter / IsGetter / IsOther / IsAddOn / IsRemoveOn / IsFire /
                    IsConstructor {get;set;}
        public override MethodDefinition Resolve ()      // returns this
    }

    public sealed class GenericInstanceMethod : MethodSpecification, IGenericInstance {
        public GenericInstanceMethod (MethodReference method)   // the open method
        public bool HasGenericArguments
        public Collection<TypeReference> GenericArguments
    }
    public abstract class MethodSpecification : MethodReference {
        public MethodReference ElementMethod
    }

    public sealed class ParameterDefinition : ParameterReference,
                                              ICustomAttributeProvider,
                                              IConstantProvider,
                                              IMarshalInfoProvider {
        public ParameterDefinition (TypeReference parameterType)
        public ParameterDefinition (string name, ParameterAttributes attributes,
                                    TypeReference parameterType)
        public string Name {get;set;}                    // from ParameterReference
        public int Index                                 // 0-based, excludes `this`
        public TypeReference ParameterType {get;set;}
        public ParameterAttributes Attributes {get;set;}
        public IMethodSignature Method
        public int Sequence                              // 1-based metadata sequence
        public bool HasConstant
        public object Constant {get;set;}                // default value
        public bool HasDefault / HasFieldMarshal {get;set;}
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public bool HasMarshalInfo
        public MarshalInfo MarshalInfo {get;set;}
    }

    public sealed class MethodReturnType : IConstantProvider, ICustomAttributeProvider,
                                           IMarshalInfoProvider {
        public IMethodSignature Method
        public TypeReference ReturnType {get;set;}
        public ParameterAttributes Attributes {get;set;}
        public string Name {get;set;}
        public bool HasConstant
        public object Constant {get;set;}
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public bool HasMarshalInfo
        public MarshalInfo MarshalInfo {get;set;}
    }

    public class FieldReference : MemberReference {
        public FieldReference (string name, TypeReference fieldType)
        public FieldReference (string name, TypeReference fieldType,
                               TypeReference declaringType)
        public TypeReference FieldType {get;set;}
        public new virtual FieldDefinition Resolve ()
    }
    public sealed class FieldDefinition : FieldReference, IMemberDefinition,
                                          IConstantProvider, IMarshalInfoProvider {
        public FieldDefinition (string name, FieldAttributes attributes,
                                TypeReference fieldType)
        public FieldAttributes Attributes {get;set;}
        public bool HasLayoutInfo
        public int Offset {get;set;}                     // explicit layout
        public int RVA
        public byte [] InitialValue {get;set;}           // RVA-static data
        public bool HasConstant
        public object Constant {get;set;}                // literal value
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public bool HasMarshalInfo
        public MarshalInfo MarshalInfo {get;set;}
        public bool HasDefault / HasFieldRVA {get;set;}
        public new TypeDefinition DeclaringType {get;set;}
        public bool IsCompilerControlled / IsPrivate / IsFamilyAndAssembly /
                    IsAssembly / IsFamily / IsFamilyOrAssembly / IsPublic / IsStatic /
                    IsInitOnly / IsLiteral / IsNotSerialized / IsSpecialName /
                    IsPInvokeImpl / IsRuntimeSpecialName {get;set;}
        public override FieldDefinition Resolve ()
    }

    public sealed class PropertyDefinition : PropertyReference, IMemberDefinition,
                                             IConstantProvider {
        public PropertyDefinition (string name, PropertyAttributes attributes,
                                   TypeReference propertyType)
        public TypeReference PropertyType {get;set;}     // from PropertyReference
        public PropertyAttributes Attributes {get;set;}
        public bool HasThis {get;set;}
        public MethodDefinition GetMethod {get;set;}
        public MethodDefinition SetMethod {get;set;}
        public bool HasOtherMethods
        public Collection<MethodDefinition> OtherMethods
        public bool HasParameters                        // indexers
        public override Collection<ParameterDefinition> Parameters
        public bool HasConstant
        public object Constant {get;set;}
        public bool HasDefault {get;set;}
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public new TypeDefinition DeclaringType {get;set;}
        public override PropertyDefinition Resolve ()
    }

    public sealed class EventDefinition : EventReference, IMemberDefinition {
        public EventDefinition (string name, EventAttributes attributes,
                                TypeReference eventType)
        public TypeReference EventType {get;set;}        // from EventReference
        public EventAttributes Attributes {get;set;}
        public MethodDefinition AddMethod {get;set;}
        public MethodDefinition InvokeMethod {get;set;}
        public MethodDefinition RemoveMethod {get;set;}
        public bool HasOtherMethods
        public Collection<MethodDefinition> OtherMethods
        public bool HasCustomAttributes
        public Collection<CustomAttribute> CustomAttributes
        public new TypeDefinition DeclaringType {get;set;}
        public override EventDefinition Resolve ()
    }

Adding a property or event does NOT add its accessor methods: add the
MethodDefinitions to `type.Methods` yourself and then assign
`GetMethod`/`SetMethod`/`AddMethod`/`RemoveMethod`.


THE METADATA MODEL: ATTRIBUTES, SECURITY, MARSHALLING, NAMES, RESOURCES
-----------------------------------------------------------------------

    public sealed class CustomAttribute : ICustomAttribute {
        public CustomAttribute (MethodReference constructor)
        public CustomAttribute (MethodReference constructor, byte [] blob)
        public MethodReference Constructor {get;set;}
        public TypeReference AttributeType
        public bool IsResolved                           // blob decoded into arguments
        public bool HasConstructorArguments
        public Collection<CustomAttributeArgument> ConstructorArguments
        public bool HasFields
        public Collection<CustomAttributeNamedArgument> Fields
        public bool HasProperties
        public Collection<CustomAttributeNamedArgument> Properties
        public byte [] GetBlob ()
    }
    public struct CustomAttributeArgument {
        public CustomAttributeArgument (TypeReference type, object value)
        public TypeReference Type
        public object Value          // primitive, string, TypeReference,
                                     //   CustomAttributeArgument (boxed object arg),
                                     //   or CustomAttributeArgument[] for arrays
    }
    public struct CustomAttributeNamedArgument {
        public CustomAttributeNamedArgument (string name, CustomAttributeArgument argument)
        public string Name
        public CustomAttributeArgument Argument
    }
    public interface ICustomAttributeProvider : IMetadataTokenProvider {
        Collection<CustomAttribute> CustomAttributes { get; }
        bool HasCustomAttributes { get; }
    }

Reading an attribute: `attr.AttributeType.FullName == "System.ObsoleteAttribute"`,
then `attr.ConstructorArguments [0].Value` (a string here). Enum-typed
arguments arrive as the underlying integer with `.Type` set to the enum
type; decoding them requires the enum's definition to be resolvable.

    public sealed class SecurityDeclaration {
        public SecurityDeclaration (SecurityAction action)
        public SecurityDeclaration (SecurityAction action, byte [] blob)
        public SecurityAction Action {get;set;}
        public bool HasSecurityAttributes
        public Collection<SecurityAttribute> SecurityAttributes
        public byte [] GetBlob ()
    }
    public sealed class SecurityAttribute : ICustomAttribute {
        public SecurityAttribute (TypeReference attributeType)
        public TypeReference AttributeType {get;set;}
        public Collection<CustomAttributeNamedArgument> Fields / Properties
    }

    public class MarshalInfo {                            // [MarshalAs]
        public MarshalInfo (NativeType native)
        public NativeType NativeType {get;set;}
    }
    public sealed class ArrayMarshalInfo : MarshalInfo        // ElementType, Size,
                                                              //   SizeParameterIndex,
                                                              //   SizeParameterMultiplier
    public sealed class CustomMarshalInfo : MarshalInfo       // Guid, UnmanagedType,
                                                              //   ManagedType, Cookie
    public sealed class SafeArrayMarshalInfo : MarshalInfo    // ElementType (VariantType)
    public sealed class FixedArrayMarshalInfo : MarshalInfo   // ElementType, Size
    public sealed class FixedSysStringMarshalInfo : MarshalInfo  // Size

    public sealed class PInvokeInfo {                     // [DllImport]
        public PInvokeInfo (PInvokeAttributes attributes, string entryPoint,
                            ModuleReference module)
        public PInvokeAttributes Attributes {get;set;}
        public string EntryPoint {get;set;}
        public ModuleReference Module {get;set;}          // the native library
        public bool IsNoMangle / IsCharSetAnsi / IsCharSetUnicode / IsCharSetAuto /
                    SupportsLastError / IsCallConvWinapi / IsCallConvCdecl /
                    IsCallConvStdCall / IsCallConvThiscall / IsCallConvFastcall /
                    IsBestFitEnabled / IsThrowOnUnmappableCharEnabled ... {get;set;}
    }

    public sealed class CallSite : IMethodSignature {      // operand of `calli`
        public CallSite (TypeReference returnType)
        public bool HasThis / ExplicitThis {get;set;}
        public MethodCallingConvention CallingConvention {get;set;}
        public Collection<ParameterDefinition> Parameters
        public TypeReference ReturnType {get;set;}
    }

Names and scopes:

    public class AssemblyNameReference : IMetadataScope {
        public AssemblyNameReference (string name, Version version)
        public static AssemblyNameReference Parse (string fullName)
        public string Name {get;set;}
        public string Culture {get;set;}
        public Version Version {get;set;}
        public AssemblyAttributes Attributes {get;set;}
        public bool HasPublicKey / IsSideBySideCompatible / IsRetargetable /
                    IsWindowsRuntime {get;set;}
        public byte [] PublicKey {get;set;}
        public byte [] PublicKeyToken {get;set;}          // computed from PublicKey
        public AssemblyHashAlgorithm HashAlgorithm {get;set;}
        public virtual byte [] Hash {get;set;}
        public string FullName                            // "Name, Version=..., Culture=...,
                                                          //   PublicKeyToken=..."
        public virtual MetadataScopeType MetadataScopeType
    }
    public sealed class AssemblyNameDefinition : AssemblyNameReference {
        public AssemblyNameDefinition (string name, Version version)
    }
    public class ModuleReference : IMetadataScope {
        public ModuleReference (string name)
        public string Name {get;set;}
    }
    public enum MetadataScopeType { AssemblyNameReference, ModuleReference, ModuleDefinition }
    public interface IMetadataScope : IMetadataTokenProvider {
        MetadataScopeType MetadataScopeType { get; }
        string Name { get; set; }
    }

Resources:

    public abstract class Resource {
        public string Name {get;set;}
        public ManifestResourceAttributes Attributes {get;set;}
        public abstract ResourceType ResourceType            // Linked, Embedded,
                                                             //   AssemblyLinked
        public bool IsPublic / IsPrivate {get;set;}
    }
    public sealed class EmbeddedResource : Resource {
        public EmbeddedResource (string name, ManifestResourceAttributes attributes, byte [] data)
        public EmbeddedResource (string name, ManifestResourceAttributes attributes, Stream stream)
        public Stream GetResourceStream ()
        public byte [] GetResourceData ()
    }
    public sealed class LinkedResource : Resource {          // File, Hash
        public LinkedResource (string name, ManifestResourceAttributes flags)
        public LinkedResource (string name, ManifestResourceAttributes flags, string file)
    }
    public sealed class AssemblyLinkedResource : Resource {  // Assembly
        public AssemblyLinkedResource (string name, ManifestResourceAttributes flags)
        public AssemblyLinkedResource (string name, ManifestResourceAttributes flags,
                                       AssemblyNameReference reference)
    }

    public sealed class ExportedType : IMetadataTokenProvider {   // type forwarders and
        public ExportedType (string @namespace, string name,     //   exported nested types
                             ModuleDefinition module, IMetadataScope scope)
        public string Namespace / Name {get;set;}
        public TypeAttributes Attributes {get;set;}
        public IMetadataScope Scope {get;set;}
        public ExportedType DeclaringType {get;set;}
        public int Identifier {get;set;}
        public bool IsForwarder {get;set;}
        public string FullName
        public TypeDefinition Resolve ()
    }

Tokens and the core type system:

    public struct MetadataToken : IEquatable<MetadataToken> {   // ns .Metadata
        public MetadataToken (uint token)
        public MetadataToken (TokenType type)
        public MetadataToken (TokenType type, uint rid)
        public MetadataToken (TokenType type, int rid)
        public static readonly MetadataToken Zero
        public uint RID
        public TokenType TokenType
        public int ToInt32 ()
        public uint ToUInt32 ()
    }
    public enum TokenType : uint {                              // ns .Metadata
        Module, TypeRef, TypeDef, Field, Method, Param, InterfaceImpl, MemberRef,
        CustomAttribute, Permission, Signature, Event, Property, ModuleRef,
        TypeSpec, Assembly, AssemblyRef, File, ExportedType, ManifestResource,
        GenericParam, MethodSpec, GenericParamConstraint, Document,
        MethodDebugInformation, LocalScope, LocalVariable, LocalConstant,
        ImportScope, StateMachineMethod, CustomDebugInformation, String
    }

    public abstract class TypeSystem {                    // module.TypeSystem
        public IMetadataScope CoreLibrary                 // the corlib reference
        public TypeReference Object / Void / Boolean / Char / SByte / Byte /
                             Int16 / UInt16 / Int32 / UInt32 / Int64 / UInt64 /
                             Single / Double / IntPtr / UIntPtr / String /
                             TypedReference
    }

Use `module.TypeSystem.Int32` (etc.) whenever you need a primitive type
reference in THAT module; these are already imported and always correct for
the module's core library (mscorlib, System.Runtime, System.Private.CoreLib
or netstandard, whichever the module references).

Attribute/flag enums (all in CodeBrix.AssemblyTools; values mirror ECMA-335):
TypeAttributes, MethodAttributes, MethodImplAttributes,
MethodSemanticsAttributes, MethodCallingConvention, FieldAttributes,
ParameterAttributes, PropertyAttributes, EventAttributes,
GenericParameterAttributes, GenericParameterType, AssemblyAttributes,
AssemblyHashAlgorithm, ModuleAttributes, ModuleCharacteristics, ModuleKind,
MetadataKind, TargetArchitecture, TargetRuntime, ManifestResourceAttributes,
ResourceType, PInvokeAttributes, NativeType, VariantType, SecurityAction,
MetadataType. Provider interfaces: ICustomAttributeProvider,
ISecurityDeclarationProvider, IConstantProvider, IMarshalInfoProvider,
IMemberDefinition, IMethodSignature, IMetadataTokenProvider,
ICustomDebugInformationProvider, IModifierType.


IMPORTING REFERENCES INTO A MODULE
----------------------------------

Every TypeReference/MethodReference/FieldReference stored in a module's
metadata (as an operand, a field type, a base type, a parameter type...) must
BELONG to that module. A reference obtained from another module -- or from
System.Reflection -- must be imported first. The writer enforces this:
writing a module that contains a foreign member throws
ArgumentException("Member '...' is declared in another module and needs to
be imported").

    // from System.Reflection (the running process's view of a type)
    public TypeReference   ImportReference (Type type)
    public TypeReference   ImportReference (Type type, IGenericParameterProvider context)
    public FieldReference  ImportReference (System.Reflection.FieldInfo field)
    public FieldReference  ImportReference (System.Reflection.FieldInfo field, IGenericParameterProvider context)
    public MethodReference ImportReference (System.Reflection.MethodBase method)
    public MethodReference ImportReference (System.Reflection.MethodBase method, IGenericParameterProvider context)

    // from another CodeBrix.AssemblyTools module
    public TypeReference   ImportReference (TypeReference type)
    public TypeReference   ImportReference (TypeReference type, IGenericParameterProvider context)
    public FieldReference  ImportReference (FieldReference field)
    public FieldReference  ImportReference (FieldReference field, IGenericParameterProvider context)
    public MethodReference ImportReference (MethodReference method)
    public MethodReference ImportReference (MethodReference method, IGenericParameterProvider context)

Facts:
  * Importing a reference that already belongs to the target module returns
    it unchanged, so importing is idempotent and cheap to do defensively.
  * The reflection overloads name the assembly the type lives in AT RUN TIME
    in your tool's process (e.g. `System.Console` / `System.Private.CoreLib`
    on .NET). That reference is added to `AssemblyReferences` as-is. If the
    target assembly must reference a different framework (older
    `mscorlib`, `netstandard`), import from a TypeReference resolved via
    your assembly resolver instead, or use `module.TypeSystem.*` for
    primitives.
  * `context` is the generic owner (a generic type or method) used to
    resolve generic parameters (`T`) appearing in the imported signature.
  * Importing a TypeDefinition yields a TypeReference; the definition
    itself is not copied. To COPY members between modules you must
    construct new definitions and import every type they mention.
  * The older `Import(...)` overloads still exist but are marked
    [Obsolete]; use ImportReference.

Custom importers (rarely needed): IMetadataImporterProvider /
IMetadataImporter (DefaultMetadataImporter) and IReflectionImporterProvider /
IReflectionImporter (DefaultReflectionImporter), set via ReaderParameters or
ModuleParameters. Override `ImportReference(AssemblyNameReference)` in a
subclass to redirect framework references.


RESOLVING REFERENCES
--------------------

    TypeReference.Resolve ()    -> TypeDefinition   (or null)
    MethodReference.Resolve ()  -> MethodDefinition (or null)
    FieldReference.Resolve ()   -> FieldDefinition  (or null)
    MemberReference.Resolve ()  -> IMemberDefinition (or null)
    ExportedType.Resolve ()     -> TypeDefinition   (follows the forwarder)

Resolve() uses `Module.MetadataResolver`, which uses `Module.AssemblyResolver`
to open the assembly named by the reference's Scope, then looks the member
up by name and signature.

  * It returns NULL when the assembly was opened but the type/member is not
    in it (version skew, trimmed assembly, wrong search directory picked
    up an unrelated file of the same name). Always null-check.
  * It THROWS AssemblyResolutionException (a FileNotFoundException) when the
    assembly itself cannot be located.
  * A GenericParameter resolves to null (it is not a type). A
    TypeSpecification (array, pointer, generic instance...) resolves to
    the definition of its element type.
  * Calling Resolve() on a definition returns the definition itself.
  * Resolution requires the SAME resolver instance to stay alive: the
    returned definitions belong to assemblies cached inside the resolver
    and die when it is disposed.


THE IL MODEL
------------

    public sealed class MethodBody {                     // ns .Cil
        public MethodBody (MethodDefinition method)
        public MethodDefinition Method
        public int MaxStackSize {get;set;}               // recomputed by the writer
        public int CodeSize
        public bool InitLocals {get;set;}
        public MetadataToken LocalVarToken {get;set;}
        public Collection<Instruction> Instructions
        public bool HasExceptionHandlers
        public Collection<ExceptionHandler> ExceptionHandlers
        public bool HasVariables
        public Collection<VariableDefinition> Variables
        public ParameterDefinition ThisParameter         // for instance methods
        public ILProcessor GetILProcessor ()
    }

Always mutate instructions through an ILProcessor: it keeps
`Instruction.Previous/Next` linked and, when a symbol reader is attached,
fixes up the debug scopes that refer to the instructions you move.

    public sealed class ILProcessor {
        public MethodBody Body
        // Create: build an Instruction without adding it
        public Instruction Create (OpCode opcode)
        public Instruction Create (OpCode opcode, TypeReference type)
        public Instruction Create (OpCode opcode, MethodReference method)
        public Instruction Create (OpCode opcode, FieldReference field)
        public Instruction Create (OpCode opcode, CallSite site)
        public Instruction Create (OpCode opcode, string value)
        public Instruction Create (OpCode opcode, sbyte value)
        public Instruction Create (OpCode opcode, byte value)
        public Instruction Create (OpCode opcode, int value)
        public Instruction Create (OpCode opcode, long value)
        public Instruction Create (OpCode opcode, float value)
        public Instruction Create (OpCode opcode, double value)
        public Instruction Create (OpCode opcode, Instruction target)      // branches
        public Instruction Create (OpCode opcode, Instruction [] targets)  // switch
        public Instruction Create (OpCode opcode, VariableDefinition variable)
        public Instruction Create (OpCode opcode, ParameterDefinition parameter)
        // Emit: Create + Append, same 16 overloads
        public void Emit (OpCode opcode)
        public void Emit (OpCode opcode, TypeReference type)
        public void Emit (OpCode opcode, MethodReference method)
        public void Emit (OpCode opcode, FieldReference field)
        public void Emit (OpCode opcode, CallSite site)
        public void Emit (OpCode opcode, string value)
        public void Emit (OpCode opcode, sbyte value)
        public void Emit (OpCode opcode, byte value)
        public void Emit (OpCode opcode, int value)
        public void Emit (OpCode opcode, long value)
        public void Emit (OpCode opcode, float value)
        public void Emit (OpCode opcode, double value)
        public void Emit (OpCode opcode, Instruction target)
        public void Emit (OpCode opcode, Instruction [] targets)
        public void Emit (OpCode opcode, VariableDefinition variable)
        public void Emit (OpCode opcode, ParameterDefinition parameter)
        // structural edits
        public void InsertBefore (Instruction target, Instruction instruction)
        public void InsertAfter (Instruction target, Instruction instruction)
        public void InsertAfter (int index, Instruction instruction)
        public void Append (Instruction instruction)
        public void Replace (Instruction target, Instruction instruction)
        public void Replace (int index, Instruction instruction)
        public void Remove (Instruction instruction)
        public void RemoveAt (int index)
        public void Clear ()
    }

The operand type must match the opcode's OperandType (e.g. `OpCodes.Ldstr`
takes a string, `OpCodes.Call` a MethodReference, `OpCodes.Ldc_I4` an int,
`OpCodes.Ldc_I4_S` an sbyte, `OpCodes.Br_S` an Instruction, `OpCodes.Switch`
an Instruction[]). Mismatches are ArgumentException at Create time.

    public sealed class Instruction {
        public int Offset {get;set;}                     // valid after read / write;
                                                         //   stale after edits
        public OpCode OpCode {get;set;}
        public object Operand {get;set;}
        public Instruction Previous / Next {get;set;}
        public Instruction GetPrototype ()               // new Instruction(OpCode, Operand)
        public int GetSize ()
        public override string ToString ()               // "IL_0004: call ..."
        public static Instruction Create (OpCode opcode)  // same 16 overloads as
                                                          //   ILProcessor.Create
    }

    public struct OpCode : IEquatable<OpCode> {
        public string Name
        public int Size                                  // 1 or 2 bytes
        public byte Op1 / Op2
        public short Value
        public Code Code                                 // enum, one member per opcode
        public FlowControl FlowControl                   // Branch, Break, Call, Cond_Branch,
                                                         //   Meta, Next, Phi, Return, Throw
        public OpCodeType OpCodeType
        public OperandType OperandType
        public StackBehaviour StackBehaviourPop / StackBehaviourPush
        // == and != are defined
    }
    public static class OpCodes { public static readonly OpCode Nop, Ldarg_0, ...,
                                  Ldstr, Call, Callvirt, Newobj, Ret, Br, Br_S,
                                  Brtrue, Brfalse, Ldloc, Stloc, Ldfld, Stfld,
                                  Ldsfld, Stsfld, Box, Unbox_Any, Castclass, Isinst,
                                  Ldtoken, Throw, Leave, Leave_S, Endfinally,
                                  Ldnull, Dup, Pop, Ldc_I4, Ldc_I4_S, Ldc_I8,
                                  Ldc_R4, Ldc_R8, Switch, Calli ... (219 fields) }
    public enum Code { Nop, Break, Ldarg_0, ... }        // compare instr.OpCode.Code

    public sealed class VariableDefinition : VariableReference {
        public VariableDefinition (TypeReference variableType)
        public TypeReference VariableType {get;set;}
        public int Index                                 // position in Body.Variables
        public bool IsPinned
    }

    public sealed class ExceptionHandler {
        public ExceptionHandler (ExceptionHandlerType handlerType)
        public Instruction TryStart / TryEnd {get;set;}          // TryEnd is EXCLUSIVE
        public Instruction FilterStart {get;set;}
        public Instruction HandlerStart / HandlerEnd {get;set;}  // HandlerEnd EXCLUSIVE
                                                                 //   (null = end of method)
        public TypeReference CatchType {get;set;}
        public ExceptionHandlerType HandlerType {get;set;}
    }
    public enum ExceptionHandlerType { Catch = 0, Filter = 1, Finally = 2, Fault = 4 }

Rocks for IL (namespace CodeBrix.AssemblyTools.Rocks, extension methods):

    public static void SimplifyMacros (this MethodBody self)   // Br_S -> Br, Ldloc_0 ->
                                                               //   Ldloc, Ldc_I4_S -> Ldc_I4
    public static void OptimizeMacros (this MethodBody self)   // the reverse
    public static void Optimize (this MethodBody self)         // OptimizeMacros + shrink
                                                               //   int-range Ldc_I8
    public static void Parse (MethodDefinition method, IILVisitor visitor)  // ILParser;
                                    // IILVisitor has 16 OnInlineXxx callbacks, one
                                    // per operand kind

Pattern for safe IL rewriting: `body.SimplifyMacros()` -> insert/remove ->
`body.OptimizeMacros()`. Short-form branches (`Br_S`, `Leave_S`...) have a
one-byte signed offset; inserting code that pushes a target out of range
without simplifying first produces an unverifiable method.


DEBUG INFORMATION MODEL
-----------------------

    public sealed class MethodDebugInformation : DebugInformation {   // ns .Cil
        public MethodDefinition Method
        public bool HasSequencePoints
        public Collection<SequencePoint> SequencePoints
        public ScopeDebugInformation Scope {get;set;}
        public MethodDefinition StateMachineKickOffMethod {get;set;}
        public SequencePoint GetSequencePoint (Instruction instruction)
        public IDictionary<Instruction, SequencePoint> GetSequencePointMapping ()
        public IEnumerable<ScopeDebugInformation> GetScopes ()
        public bool TryGetName (VariableDefinition variable, out string name)
    }
    public sealed class SequencePoint {
        public SequencePoint (Instruction instruction, Document document)
        public int Offset                                // tracks the instruction
        public int StartLine / StartColumn / EndLine / EndColumn {get;set;}
        public bool IsHidden                             // StartLine == 0xfeefee
        public Document Document {get;set;}
    }
    public sealed class Document : DebugInformation {
        public Document (string url)
        public string Url {get;set;}
        public DocumentType Type {get;set;}
        public DocumentHashAlgorithm HashAlgorithm {get;set;}   // None, MD5, SHA1, SHA256
        public DocumentLanguage Language {get;set;}             // C, Cpp, CSharp, Basic, ...
        public DocumentLanguageVendor LanguageVendor {get;set;}
        public Guid TypeGuid / HashAlgorithmGuid / LanguageGuid / LanguageVendorGuid {get;set;}
        public byte [] Hash {get;set;}
        public byte [] EmbeddedSource {get;set;}
    }
    public sealed class ScopeDebugInformation : DebugInformation {
        public ScopeDebugInformation (Instruction start, Instruction end)
        public InstructionOffset Start / End {get;set;}
        public ImportDebugInformation Import {get;set;}          // usings
        public bool HasScopes
        public Collection<ScopeDebugInformation> Scopes
        public bool HasVariables
        public Collection<VariableDebugInformation> Variables    // local NAMES
        public bool HasConstants
        public Collection<ConstantDebugInformation> Constants
        public bool TryGetName (VariableDefinition variable, out string name)
    }
    public sealed class VariableDebugInformation : DebugInformation {
        public VariableDebugInformation (VariableDefinition variable, string name)
    }
    public struct InstructionOffset {                    // Offset, IsEndOfMethod
        public InstructionOffset (Instruction instruction)
        public InstructionOffset (int offset)
    }
    public abstract class CustomDebugInformation : DebugInformation
        // subclasses: BinaryCustomDebugInformation, StateMachineScopeDebugInformation,
        //   AsyncMethodBodyDebugInformation, EmbeddedSourceDebugInformation,
        //   SourceLinkDebugInformation; kind via CustomDebugInformationKind
        //   { Binary, StateMachineScope, DynamicVariable, DefaultNamespace,
        //     AsyncMethodBody, EmbeddedSource, SourceLink }
    public sealed class ImageDebugHeader { HasEntries, Entries }  // the PE debug
    public sealed class ImageDebugHeaderEntry { Directory, Data } //   directory
    public struct ImageDebugDirectory { Type, TimeDateStamp, MajorVersion, ... }

Local variable NAMES live only in the PDB (`Scope.Variables`), never in the
IL; `VariableDefinition` has no Name. To name a new local for debuggers, add
`new VariableDebugInformation(variable, "name")` to the enclosing scope.


WRITING ASSEMBLIES AND MODULES
------------------------------

    // AssemblyDefinition (forwards to MainModule) and ModuleDefinition
    public void Write (string fileName)
    public void Write (string fileName, WriterParameters parameters)
    public void Write ()                                 // in place: requires the module
                                                         //   to have been read with
                                                         //   ReadWrite = true
    public void Write (WriterParameters parameters)
    public void Write (Stream stream)                    // stream must be writable AND
                                                         //   seekable; not disposed for you
    public void Write (Stream stream, WriterParameters parameters)

    public sealed class WriterParameters {
        public uint? Timestamp {get;set;}                // PE header timestamp; null
                                                         //   keeps the input's value
        public bool WriteSymbols {get;set;}              // use DefaultSymbolWriterProvider
                                                         //   when SymbolWriterProvider is null
        public ISymbolWriterProvider SymbolWriterProvider {get;set;}
        public Stream SymbolStream {get;set;}            // write the PDB here instead of
                                                         //   the sidecar path
        public bool DeterministicMvid {get;set;}         // MVID = hash of the output
        public byte [] StrongNameKeyBlob {get;set;}      // contents of a .snk (managed,
                                                         //   cross-platform)
        public string StrongNameKeyContainer {get;set;}  // CSP container (Windows)
        public System.Reflection.StrongNameKeyPair StrongNameKeyPair {get;set;}
        public bool HasStrongNameKey                     // any of the three is set
    }

What Write does, in order: fully reads a Deferred module; disposes the
module's symbol reader; picks a symbol writer; rebuilds ALL metadata tables
and heaps from the object model (tokens are reassigned, so
`MetadataToken` values you captured before the write are not stable);
computes MaxStackSize and instruction offsets; emits the PE; optionally
computes a deterministic MVID and applies a strong-name signature.

Symbol writer selection:

    DefaultSymbolWriterProvider          // requires module.SymbolReader != null: it
                                         //   asks the READER which writer matches
                                         //   (portable -> portable, embedded ->
                                         //   embedded, native -> native, mdb -> mdb).
                                         //   Throws InvalidOperationException when
                                         //   no symbols were read.
    PortablePdbWriterProvider            // ns .Cil: sidecar portable .pdb
    EmbeddedPortablePdbWriterProvider    // ns .Cil: portable PDB embedded in the image
    Pdb.PdbWriterProvider                // portable if the reader was portable,
                                         //   else native
    Pdb.NativePdbWriterProvider          // native Windows PDB (Windows only; the
                                         //   Stream overload is not implemented)
    Mdb.MdbWriterProvider                // Mono .mdb

To write symbols for an assembly you did NOT read symbols for (e.g. one you
built from scratch), set `SymbolWriterProvider = new PortablePdbWriterProvider()`
explicitly instead of `WriteSymbols = true`.

Constraints:
  * Mixed-mode (C++/CLI, non-ILOnly) images cannot be written:
    NotSupportedException("Writing mixed-mode assemblies is not supported").
    They can be READ.
  * Writing produces only the managed PE (and PDB). No apphost `.exe`,
    `runtimeconfig.json` or `deps.json` is generated -- an executable you
    build from scratch needs those from the SDK to run.
  * Strong-name signing with `StrongNameKeyPair` relies on
    `System.Reflection.StrongNameKeyPair` serialization; prefer
    `StrongNameKeyBlob` (`File.ReadAllBytes("key.snk")`). Delay-signed
    outputs are your responsibility (set the public key on `Name.PublicKey`
    and leave the signature blank).


CREATING ASSEMBLIES FROM SCRATCH
--------------------------------

    public static AssemblyDefinition CreateAssembly (AssemblyNameDefinition assemblyName,
                                                     string moduleName, ModuleKind kind)
    public static AssemblyDefinition CreateAssembly (AssemblyNameDefinition assemblyName,
                                                     string moduleName, ModuleParameters parameters)
    public static ModuleDefinition CreateModule (string name, ModuleKind kind)
    public static ModuleDefinition CreateModule (string name, ModuleParameters parameters)

    public sealed class ModuleParameters {
        public ModuleParameters ()                       // Dll, Net_4_0, current arch
        public ModuleKind Kind {get;set;}
        public TargetRuntime Runtime {get;set;}
        public uint? Timestamp {get;set;}
        public TargetArchitecture Architecture {get;set;}
        public IAssemblyResolver AssemblyResolver {get;set;}
        public IMetadataResolver MetadataResolver {get;set;}
        public IMetadataImporterProvider MetadataImporterProvider {get;set;}
        public IReflectionImporterProvider ReflectionImporterProvider {get;set;}
    }

A new module already contains the `<Module>` global type at `Types[0]`, so
your first added type is `Types[1]`. `CreateModule("Foo.dll", ...)` also
creates the owning AssemblyDefinition named "Foo" (extension stripped)
unless Kind is NetModule. Set `assembly.EntryPoint` for Console/Windows
kinds. A brand-new module's `TypeSystem.CoreLibrary` refers to the core
library of the RUNNING runtime, and reflection imports follow suit (see
IMPORTING REFERENCES).


ROCKS: EXTENSION METHODS
------------------------

All in namespace CodeBrix.AssemblyTools.Rocks (add the using):

    // ModuleDefinitionRocks
    public static IEnumerable<TypeDefinition> GetAllTypes (this ModuleDefinition self)
        // every type incl. nested -- same result as GetTypes(), kept for
        //   upstream compatibility
    // TypeDefinitionRocks
    public static IEnumerable<MethodDefinition> GetConstructors (this TypeDefinition self)
    public static MethodDefinition GetStaticConstructor (this TypeDefinition self)
    public static IEnumerable<MethodDefinition> GetMethods (this TypeDefinition self)
        // non-constructor methods
    public static TypeReference GetEnumUnderlyingType (this TypeDefinition self)
    // MethodDefinitionRocks
    public static MethodDefinition GetBaseMethod (this MethodDefinition self)
        // the overridden method one level up (self if none)
    public static MethodDefinition GetOriginalBaseMethod (this MethodDefinition self)
        // walks to the root of the override chain
    // TypeReferenceRocks
    public static ArrayType MakeArrayType (this TypeReference self)
    public static ArrayType MakeArrayType (this TypeReference self, int rank)
    public static PointerType MakePointerType (this TypeReference self)
    public static ByReferenceType MakeByReferenceType (this TypeReference self)
    public static OptionalModifierType MakeOptionalModifierType (this TypeReference self,
                                                                 TypeReference modifierType)
    public static RequiredModifierType MakeRequiredModifierType (this TypeReference self,
                                                                 TypeReference modifierType)
    public static GenericInstanceType MakeGenericInstanceType (this TypeReference self,
                                                               params TypeReference [] arguments)
    public static PinnedType MakePinnedType (this TypeReference self)
    public static SentinelType MakeSentinelType (this TypeReference self)
    // MethodBodyRocks -- see THE IL MODEL
    // ParameterReferenceRocks
    public static int GetSequence (this ParameterReference self)
    // DocCommentId
    public static string GetDocCommentId (IMemberDefinition member)
        // "T:Ns.Type", "M:Ns.Type.Method(System.Int32)" -- the XML-doc id

Instantiating a generic method reference: wrap the open MethodReference in
`new GenericInstanceMethod(method)` and add to `GenericArguments`. For a
method ON a generic instance type (e.g. `List<int>.Add`), create a
`new MethodReference(name, returnType, genericInstanceType) { HasThis = true }`
and add ParameterDefinitions whose types use the type's own GenericParameters
(`listDef.GenericParameters[0]`), then ImportReference it.


COLLECTIONS
-----------

    public class Collection<T> : IList<T>, IList {       // ns .Collections.Generic
        public Collection ()  /  Collection (int capacity)  /  Collection (ICollection<T> items)
        public int Count
        public T this [int index] {get;set;}
        public int Capacity {get;set;}
        public void Add (T item)
        public bool Contains (T item)
        public int IndexOf (T item)
        public void Insert (int index, T item)
        public void RemoveAt (int index)
        public bool Remove (T item)
        public void Clear ()
        public void CopyTo (T [] array, int arrayIndex)
        public T [] ToArray ()
        public Enumerator GetEnumerator ()               // struct enumerator
    }
    public sealed class ReadOnlyCollection<T> : Collection<T>   // Empty, wraps arrays

The model's collections are NOT thread-safe and are not snapshotted: do not
Add/Remove from a collection while `foreach`-ing over it -- copy with
`.ToList()` first. Type/method/field collections wire up `DeclaringType` /
`Module` on Add and clear them on Remove.


COMPLETE EXAMPLES
=================

1. Read an assembly, find a method, inject a call at its start, write with
   symbols (the canonical instrumentation pipeline):

    using System;
    using System.IO;
    using System.Linq;
    using CodeBrix.AssemblyTools;
    using CodeBrix.AssemblyTools.Cil;
    using CodeBrix.AssemblyTools.Rocks;

    static void Instrument (string inputPath, string outputPath)
    {
        var resolver = new DefaultAssemblyResolver ();
        resolver.AddSearchDirectory (Path.GetDirectoryName (Path.GetFullPath (inputPath)));

        var readerParameters = new ReaderParameters {
            AssemblyResolver = resolver,
            ReadSymbols = true,                           // portable/embedded/native/mdb
            SymbolReaderProvider = new DefaultSymbolReaderProvider (throwIfNoSymbol: false),
        };

        using (resolver)
        using (var assembly = AssemblyDefinition.ReadAssembly (inputPath, readerParameters)) {
            var module = assembly.MainModule;

            var type = module.GetType ("MyApp.OrderService");   // null if absent
            var method = type.Methods.First (m => m.Name == "PlaceOrder" && m.HasBody);

            var writeLine = module.ImportReference (
                typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }));

            var body = method.Body;
            body.SimplifyMacros ();                       // widen short branches

            var il = body.GetILProcessor ();
            var first = body.Instructions [0];
            il.InsertBefore (first, il.Create (OpCodes.Ldstr, "Entering " + method.FullName));
            il.InsertBefore (first, il.Create (OpCodes.Call, writeLine));

            body.OptimizeMacros ();                       // re-shorten where legal

            var writerParameters = new WriterParameters {
                WriteSymbols = module.HasSymbols,         // only if symbols were read
            };
            assembly.Write (outputPath, writerParameters);
        }
    }

   Notes: existing exception handlers and sequence points that referenced
   `first` now start at the injected `Ldstr` because InsertBefore keeps the
   target instruction in place and links the new one before it. Branches
   TO `first` still land on `first` (branch operands are Instruction
   references, not offsets).

2. Walk everything in a module, including nested types and IL, without
   holding the file open:

    using var module = ModuleDefinition.ReadModule (path, new ReaderParameters {
        ReadingMode = ReadingMode.Immediate,
        InMemory = true,
    });

    foreach (var type in module.GetTypes ()) {            // includes nested types
        Console.WriteLine ($"{type.FullName}  base={type.BaseType?.FullName}");
        foreach (var field in type.Fields)
            Console.WriteLine ($"  {field.FieldType.FullName} {field.Name}" +
                               (field.HasConstant ? $" = {field.Constant}" : ""));
        foreach (var method in type.Methods) {
            Console.WriteLine ($"  {method.FullName}");
            if (!method.HasBody)
                continue;
            foreach (var instruction in method.Body.Instructions)
                Console.WriteLine ($"    {instruction}");
            foreach (var handler in method.Body.ExceptionHandlers)
                Console.WriteLine ($"    {handler.HandlerType} {handler.CatchType?.FullName}" +
                                   $" try {handler.TryStart.Offset}-{handler.TryEnd.Offset}");
        }
    }

3. Build a console assembly from nothing and write it with a portable PDB:

    var name = new AssemblyNameDefinition ("Hello", new Version (1, 0, 0, 0));
    using var assembly = AssemblyDefinition.CreateAssembly (name, "Hello.dll", ModuleKind.Console);
    var module = assembly.MainModule;

    var program = new TypeDefinition ("Hello", "Program",
        TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract |
        TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
        module.TypeSystem.Object);
    module.Types.Add (program);

    var main = new MethodDefinition ("Main",
        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
        module.TypeSystem.Void);
    main.Parameters.Add (new ParameterDefinition ("args", ParameterAttributes.None,
        module.TypeSystem.String.MakeArrayType ()));      // Rocks
    program.Methods.Add (main);

    var il = main.Body.GetILProcessor ();                 // Body is created on demand
    var counter = new VariableDefinition (module.TypeSystem.Int32);
    main.Body.Variables.Add (counter);
    main.Body.InitLocals = true;

    var writeLine = module.ImportReference (
        typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }));

    il.Emit (OpCodes.Ldstr, "Hello from CodeBrix.AssemblyTools");
    il.Emit (OpCodes.Call, writeLine);
    il.Emit (OpCodes.Ldc_I4, 42);
    il.Emit (OpCodes.Stloc, counter);
    il.Emit (OpCodes.Ret);

    assembly.EntryPoint = main;
    assembly.Write ("Hello.dll", new WriterParameters {
        SymbolWriterProvider = new PortablePdbWriterProvider (),   // explicit: nothing
    });                                                            //   was read

   (To run it, copy a matching `Hello.runtimeconfig.json` next to it and use
   `dotnet Hello.dll`; this package does not generate host files.)

4. Add a try/finally around an existing body:

    var body = method.Body;
    body.SimplifyMacros ();
    var il = body.GetILProcessor ();

    // every `ret` must become `leave <after>`; collect them first
    var rets = body.Instructions.Where (i => i.OpCode == OpCodes.Ret).ToList ();
    var ret = il.Create (OpCodes.Ret);
    var endFinally = il.Create (OpCodes.Endfinally);
    il.Append (endFinally);
    il.Append (ret);
    foreach (var r in rets)
        il.Replace (r, il.Create (OpCodes.Leave, ret));   // Replace keeps branch
                                                          //   targets pointing at
                                                          //   the new instruction
    var cleanup = module.ImportReference (typeof (Console).GetMethod ("Out").GetGetMethod ());
    var flush = module.ImportReference (typeof (TextWriter).GetMethod ("Flush"));
    il.InsertBefore (endFinally, il.Create (OpCodes.Call, cleanup));
    il.InsertBefore (endFinally, il.Create (OpCodes.Callvirt, flush));

    body.ExceptionHandlers.Add (new ExceptionHandler (ExceptionHandlerType.Finally) {
        TryStart = body.Instructions [0],
        TryEnd = body.Instructions.First (i => i.OpCode == OpCodes.Call && i.Operand == cleanup),
        HandlerStart = body.Instructions.First (i => i.OpCode == OpCodes.Call && i.Operand == cleanup),
        HandlerEnd = ret,
    });
    body.OptimizeMacros ();

5. Read and edit a custom attribute; add a new one:

    var obsolete = type.CustomAttributes
        .FirstOrDefault (a => a.AttributeType.FullName == "System.ObsoleteAttribute");
    if (obsolete != null && obsolete.HasConstructorArguments)
        Console.WriteLine ((string) obsolete.ConstructorArguments [0].Value);

    var ctor = module.ImportReference (
        typeof (System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor (Type.EmptyTypes));
    type.CustomAttributes.Add (new CustomAttribute (ctor));

    var descCtor = module.ImportReference (
        typeof (System.ComponentModel.DescriptionAttribute).GetConstructor (new [] { typeof (string) }));
    var desc = new CustomAttribute (descCtor);
    desc.ConstructorArguments.Add (new CustomAttributeArgument (module.TypeSystem.String, "rewritten"));
    method.CustomAttributes.Add (desc);

6. Rewrite a file in place:

    using (var module = ModuleDefinition.ReadModule (path, new ReaderParameters { ReadWrite = true })) {
        module.Assembly.Name.Version = new Version (2, 0, 0, 0);
        module.Write ();                                  // writes back to `path`
    }

7. Resolve across assemblies and call a generic method on a generic type:

    var listDef = module.ImportReference (typeof (System.Collections.Generic.List<>)).Resolve ();
    var listOfInt = module.ImportReference (listDef).MakeGenericInstanceType (module.TypeSystem.Int32);

    var addDef = listDef.Methods.First (m => m.Name == "Add");
    var add = new MethodReference (addDef.Name, addDef.ReturnType, listOfInt) { HasThis = true };
    add.Parameters.Add (new ParameterDefinition (listDef.GenericParameters [0]));
    var addRef = module.ImportReference (add);           // List<int>::Add(!0)

    var listCtor = module.ImportReference (
        new MethodReference (".ctor", module.TypeSystem.Void, listOfInt) { HasThis = true });

    il.Emit (OpCodes.Newobj, listCtor);
    il.Emit (OpCodes.Dup);
    il.Emit (OpCodes.Ldc_I4, 7);
    il.Emit (OpCodes.Callvirt, addRef);
    il.Emit (OpCodes.Pop);


MINIMUM VIABLE PROJECT
======================

    Rewriter.csproj
    ----------------
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.AssemblyTools.MitLicenseForever" />
      </ItemGroup>
    </Project>

    Program.cs
    ----------
    using System;
    using System.IO;
    using CodeBrix.AssemblyTools;
    using CodeBrix.AssemblyTools.Cil;

    var input = args [0];
    var output = args.Length > 1 ? args [1] : Path.ChangeExtension (input, ".rewritten.dll");

    var resolver = new DefaultAssemblyResolver ();
    resolver.AddSearchDirectory (Path.GetDirectoryName (Path.GetFullPath (input)));

    using (resolver)
    using (var assembly = AssemblyDefinition.ReadAssembly (input,
               new ReaderParameters { AssemblyResolver = resolver })) {
        foreach (var type in assembly.MainModule.GetTypes ())
            foreach (var method in type.Methods)
                if (method.HasBody)
                    Console.WriteLine ($"{method.FullName}: {method.Body.Instructions.Count} instructions");

        assembly.Write (output);
    }

(Use `dotnet add package CodeBrix.AssemblyTools.MitLicenseForever` to pin
the current version in the csproj; `<Nullable>disable</Nullable>` is only a
suggestion -- the package's API returns null in the documented cases and
carries no nullability annotations.)


PERFORMANCE TIPS
================

  * Keep `ReadingMode.Deferred` (the default) when you touch a fraction of
    the assembly: each type/method/body is decoded on first access only.
    Use `ReadingMode.Immediate` when you will visit everything, or when
    you need to close the input stream early -- Immediate also drops the
    internal metadata caches after reading.
  * `InMemory = true` costs one file-sized allocation up front but frees
    the file handle immediately and turns all later page reads into memory
    reads; it is the right choice when scanning many assemblies in a loop
    or when the file must remain writable/deletable by others.
  * Test `HasXxx` (HasBody, HasCustomAttributes, HasNestedTypes, ...) before
    touching the collection: the `Has` properties answer from the metadata
    row counts without decoding anything.
  * `module.Types` is top-level only and cheap; `GetTypes()` /
    `GetAllTypes()` walk nested types recursively. `GetType("Ns.Name")` is a
    dictionary lookup -- prefer it over a LINQ scan for a known name.
  * Share ONE DefaultAssemblyResolver across all modules you process: it
    caches resolved assemblies by full name, so repeated `Resolve()` calls
    into the same framework assembly open it once.
  * Use `il.Create` + `InsertBefore/InsertAfter` in a batch rather than
    `Replace` one-by-one; each structural edit re-links neighbours and, when
    symbols are loaded, fixes up debug scopes.
  * Writing always rebuilds every table; for large assemblies the write
    dominates. Write once at the end, not after each edit.
  * Custom attributes are decoded lazily per attribute (`IsResolved`); the
    `AttributeType.FullName` check needs no decoding, so filter on it before
    reading `ConstructorArguments`.


COMMON PITFALLS TO AVOID
========================

  * File locking / "file in use": the `ReadModule(string)` /
    `ReadAssembly(string)` overloads keep the input file open until you
    Dispose the module. Wrap every module/assembly in `using`, or read with
    `InMemory = true` if you must delete/overwrite the input while the
    model is alive.
  * Writing to the path you read from: `Write(inputPath)` on a module that
    was opened read-only tries to recreate the file the module still holds
    open. Either write to a DIFFERENT path, or open with
    `new ReaderParameters { ReadWrite = true }` and call the parameterless
    `Write()` (or `Write(WriterParameters)`) to save in place.
  * `Write()` on a module opened without `ReadWrite = true` fails
    ("Stream must be writable and seekable" / InvalidOperationException
    for a module with no image); `Write()` on a from-scratch module has no
    target -- use `Write(path)`.
  * Resolver search directories are "." and "bin" RELATIVE TO THE CURRENT
    WORKING DIRECTORY by default. Always `AddSearchDirectory` the folder
    of the assembly you are rewriting and the folders holding its
    references (framework reference pack, NuGet package outputs).
  * The resolver's last fallback is the framework of the process RUNNING
    your tool, not the target's. A tool on .NET 10 rewriting a .NET
    Framework or netstandard assembly will silently resolve
    `System.Object` to `System.Private.CoreLib` of the running runtime
    unless the target's reference assemblies come first in the search
    list.
  * `ImportReference(typeof(X))` references the RUNNING runtime's assembly
    for X. That is correct when the target runs on the same runtime; for
    other targets, import from a TypeReference/MethodReference resolved
    from the target's own references, or use `module.TypeSystem.*`.
  * Forgetting to import: using a TypeReference/MethodReference from
    another module (including a `Resolve()`d definition, which belongs to
    the OTHER assembly) as an operand or member type compiles fine and
    fails only at `Write` with "Member '...' is declared in another module
    and needs to be imported". Import everything foreign, always.
  * `Resolve()` returns null when the member is missing from the located
    assembly and THROWS AssemblyResolutionException when the assembly is
    not found. Handle both; do not assume a non-null result.
  * Symbol mismatch: a stale `.pdb` beside the input throws
    SymbolsNotMatchingException by default. Set
    `ThrowIfSymbolsAreNotMatching = false` to proceed without symbols, and
    then do NOT set `WriteSymbols = true` (there is no reader for the
    DefaultSymbolWriterProvider to mirror -- InvalidOperationException).
  * `WriteSymbols = true` without having read symbols throws
    InvalidOperationException; pick an explicit `SymbolWriterProvider`
    for assemblies built from scratch.
  * Native PDB WRITING is Windows-only (COM). Reading native PDBs works
    everywhere. Convert by reading native and writing with
    `PortablePdbWriterProvider`.
  * Short-form branches: inserting instructions into a body that uses
    `Br_S`/`Leave_S`/`Ldloc_S` etc. can push targets out of the 1-byte
    range. Call `body.SimplifyMacros()` before editing and
    `body.OptimizeMacros()` after.
  * `Instruction.Offset` is only meaningful right after a read or write;
    after edits it is stale until the next write. Compare instructions by
    reference, not by offset.
  * `ExceptionHandler.TryEnd` and `HandlerEnd` are EXCLUSIVE (the first
    instruction after the block). Inserting before `TryEnd` puts the new
    instruction INSIDE the protected region; inserting before
    `HandlerStart` puts it inside the try block, not the handler.
  * Never mutate a `Collection<T>` while iterating it with `foreach`; copy
    with `.ToList()` first. Removing an instruction that is still a branch
    target or a handler boundary leaves a dangling reference that fails at
    write time or produces invalid IL.
  * `MetadataToken` values are reassigned on write; do not persist them
    across a write or use them as stable keys. Use `FullName`.
  * `module.Types` does not contain nested types; `GetType("Outer/Nested")`
    (slash separator) or `GetTypes()` does.
  * Netmodules: `ReadAssembly` throws ArgumentException on a module without
    an assembly manifest; use `ReadModule` and check `module.Assembly`.
  * Mixed-mode (C++/CLI) images read fine but cannot be written
    (NotSupportedException).
  * A property/event added to a type does not add its accessor methods to
    `type.Methods`; add them explicitly or the property points at methods
    that do not exist in the output.
  * Nullable reference types: the package has NRT disabled. Members
    documented as "or null" really return null; consumers with
    `<Nullable>enable</Nullable>` get no warnings and should null-check by
    hand.


WHAT THIS PACKAGE DOES NOT DO
=============================

  * It does not load or execute code. Nothing here touches
    AssemblyLoadContext, Reflection.Emit or the JIT; it is a file-format
    library. To run what you wrote, use the normal .NET host.
  * It does not verify IL. You can write an unbalanced stack, a branch to a
    removed instruction or a wrong-typed operand and get a file that
    fails at run time. (MaxStackSize is computed for you; correctness is
    not.)
  * It does not decompile to C#, and it does not assemble from IL text
    (there is no ilasm/ildasm equivalent). `Instruction.ToString()` gives
    a readable listing only.
  * It does not generate host files: no apphost `.exe`, no
    `runtimeconfig.json`, no `deps.json`, no NuGet packaging.
  * It does not merge assemblies or copy members between modules for you;
    you build new definitions and import every referenced type.
  * It does not write mixed-mode (native + managed) images, and it does not
    write native Windows PDBs on non-Windows hosts.
  * It does not model Windows Runtime (.winmd) projections unless
    `ApplyWindowsRuntimeProjections = true`, and it does not author
    .winmd files.
  * It does not sign with certificates (Authenticode); only strong-name
    signing is supported.
  * It does not provide a thread-safe model: one module per thread, or
    external locking.


WORKING EXAMPLES ON GITHUB
==========================

The test project exercises every area above against real fixture
assemblies. Base URL:
https://github.com/ellisnet/CodeBrix.AssemblyTools/tree/main/tests/CodeBrix.AssemblyTools.Tests

    SmokeTests.cs                  CreateModule/CreateAssembly, TypeSystem, IL emission,
                                   MetadataToken, OpCodes, Collection<T> basics
    Core/ILProcessorTests.cs       Create/Append/InsertBefore/InsertAfter/Replace/
                                   Remove/Clear; debug-scope fix-ups on edits
    Core/MethodBodyTests.cs        Instructions, Variables, ExceptionHandlers, locals,
                                   SimplifyMacros/OptimizeMacros round trips
    Core/ModuleTests.cs            ReadModule overloads, ReadingMode, ReadWrite in-place
                                   write, ImmediateRead, GetType/GetTypes, resources,
                                   exported types, MVID
    Core/AssemblyTests.cs          ReadAssembly, Name/Version/PublicKey, DefaultSymbol-
                                   ReaderProvider(throwIfNoSymbol: false)
    Core/ResolveTests.cs           Resolve() for types/methods/fields, custom
                                   BaseAssemblyResolver subclass, search directories,
                                   AssemblyResolutionException
    Core/ImportDefinitionTests.cs  ImportReference of TypeReference/MethodReference/
                                   FieldReference incl. generic contexts
    Core/ImportReflectionTests.cs  ImportReference of Type/MethodBase/FieldInfo incl.
                                   custom modifiers
    Core/CustomAttributesTests.cs  ConstructorArguments, Fields, Properties, enums,
                                   arrays, boxed args, GetBlob
    Core/TypeTests.cs              TypeDefinition/Reference, generics, arrays, pointers,
                                   modifiers, function pointers, nested types
    Core/NestedTypesTests.cs       NestedTypes and "Outer/Nested" names
    Core/MethodTests.cs            Attributes, Overrides, PInvokeInfo, generic methods,
                                   MethodReturnType
    Core/FieldTests.cs             Constants, InitialValue/RVA, layout, MarshalInfo
    Core/PropertyTests.cs, Core/EventTests.cs, Core/ParameterTests.cs,
    Core/VariableTests.cs          the remaining member kinds
    Core/SymbolTests.cs            Portable/native/mdb providers, SymbolsNotMatching-
                                   Exception, ThrowIfSymbolsAreNotMatching
    Core/PortablePdbTests.cs       Sequence points, scopes, variable names, embedded
                                   PDB, deterministic MVID, source link, embedded source
    Core/SecurityDeclarationTests.cs  SecurityDeclaration / SecurityAttribute
    Core/TypeParserTests.cs        GetType(fullName, runtimeName: true) name parsing
    Core/TypeReferenceComparisonTests.cs, Core/MethodReferenceComparerTests.cs
    Core/ImageReadTests.cs         PE header facts: Kind, Architecture, Runtime,
                                   Characteristics, debug header
    Pdb/PdbTests.cs                Native PDB read/write round trips (Windows-only
                                   for the write half)
    Mdb/MdbTests.cs                Mono MDB read/write round trips
    Rocks/ModuleDefinitionRocksTests.cs, Rocks/TypeDefinitionRocksTests.cs,
    Rocks/MethodDefinitionRocksTests.cs, Rocks/TypeReferenceRocksTests.cs,
    Rocks/DocCommentIdTests.cs     every extension method in ROCKS


QUICK REFERENCE CARD
====================

    // open
    using var asm = AssemblyDefinition.ReadAssembly (path);                    // deferred
    using var asm = AssemblyDefinition.ReadAssembly (path, new ReaderParameters {
        ReadingMode = ReadingMode.Immediate, InMemory = true, ReadWrite = false,
        ReadSymbols = true, ThrowIfSymbolsAreNotMatching = false,
        AssemblyResolver = resolver });
    using var mod = ModuleDefinition.ReadModule (path);                        // netmodules too
    var mod = asm.MainModule;

    // resolver
    var resolver = new DefaultAssemblyResolver ();
    resolver.AddSearchDirectory (dir);          resolver.ResolveFailure += (s, n) => null;

    // find
    TypeDefinition   t = mod.GetType ("Ns.Outer/Nested");   // null if absent
    IEnumerable<TypeDefinition> all = mod.GetTypes ();       // incl. nested
    MethodDefinition m = t.Methods.First (x => x.Name == "Foo");
    FieldDefinition  f = t.Fields.First (x => x.Name == "_bar");
    PropertyDefinition p = t.Properties.First (x => x.Name == "Baz");   // p.GetMethod / p.SetMethod
    CustomAttribute  a = t.CustomAttributes.First (x => x.AttributeType.FullName == "...");

    // reference -> definition
    TypeDefinition   td = typeRef.Resolve ();     // null if missing; throws
    MethodDefinition md = methodRef.Resolve ();   //   AssemblyResolutionException
                                                  //   if the assembly is not found

    // bring foreign things into this module
    TypeReference   tr = mod.ImportReference (typeof (Console));
    MethodReference mr = mod.ImportReference (typeof (Console).GetMethod ("WriteLine", new [] { typeof (string) }));
    TypeReference   tr = mod.ImportReference (otherModuleTypeDef);
    TypeReference   gi = mod.ImportReference (openType).MakeGenericInstanceType (mod.TypeSystem.Int32);  // Rocks

    // primitives of THIS module
    mod.TypeSystem.Void / Object / String / Int32 / Boolean / ... / CoreLibrary

    // IL
    MethodBody body = m.Body;                    // null when !m.HasBody
    body.SimplifyMacros ();                      // Rocks
    ILProcessor il = body.GetILProcessor ();
    Instruction i = il.Create (OpCodes.Call, mr);
    il.Emit (OpCodes.Ldstr, "x");   il.Append (i);   il.InsertBefore (target, i);
    il.InsertAfter (target, i);     il.Replace (old, i);   il.Remove (i);   il.Clear ();
    body.Variables.Add (new VariableDefinition (mod.TypeSystem.Int32));   body.InitLocals = true;
    body.ExceptionHandlers.Add (new ExceptionHandler (ExceptionHandlerType.Catch) {
        TryStart = ..., TryEnd = ..., HandlerStart = ..., HandlerEnd = ..., CatchType = tr });
    body.OptimizeMacros ();                      // Rocks
    instr.OpCode.Code == Code.Call;  instr.Operand as MethodReference;  instr.Next / .Previous

    // symbols
    m.DebugInformation.SequencePoints            // SequencePoint: StartLine, Document.Url
    m.DebugInformation.Scope.Variables           // VariableDebugInformation names
    new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () }
    // Cil: DefaultSymbolReaderProvider, PortablePdbReaderProvider, EmbeddedPortablePdbReaderProvider
    // Pdb: PdbReaderProvider, NativePdbReaderProvider   Mdb: MdbReaderProvider

    // build new
    var asm  = AssemblyDefinition.CreateAssembly (new AssemblyNameDefinition ("N", new Version (1,0,0,0)), "N.dll", ModuleKind.Dll);
    var type = new TypeDefinition ("Ns", "T", TypeAttributes.Public | TypeAttributes.Class, mod.TypeSystem.Object);
    var meth = new MethodDefinition ("M", MethodAttributes.Public | MethodAttributes.Static, mod.TypeSystem.Void);
    var fld  = new FieldDefinition ("_f", FieldAttributes.Private, mod.TypeSystem.Int32);
    var prop = new PropertyDefinition ("P", PropertyAttributes.None, mod.TypeSystem.Int32) { GetMethod = getter };
    var evt  = new EventDefinition ("E", EventAttributes.None, handlerType) { AddMethod = add, RemoveMethod = remove };
    var attr = new CustomAttribute (mod.ImportReference (ctorInfo));
    attr.ConstructorArguments.Add (new CustomAttributeArgument (mod.TypeSystem.String, "v"));
    mod.Types.Add (type);  type.Methods.Add (meth);  type.Fields.Add (fld);  asm.EntryPoint = meth;

    // write
    asm.Write (outPath);
    asm.Write (outPath, new WriterParameters { WriteSymbols = true });                     // mirror what was read
    asm.Write (outPath, new WriterParameters { SymbolWriterProvider = new PortablePdbWriterProvider () });
    asm.Write (outPath, new WriterParameters { StrongNameKeyBlob = File.ReadAllBytes ("k.snk"), DeterministicMvid = true });
    mod.Write ();                                // in place; needs ReadWrite = true at read time

    // exceptions to expect
    AssemblyResolutionException (FileNotFoundException)   ResolutionException
    SymbolsNotFoundException (FileNotFoundException)      SymbolsNotMatchingException (InvalidOperationException)
    ArgumentException "Member '...' is declared in another module and needs to be imported"
    NotSupportedException "Writing mixed-mode assemblies is not supported"
