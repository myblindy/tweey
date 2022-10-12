using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using Twee.Core.Support;

namespace Twee.Core;

public class VFSReader : IDisposable
{
    readonly List<VFSEntry> directories = new();
    readonly MemoryMappedFile archiveFile;
    private bool disposedValue;

    public VFSReader(string archivePath)
    {
        archiveFile = MemoryMappedFile.CreateFromFile(archivePath, FileMode.Open);

        // read the indices
        using var reader = new BinaryReader(archiveFile.CreateViewStream());

        // version
        if (reader.ReadByte() is { } version && version != 0)
            throw new IOException($"Invalid VFS version encountered: {version}");

        var footerOffset = reader.ReadUInt64();
        reader.BaseStream.Seek((long)footerOffset, SeekOrigin.Begin);

        while (reader.ReadByte() is { } pathPartCount && pathPartCount > 0)
        {
            // path parts
            VFSEntry? entry = null;
            while (pathPartCount-- > 0)
            {
                var pathPart = reader.ReadString();

                if (entry is null)
                    if (directories.FirstOrDefault(d => d.Name.Equals(pathPart, StringComparison.InvariantCultureIgnoreCase)) is { } nextDirectory)
                        entry = nextDirectory;
                    else
                        directories.Add(entry = new(pathPart, null));
                else if (entry.Entries.FirstOrDefault(d => d.Name.Equals(pathPart, StringComparison.InvariantCultureIgnoreCase)) is { } nextDirectory)
                    entry = nextDirectory;
                else
                    entry.Entries.Add(entry = new(pathPart, entry));
            }

            entry!.FileOffset = reader.ReadUInt64();
            entry!.FileSize = reader.ReadUInt64();
        }
    }

    VFSEntry? GetEntry(string path)
    {
        VFSEntry? entry = null;
        foreach (var pathPart in path.GetDirectoryParts(".").Reverse())
        {
            if (entry is null)
                if (directories.FirstOrDefault(d => d.Name.Equals(pathPart, StringComparison.InvariantCultureIgnoreCase)) is { } nextDirectory)
                    entry = nextDirectory;
                else
                    return null;
            else if (entry.Entries.FirstOrDefault(d => d.Name.Equals(pathPart, StringComparison.InvariantCultureIgnoreCase)) is { } nextDirectory)
                entry = nextDirectory;
            else
                return null;
        }

        return entry;
    }

    public Stream? OpenRead(string path)
    {
        if (GetEntry(path) is not { } entry)
            return null;
        return new GZipStream(archiveFile.CreateViewStream((long)entry.FileOffset, (long)entry.FileSize), CompressionMode.Decompress);
    }

    public IEnumerable<string> EnumerateFiles(string path, SearchOption allDirectories)
    {
        if (GetEntry(path) is not { } entry)
            yield break;

        var tempPartPathList = new List<string>();
        var openSet = new Queue<VFSEntry>();
        openSet.Enqueue(entry);

        do
        {
            var currentEntry = openSet.Dequeue();

            tempPartPathList.Clear();
            void buildTempPartPathList(VFSEntry entry)
            {
                if (entry.Parent is { } parent)
                    buildTempPartPathList(parent);
                tempPartPathList!.Add(entry.Name);
            }
            buildTempPartPathList(currentEntry);
            var tempPartPath = string.Join('/', tempPartPathList);

            foreach (var childEntry in currentEntry.Entries)
                if (childEntry.IsFile)
                    yield return tempPartPath + "/" + childEntry.Name;
                else
                    openSet.Enqueue(childEntry);
        } while (allDirectories is SearchOption.TopDirectoryOnly || openSet.Count > 0);
    }

    public string? ReadAllText(string path)
    {
        if (OpenRead(path) is not { } stream)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public string WriteToTemporaryFile(string path)
    {
        if (OpenRead(path) is not { } inputStream)
            throw new FileNotFoundException("VFS file not found.", path);

        while (true)
        {
            var tempFileName = Path.GetRandomFileName();
            tempFileName = Path.ChangeExtension(tempFileName, Path.GetExtension(path));
            tempFileName = Path.Combine(Path.GetTempPath(), tempFileName);

            if (!File.Exists(tempFileName))
            {
                using var outputStream = File.Create(tempFileName);
                inputStream.CopyTo(outputStream);
                return tempFileName;
            }
        }
    }

    record VFSEntry(string Name, VFSEntry? Parent)
    {
        public List<VFSEntry> Entries { get; } = new();
        public ulong FileOffset { get; set; }
        public ulong FileSize { get; set; }

        public bool IsFile => FileOffset > 0;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed
            }

            // unmanaged
            archiveFile.Dispose();

            disposedValue = true;
        }
    }

    ~VFSReader()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
