using System.IO.Compression;
using Twee.Core.Support;

namespace Twee.Core;

public static class VFSWriter
{
    public static void Write(string archivePath, IEnumerable<string> inputs, out double ratio)
    {
        ulong totalSize = 0, totalCompressedSize = 0;

        var footerActions = new List<Action>();

        using var outputStream = File.Create(archivePath);
        using var outputWriter = new BinaryWriter(outputStream);

        // step 1, write the compressed file data
        outputWriter.Write((byte)0);    // version
        outputWriter.Write(0UL);        // header offset, to be filled at the end
        outputWriter.Flush();

        var lastPosition = outputStream.Position;
        foreach (var input in inputs)
            foreach (var inputFile in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
            {
                using (var compressedStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
                using (var inputFileStream = File.OpenRead(inputFile))
                {
                    totalSize += (ulong)inputFileStream.Length;
                    inputFileStream.CopyTo(compressedStream);
                }

                var compressedSize = (ulong)(outputStream.Position - lastPosition);
                var savedLastPosition = lastPosition;

                footerActions.Add(() =>
                {
                    var directoryPaths = inputFile.GetDirectoryParts(".").Reverse().ToList();

                    outputWriter.Write((byte)directoryPaths.Count);
                    foreach (var part in directoryPaths)
                        outputWriter.Write(part);
                    outputWriter.Write(savedLastPosition);
                    outputWriter.Write(compressedSize);
                });

                totalCompressedSize += compressedSize;
                lastPosition = outputStream.Position;
            }

        // step 2, write the compressed file names
        outputWriter.Flush();
        outputStream.Flush();
        var offset = outputWriter.BaseStream.Position;

        foreach (var action in footerActions)
            action();

        // step 3, update the header offset at the beginning of the output file
        outputWriter.Seek(sizeof(byte), SeekOrigin.Begin);
        outputWriter.Write(offset);

        ratio = (double)totalCompressedSize / totalSize;
    }
}
