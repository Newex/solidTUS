// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using SolidTUS.Benchmarks.FileWriter;

BenchmarkRunner.Run<UploadTest>();