using OpenTK.Audio.OpenAL;
using Tweey.Actors;

namespace Tweey.Renderer;

class WorldAudio
{
    private readonly World world;

    public WorldAudio(World world)
    {
        this.world = world;
    }

    const string HammeringFile = @"Data/Sounds/hammering.ogg";

    readonly Dictionary<string, int> soundBuffers = new();
    readonly Dictionary<Building, int> buildingsBeingBuiltSources = new();
    readonly HashSet<Building> buildingsBeingBuilt = new();

    int GetSoundBufferHandle(string path) => soundBuffers.TryGetValue(path, out var soundBufferHandle) ? soundBufferHandle : soundBuffers[path] = LoadBufferFromFile(path);

    readonly List<Building> tempBuildings = new();
    public void Process(double deltaSec)
    {
        buildingsBeingBuilt.Clear();
        foreach (var building in world.GetEntities<Building>().Where(b => !b.IsBuilt && b.AssignedWorkersWorking.Cast<bool>().Any(b => b)))
            buildingsBeingBuilt.Add(building);

        // delete sources that belong to finished buildings
        tempBuildings.Clear();
        tempBuildings.AddRange(buildingsBeingBuiltSources.Keys.Except(buildingsBeingBuilt));
        foreach (var buildingToRemove in tempBuildings)
        {
            AL.DeleteSource(buildingsBeingBuiltSources[buildingToRemove]);
            buildingsBeingBuiltSources.Remove(buildingToRemove);
        }

        // create new sources
        tempBuildings.Clear();
        tempBuildings.AddRange(buildingsBeingBuilt.Except(buildingsBeingBuiltSources.Keys));
        foreach (var newBuilding in tempBuildings)
        {
            var sourceHandle = AL.GenSource();
            buildingsBeingBuiltSources[newBuilding] = sourceHandle;
            var soundBufferHandle = GetSoundBufferHandle(HammeringFile);
            AL.Source(sourceHandle, ALSourceb.Looping, true);
            AL.Source(sourceHandle, ALSourcei.Buffer, soundBufferHandle);
            AL.SourcePlay(sourceHandle);
        }
    }

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
}
