using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using SolidTUS.Parsers;

namespace SolidTUS.Benchmarks.Parsing;

[MemoryDiagnoser]
public class MetadataParsing
{
    private readonly string metadata;

    public MetadataParsing()
    {
        static string Encode(string value) => Base64Converters.Encode(value);
        // Construct metadata
        var sb = new StringBuilder();
        sb.AppendFormat("{0} {1},", "key1", Encode("value1"))
            .AppendFormat("{0} {1},", "key2", Encode("value2"))
            .AppendFormat("{0} {1},", "key3", Encode("value3"))
            .AppendFormat("{0} {1},", "key4", Encode("value4"))
            .AppendFormat("{0} {1},", "key5", Encode("value5"));
        sb.Append(Encode("only_key1,"))
            .Append(Encode("only_key2,"))
            .Append(Encode("only_key3,"))
            .Append(Encode("only_key4,"))
            .Append(Encode("only_key5,"))
            .Append(Encode("only_key6"));
        
        metadata = sb.ToString();
    }

    [Benchmark]
    public Dictionary<string, string> OldParse() => MetadataParser.Parse(metadata);

    [Benchmark]
    public Dictionary<string, string> NewParse() => MetadataParser.Parse(metadata);



}
