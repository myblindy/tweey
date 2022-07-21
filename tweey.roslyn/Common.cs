using System.Reflection;

namespace tweey.roslyn;

static class Common
{
    internal static readonly string GeneratedCodeAttributeText =
        $"[System.CodeDom.Compiler.GeneratedCode(\"{Assembly.GetAssembly(typeof(Common)).GetName().Name}\", \"{Assembly.GetAssembly(typeof(Common)).GetName().Version}\")]";
}
