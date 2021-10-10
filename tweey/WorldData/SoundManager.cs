namespace Tweey.Renderer;

class SoundManager
{
    private readonly World world;

    public SoundManager(World world)
    {
        this.world = world;

        var audioDevice = ALC.OpenDevice(null);
        if (audioDevice.Handle != IntPtr.Zero)
        {
            var audioContext = ALC.CreateContext(audioDevice, new ALContextAttributes());
            ALC.MakeContextCurrent(audioContext);
        }
    }

    const string HammeringFile = @"Data/Sounds/hammering.ogg";

    readonly Dictionary<string, int> soundBuffers = new();
    readonly Dictionary<Building, int> buildingsBeingBuiltSources = new();

    int GetSoundBufferHandle(string path) => soundBuffers.TryGetValue(path, out var soundBufferHandle) ? soundBufferHandle : soundBuffers[path] = LoadBufferFromFile(path);
    static int LoadBufferFromFile(string path)
    {
        using var vorbis = new NVorbis.VorbisReader(path);
        var buffer = new float[(int)Math.Floor(vorbis.Channels * vorbis.SampleRate * vorbis.TotalTime.TotalSeconds)];
        var samplesRead = vorbis.ReadSamples(buffer);
        var castBuffer = buffer.Select(f => (short)Math.Clamp((int)(short.MaxValue * f), short.MinValue, short.MaxValue)).ToArray(buffer.Length);

        var bufferHandle = AL.GenBuffer();
        AL.BufferData(bufferHandle, ALFormat.Mono16, castBuffer, vorbis.SampleRate);

        return bufferHandle;
    }

    public void OnStartedBuildingJob(Building building, Villager villager)
    {
        var sourceHandle = AL.GenSource();
        buildingsBeingBuiltSources[building] = sourceHandle;
        var soundBufferHandle = GetSoundBufferHandle(HammeringFile);
        AL.Source(sourceHandle, ALSourceb.Looping, true);
        AL.Source(sourceHandle, ALSourcei.Buffer, soundBufferHandle);
        AL.SourcePlay(sourceHandle);
    }

    public void OnEndedBuildingJob(Building building, Villager villager)
    {
        AL.DeleteSource(buildingsBeingBuiltSources[building]);
        buildingsBeingBuiltSources.Remove(building);
    }

    public void Update(double deltaSec)
    {
    }

}
