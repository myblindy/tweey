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

    const string BuildBuildingFile = @"Data/Sounds/build_building.ogg";
    const string PlacedBuildingFile = @"Data/Sounds/place_building.ogg";
    const string ButtonClickFile = @"Data/Sounds/button_click.ogg";

    readonly Dictionary<string, int> soundBuffers = new();
    readonly Dictionary<Building, int> buildingsBeingBuiltSources = new();
    readonly List<int> nonLoopingSoundSources = new();

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

    int CreateSource(string file, bool looping)
    {
        var sourceHandle = AL.GenSource();
        AL.Source(sourceHandle, ALSourceb.Looping, looping);
        AL.Source(sourceHandle, ALSourcei.Buffer, GetSoundBufferHandle(file));
        AL.SourcePlay(sourceHandle);

        return sourceHandle;
    }

    internal void OnCurrentBuildingTemplateChanged(BuildingTemplate? buildingTemplate) =>
        nonLoopingSoundSources.Add(CreateSource(ButtonClickFile, false));

    public void OnPlacedBuilding(Building building) =>
        nonLoopingSoundSources.Add(CreateSource(PlacedBuildingFile, false));

    public void OnStartedBuildingJob(Building building, Villager villager) => 
        buildingsBeingBuiltSources[building] = CreateSource(BuildBuildingFile, true);

    public void OnEndedBuildingJob(Building building, Villager villager)
    {
        AL.DeleteSource(buildingsBeingBuiltSources[building]);
        buildingsBeingBuiltSources.Remove(building);
    }

    readonly HashSet<int> tempDoneNonLoopingSoundSources = new();
    public void Update(double deltaSec)
    {
        tempDoneNonLoopingSoundSources.Clear();
        tempDoneNonLoopingSoundSources.AddRange(nonLoopingSoundSources.Where(sourceHandle => AL.GetSourceState(sourceHandle) == ALSourceState.Stopped));

        nonLoopingSoundSources.RemoveAll(sourceHandle => tempDoneNonLoopingSoundSources.Contains(sourceHandle));
        foreach (var soundSource in tempDoneNonLoopingSoundSources)
            AL.DeleteSource(soundSource);
    }
}
