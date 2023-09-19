# How to run this?

To compile the client frontend, go to: svelte directory.

Downloads all dependencies.
```console
$ npm install
```

Then compile svelte:
```console
$ npm run build
```

Copy the build output `build` in svelte to `wwwroot` and start the dotnet app.

```
$ dotnet run --project sveltedotnet.csproj
```

Note the url and port number of the dotnet application e.g. `https://localhost:7134` and use that in `./svelte/src/routes/+page.svelte` Tus endpoint.