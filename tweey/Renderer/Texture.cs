namespace Tweey.Renderer
{
    interface ITexture
    {

    }

    class Texture : ITexture
    {
        public static ITexture? LastBoundTexture { get; set; }
    }
}
