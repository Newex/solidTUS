Create random file with 1 GB size.

```console
$ openssl rand -out sample.bin -base64 $(( 2**30 * 3/4 ))
```

source: https://superuser.com/a/470957