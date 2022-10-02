using System.Diagnostics;
using Twee.Core;

var sw = Stopwatch.StartNew();
VFSWriter.Write(args[0], args.Skip(1), out var ratio);
Console.WriteLine($"Built assets bundle {args[0]} in {sw.Elapsed.TotalSeconds:0.00}sec with a ratio of {ratio * 100:0.000}%");