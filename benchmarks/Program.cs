﻿// See https://aka.ms/new-console-template for more information
using System;
using BenchmarkDotNet.Running;
using SolidTUS.Benchmarks.FileWriter;
using SolidTUS.Benchmarks.Parsing;

// var summary = BenchmarkRunner.Run<MetadataParsing>();
BenchmarkRunner.Run<UploadTest>();