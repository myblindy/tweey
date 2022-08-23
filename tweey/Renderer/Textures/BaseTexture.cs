namespace Tweey.Renderer.Textures;

public abstract class BaseTexture
{
    public abstract void Bind(int unit = 0);

    static readonly List<BaseTexture?> LastBoundTexturesList = new();

    protected sealed class LastBoundTextureAccessor
    {
        public BaseTexture? this[int unit]
        {
            get
            {
                while (LastBoundTexturesList.Count <= unit)
                    LastBoundTexturesList.Add(null);
                return LastBoundTexturesList[unit];
            }
            set
            {
                while (LastBoundTexturesList.Count <= unit)
                    LastBoundTexturesList.Add(null);
                LastBoundTexturesList[unit] = value;
            }
        }
    }

    protected LastBoundTextureAccessor LastBoundTexture { get; } = new();
}
