namespace Twee.Renderer.Shaders;

public class ShaderPrograms : IEnumerable<ShaderProgram>
{
    readonly List<ShaderProgram> programs = new();

    public ShaderPrograms()
    {
        // enable parallel shader compilation, if available
        GraphicsEngine.MaxShaderCompilerThreads(uint.MaxValue);
    }

    internal void Add(ShaderProgram shaderProgram) =>
        programs.Add(shaderProgram);

    public IEnumerator<ShaderProgram> GetEnumerator() => programs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
