namespace Twee.Renderer;

public static class GraphicsEngine
{
    public static int MaxTextureSize { get; }

    public static NativeParallelShaderCompilationType NativeParallelShaderCompilation { get; }

    static GraphicsEngine()
    {
        int maxTextureSize = 0;
        GL.GetInteger(GetPName.MaxTextureSize, ref maxTextureSize);
        MaxTextureSize = maxTextureSize;

        int extensionsCount = 0;
        GL.GetInteger(GetPName.NumExtensions, ref extensionsCount);
        while (extensionsCount-- > 0)
            switch (GL.GetStringi(StringName.Extensions, (uint)extensionsCount))
            {
                case "GL_ARB_parallel_shader_compile" when NativeParallelShaderCompilation is NativeParallelShaderCompilationType.None or NativeParallelShaderCompilationType.Khr:
                    NativeParallelShaderCompilation = NativeParallelShaderCompilationType.Arb;
                    break;
                case "GL_KHR_parallel_shader_compile" when NativeParallelShaderCompilation is NativeParallelShaderCompilationType.None:
                    NativeParallelShaderCompilation = NativeParallelShaderCompilationType.Khr;
                    break;
            }
    }

    public static bool MaxShaderCompilerThreads(uint count)
    {
        if (NativeParallelShaderCompilation is NativeParallelShaderCompilationType.Arb)
            GL.ARB.MaxShaderCompilerThreadsARB(count);
        else if (NativeParallelShaderCompilation is NativeParallelShaderCompilationType.Khr)
            GL.KHR.MaxShaderCompilerThreadsKHR(count);
        else
            return false;
        return true;
    }

    public static void Clear(bool alsoClearDepth = false) =>
        GL.Clear(ClearBufferMask.ColorBufferBit | (alsoClearDepth ? ClearBufferMask.DepthBufferBit : 0));

    /// <summary>
    /// Additive blending, source <c>ONE</c> and destination <c>ONE</c>.
    /// </summary>
    public static void BlendAdditive() =>
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

    /// <summary>
    /// Normal blending, source <c>SRC_ALPHA</c> and destination <c>ONE_MINUS_SRC_ALPHA</c>.
    /// </summary>
    public static void BlendNormalAlpha() =>
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    public static void UnbindFrameBuffer() =>
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);

    public static void Viewport(int x, int y, int width, int height) =>
        GL.Viewport(x, y, width, height);
}

public enum NativeParallelShaderCompilationType { None, Arb, Khr }