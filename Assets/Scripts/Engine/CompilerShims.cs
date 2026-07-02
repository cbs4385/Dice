namespace System.Runtime.CompilerServices
{
    // Unity's reference assemblies predate this marker type, which the C# 9 compiler
    // requires to emit `init` accessors (used by records). The compiler only checks
    // for the type's existence, not its origin, so this local shim is sufficient.
    internal static class IsExternalInit
    {
    }
}
