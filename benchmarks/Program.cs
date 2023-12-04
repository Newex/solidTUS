// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using SolidTUS.Benchmarks.FileWriter;

// var summary = BenchmarkRunner.Run<MetadataParsing>();
BenchmarkRunner.Run<UploadTest>();