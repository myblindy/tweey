using System.Reflection;

namespace Twee.Roslyn;

static class Common
{
    internal static readonly string GeneratedCodeAttributeText =
        $"[System.CodeDom.Compiler.GeneratedCode(\"{Assembly.GetAssembly(typeof(Common)).GetName().Name}\", \"{Assembly.GetAssembly(typeof(Common)).GetName().Version}\")]";
}
