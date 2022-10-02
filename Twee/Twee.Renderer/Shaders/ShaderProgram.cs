namespace Twee.Renderer.Shaders;

public class ShaderProgram
{
    (string path, ShaderHandle handle)[]? shaderHandles;
    readonly ProgramHandle programHandle;
    readonly Dictionary<string, int> attributeLocations = new();

    public bool Loaded => shaderHandles is null;

    public ShaderProgram(ShaderPrograms shaderPrograms, VFSReader vfs, string vsPath, string fsPath)
    {
        ShaderHandle CompileShader(ShaderType type, string path)
        {
            var handle = GL.CreateShader(type);
            GL.ShaderSource(handle, vfs.ReadAllText(path));
            GL.CompileShader(handle);

            var status = 0;
            GL.GetShaderi(handle, ShaderParameterName.CompileStatus, ref status);
            if (status == 0)
            {
                GL.GetShaderInfoLog(handle, out var statusInfoLog);
                throw new InvalidOperationException($"""
                    Compilation errors for '{path}':

                    {statusInfoLog}
                    """);
            }

            return handle;
        }

        shaderHandles = new[]
        {
            (vsPath, CompileShader(ShaderType.VertexShader, Path.Combine("Data", "Shaders", vsPath))),
            (fsPath, CompileShader(ShaderType.FragmentShader, Path.Combine("Data", "Shaders", fsPath)))
        };

        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, shaderHandles[0].handle);
        GL.AttachShader(programHandle, shaderHandles[1].handle);
        GL.LinkProgram(programHandle);

        shaderPrograms.Add(this);
    }

    public ShaderProgram(ShaderPrograms shaderPrograms, VFSReader vfs, string path)
        : this(shaderPrograms, vfs, path + ".vert", path + ".frag")
    {
    }

    void EnsureIsLoaded()
    {
        if (Loaded) return;

        var status = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.LinkStatus, ref status);
        if (status == 0)
        {
            GL.GetProgramInfoLog(programHandle, out var programInfoLog);
            throw new InvalidOperationException($"""
                Linking errors for shader program with sources {string.Join(", ", shaderHandles!.Select(w => $"'{w.path}'"))}:

                {programInfoLog}
                """);
        }

        foreach (var (_, handle) in shaderHandles!)
            GL.DeleteShader(handle);
        shaderHandles = null;

        int activeUniformMaxLength = 0, uniformCount = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformMaxLength, ref activeUniformMaxLength);
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniforms, ref uniformCount);
        for (uint i = 0; i < uniformCount; ++i)
        {
            var length = 0;
            var name = GL.GetActiveUniformName(programHandle, i, Math.Max(1, activeUniformMaxLength), ref length);
            var location = GL.GetUniformLocation(programHandle, name);

            if (location >= 0)
                attributeLocations[name] = location;
        }

        int activeUniformBlockMaxNameLength = 0, uniformBlockCount = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformBlockMaxNameLength, ref activeUniformBlockMaxNameLength);
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformBlocks, ref uniformBlockCount);
        for (uint i = 0; i < uniformBlockCount; ++i)
        {
            var length = 0;
            var name = GL.GetActiveUniformBlockName(programHandle, i, Math.Max(1, activeUniformBlockMaxNameLength), ref length);
            var location = (int)GL.GetUniformBlockIndex(programHandle, name);

            if (location >= 0)
                attributeLocations[name] = location;
        }
    }

    static ShaderProgram? lastUsedProgram;
    public void Use()
    {
        EnsureIsLoaded();
        if (lastUsedProgram != this)
        {
            GL.UseProgram(programHandle);
            lastUsedProgram = this;
        }
    }

    public void UniformBlockBind(string uniformVariableName, uint bindingPoint)
    {
        EnsureIsLoaded();
        GL.UniformBlockBinding(programHandle, (uint)attributeLocations[uniformVariableName], bindingPoint);
    }

    public void Uniform(string uniformVariableName, int value)
    {
        EnsureIsLoaded();
        GL.ProgramUniform1i(programHandle, attributeLocations[uniformVariableName], value);
    }

    public void Uniform(string uniformVariableName, Vector4 value)
    {
        EnsureIsLoaded();
        GL.ProgramUniform4f(programHandle, attributeLocations[uniformVariableName], value.X, value.Y, value.Z, value.W);
    }
}
