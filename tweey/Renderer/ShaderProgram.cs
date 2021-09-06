namespace Tweey.Renderer;

class ShaderProgram
{
    readonly ProgramHandle programHandle;
    readonly Dictionary<string, int> attributeLocations = new();

    public ShaderProgram(string vsPath, string fsPath)
    {
        static ShaderHandle CompileShader(ShaderType type, string path)
        {
            var handle = GL.CreateShader(type);
            GL.ShaderSource(handle, File.ReadAllText(path));
            GL.CompileShader(handle);

            int status = 0;
            GL.GetShaderi(handle, ShaderParameterName.CompileStatus, ref status);
            if (status == 0)
            {
                GL.GetShaderInfoLog(handle, out var statusInfoLog);
                throw new InvalidOperationException($"Compilation errors for '{path}':\n\n{statusInfoLog}");
            }

            return handle;
        }

        var vsName = CompileShader(ShaderType.VertexShader, Path.Combine("Data", "Shaders", vsPath));
        var fsName = CompileShader(ShaderType.FragmentShader, Path.Combine("Data", "Shaders", fsPath));

        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vsName);
        GL.AttachShader(programHandle, fsName);
        GL.LinkProgram(programHandle);

        int status = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.LinkStatus, ref status);
        if (status == 0)
        {
            GL.GetProgramInfoLog(programHandle, out var programInfoLog);
            throw new InvalidOperationException($"Linking errors for '{vsPath}' and '{fsPath}':\n\n{programInfoLog}");
        }

        GL.DeleteShader(vsName);
        GL.DeleteShader(fsName);

        int activeUniformMaxLength = 0, uniformCount = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformMaxLength, ref activeUniformMaxLength);
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniforms, ref uniformCount);
        for (uint i = 0; i < uniformCount; ++i)
        {
            int length = 0;
            var name = GL.GetActiveUniformName(programHandle, i, Math.Max(1, activeUniformMaxLength), ref length);
            int location = GL.GetUniformLocation(programHandle, name);

            if (location >= 0)
                attributeLocations[name] = location;
        }

        int activeUniformBlockMaxNameLength = 0, uniformBlockCount = 0;
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformBlockMaxNameLength, ref activeUniformBlockMaxNameLength);
        GL.GetProgrami(programHandle, ProgramPropertyARB.ActiveUniformBlocks, ref uniformBlockCount);
        for (uint i = 0; i < uniformBlockCount; ++i)
        {
            int length = 0;
            var name = GL.GetActiveUniformBlockName(programHandle, i, Math.Max(1, activeUniformBlockMaxNameLength), ref length);
            var location = (int)GL.GetUniformBlockIndex(programHandle, name);

            if (location >= 0)
                attributeLocations[name] = location;
        }
    }

    public ShaderProgram(string path) : this(path + ".vert", path + ".frag") { }

    static ShaderProgram? lastUsedProgram;
    public void Use()
    {
        if (lastUsedProgram != this)
        {
            GL.UseProgram(programHandle);
            lastUsedProgram = this;
        }
    }

    public void UniformBlockBind(string uniformVariableName, uint bindingPoint) =>
        GL.UniformBlockBinding(programHandle, (uint)attributeLocations[uniformVariableName], bindingPoint);

    public void Uniform(string uniformVariableName, int value) =>
        GL.ProgramUniform1i(programHandle, attributeLocations[uniformVariableName], value);
}
