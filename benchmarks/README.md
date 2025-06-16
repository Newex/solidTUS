# Prerequisites
Create random file with 1 GB size.

```console
$ openssl rand -out sample.bin -base64 $(( 2**30 * 3/4 ))
```

source: https://superuser.com/a/470957

# Run
To run benchmarks, from the benchmark folder type:

```console
$ dotnet run --project benchmarks.csproj -c Release
```

Into the terminal.