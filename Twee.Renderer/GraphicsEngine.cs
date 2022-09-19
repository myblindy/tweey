namespace Twee.Renderer;

public static class GraphicsEngine
{
    public static int MaxTextureSize { get; }

    static GraphicsEngine()
    {
        int maxTextureSize = 0;
        GL.GetInteger(GetPName.MaxTextureSize, ref maxTextureSize);
        MaxTextureSize = maxTextureSize;
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
    public static void BlendNormalAlpha()=>
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    public static void UnbindFrameBuffer()=>
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);

    public static void Viewport(int x, int y, int width, int height) =>
        GL.Viewport(x, y, width, height);
}
