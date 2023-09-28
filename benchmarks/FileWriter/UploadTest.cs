using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

using Microsoft.Extensions.Internal;


using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.Benchmarks.FileWriter;

/*
Result of 10 runs of reading/writing a 1 Gb file.
On a Samsung SSD 960 EVO.
================================================
// * Summary *

BenchmarkDotNet v0.13.8, Ubuntu 23.04 (Lunar Lobster)
AMD Ryzen 7 3700X, 1 CPU, 4 logical and 2 physical cores
.NET SDK 7.0.111
  [Host]     : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2
  Job-GCNPZM : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=10  RunStrategy=Monitoring  
UnrollFactor=1  WarmupCount=0  

| Method            | Mean    | Error   | StdDev   |
|------------------ |--------:|--------:|---------:|
| UploadPartialFile | 4.621 s | 1.156 s | 0.7645 s |

// * Hints *
Outliers
  UploadTest.UploadPartialFile: InvocationCount=1, IterationCount=10, RunStrategy=Monitoring, UnrollFactor=1, WarmupCount=0 -> 1 outlier  was  detected (6.51 s)
================================================

This currently gives ~ 4.621 seconds for 1 Gb.
=~ 1731 Megabit per sec (216.4 Megabyte per second)

[calculator used: https://www.omnicalculator.com/other/download-time]

========================================================
4 RUNS
| Method               | Mean    | Error    | StdDev  |
|--------------------- |--------:|---------:|--------:|
| UploadPartialFileOld | 3.916 s | 10.473 s | 1.621 s |
| UploadPartialFileNew | 4.763 s | 11.048 s | 1.710 s |
========================================================
// * Summary *

BenchmarkDotNet v0.13.8, Ubuntu 23.04 (Lunar Lobster)
AMD Ryzen 7 3700X, 1 CPU, 4 logical and 2 physical cores
.NET SDK 7.0.111
  [Host]     : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2
  Job-ZVFQDE : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=10  RunStrategy=Monitoring  
UnrollFactor=1  WarmupCount=0  
========================================================
10 RUNES

| Method               | Mean    | Error   | StdDev  |
|--------------------- |--------:|--------:|--------:|
| UploadPartialFileOld | 4.084 s | 2.989 s | 1.977 s |
| UploadPartialFileNew | 5.041 s | 2.153 s | 1.424 s |
========================================================
// * Summary *

BenchmarkDotNet v0.13.8, Ubuntu 23.04 (Lunar Lobster)
AMD Ryzen 7 3700X, 1 CPU, 4 logical and 2 physical cores
.NET SDK 7.0.111
  [Host]     : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2
  Job-VRKTYU : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=10  RunStrategy=Monitoring  
UnrollFactor=1  WarmupCount=0  

| Method               | Mean    | Error   | StdDev  |
|--------------------- |--------:|--------:|--------:|
| UploadPartialFileOld | 5.248 s | 2.513 s | 1.663 s |
| UploadPartialFileNew | 6.674 s | 2.641 s | 1.747 s |
========================================================
// * Summary *

BenchmarkDotNet v0.13.8, Ubuntu 23.04 (Lunar Lobster)
AMD Ryzen 7 3700X, 1 CPU, 4 logical and 2 physical cores
.NET SDK 7.0.111
  [Host]     : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2
  Job-CJHKRK : .NET 7.0.11 (7.0.1123.42501), X64 RyuJIT AVX2

InvocationCount=1  IterationCount=10  RunStrategy=Monitoring  
UnrollFactor=1  WarmupCount=0  

| Method               | Mean    | Error   | StdDev   |
|--------------------- |--------:|--------:|---------:|
| UploadPartialFileOld | 5.173 s | 1.915 s | 1.2668 s |
| UploadPartialFileNew | 6.087 s | 1.058 s | 0.6997 s |
========================================================
*/

[SimpleJob(RunStrategy.Monitoring, warmupCount: 0, iterationCount: 10)]
public class UploadTest
{
    private IUploadMetaHandler? uploadMetaHandler;
    private FileUploadStorageHandler? uploadStorageHandler;
    private const string OutputFilename = "output.bin";
    private UploadFileInfo uploadInfo = new();


    [GlobalSetup]
    public void GlobalSetup()
    {
        var options = new FileStorageOptions
        {
            DirectoryPath = "./TestItems",
            MetaDirectoryPath = "./TestItems"
        };
        uploadMetaHandler = new FileUploadMetaHandler(Microsoft.Extensions.Options.Options.Create(options));
        var clock = new SystemClock();
        uploadStorageHandler = new FileUploadStorageHandler(clock, uploadMetaHandler);
        var info = uploadMetaHandler.GetResourceAsync(OutputFilename, CancellationToken.None).Result;
        if (info is not null)
        {
            uploadInfo = info;
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var uploadedFilePath = $"./TestItems/{OutputFilename}.chunk";
        if (File.Exists(uploadedFilePath))
        {
            File.Delete(uploadedFilePath);
        }

        if (uploadMetaHandler is not null)
        {
            // Reset metadata file
            var created = uploadMetaHandler.CreateResourceAsync(uploadInfo, CancellationToken.None).Result;
        }
    }

    [Benchmark]
    public async Task UploadPartialFileOld()
    {
        if (uploadStorageHandler is null)
        {
            return;
        }

        // A random file must exist with the given filename.
        // See readme in the TestItems folder on how to create one.
        var samplePath = "./TestItems/sample.bin";
        if (File.Exists(samplePath))
        {
            var pipe = new Pipe();
            using var fs = File.OpenRead(samplePath);
            var readFile = FillPipeAsync(fs, pipe.Writer);

            var uploadFile = await uploadStorageHandler.OnPartialUploadAsync(pipe.Reader, uploadInfo, null, CancellationToken.None);
        }
    }

    private async Task FillPipeAsync(Stream stream, PipeWriter writer)
    {
        int minBufferSize = 4096;
        while (true)
        {
            Memory<byte> memory = writer.GetMemory(minBufferSize);
            try
            {
                var read = await stream.ReadAsync(memory);
                if (read == 0)
                {
                    break;
                }

                writer.Advance(read);
            }
            catch (Exception)
            {
                break;
            }

            var result = await writer.FlushAsync();
            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync();
    }
}
