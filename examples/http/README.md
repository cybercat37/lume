# HTTP Serve Example

Run:

```bash
dotnet run --project src/axom -- serve examples/http/serve-minimal.axom --host 127.0.0.1 --port 8080
```

Then verify the built-in health endpoint:

```bash
curl -i http://127.0.0.1:8080/health
```

Expected body:

```
ok
```

Current `serve` behavior includes route discovery + Axom route execution: files under
`examples/http/routes` are discovered and executed when their path matches.
Dynamic segment values are available in handlers via `route_param("name")`.

Try:

```bash
curl -i http://127.0.0.1:8080/users/42
```

Expected body:

```
42
user by id route
```
