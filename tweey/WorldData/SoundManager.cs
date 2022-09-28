using System.Globalization;
using System.Text.RegularExpressions;
using Twee.Core;

namespace Tweey.WorldData;

partial class SoundManager
{
    private readonly World world;

    public SoundManager(World world)
    {
        this.world = world;

        // read the production line sounds
        foreach (var productionLineSoundPath in Directory.EnumerateFiles("Data/Buildings"))
            if (ProductionLineSoundPathRegex().Match(Path.GetFileName(productionLineSoundPath)) is { Success: true } match)
                productionLineSounds.Add(
                    (match.Groups[1].Value, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) - 1),
                    productionLineSoundPath);

        // open the device
        var audioDevice = ALC.OpenDevice(null);
        if (audioDevice.Handle != IntPtr.Zero)
        {
            var audioContext = ALC.CreateContext(audioDevice, new ALContextAttributes());
            ALC.MakeContextCurrent(audioContext);
        }
    }

    const string BuildBuildingFile = @"Data/Sounds/build_building.ogg";
    const string ChopTreeFile = @"Data/Sounds/chop_tree.ogg";
    const string FallTreeFile = @"Data/Sounds/fall_tree.ogg";
    const string PlacedBuildingFile = @"Data/Sounds/place_building.ogg";
    const string ButtonClickFile = @"Data/Sounds/button_click.ogg";

    readonly static Dictionary<(string buildingName, int productionLineIndex), string> productionLineSounds = new();

    static string? FileNameForEntity(PlaceableEntity entity, Villager villager) => entity switch
    {
        Building { IsBuilt: false } => BuildBuildingFile,
        Building { ProductionLines: { } } building =>
            building.ActiveProductionLines.FindIndex(w => building.GetAssignedWorkerSlot(villager)?.ActiveProductionLine == w) is { } productionLineIndex
                && productionLineIndex >= 0 && productionLineSounds.TryGetValue((building.FileName, productionLineIndex), out var path) ? path : null,
        Tree => ChopTreeFile,
        _ => null
    };

    readonly Dictionary<string, int> soundBuffers = new();
    readonly Dictionary<(PlaceableEntity, Villager), int> loopingSoundSources = new();
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

    public void OnCurrentBuildingTemplateChanged(BuildingTemplate? buildingTemplate) =>
        nonLoopingSoundSources.Add(CreateSource(ButtonClickFile, false));

    public void OnPlacedBuilding(Building building) =>
        nonLoopingSoundSources.Add(CreateSource(PlacedBuildingFile, false));

    public void OnStartedJob(PlaceableEntity entity, Villager villager)
    {
        if (FileNameForEntity(entity, villager) is { } path)
            loopingSoundSources[(entity, villager)] = CreateSource(path, true);
    }

    public void OnEndedJob(PlaceableEntity entity, Villager villager, bool last)
    {
        if (last)
        {
            AL.DeleteSource(loopingSoundSources[(entity, villager)]);
            loopingSoundSources.Remove((entity, villager));

            // if a tree, add the falling sound at the end
            if (entity is Tree)
                nonLoopingSoundSources.Add(CreateSource(FallTreeFile, false));
        }
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

    float volume = 1f;
    public float Volume { get => volume; set => AL.Listener(ALListenerf.Gain, volume = value); }

    [RegexGenerator("^(.*)-production-(\\d+).ogg")]
    private static partial Regex ProductionLineSoundPathRegex();
}
