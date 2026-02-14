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

Current `serve` behavior is a bootstrap slice (M13): the HTTP endpoint is runtime-provided (`/health`) and is not yet derived from Axom source files.

Route discovery bootstrap (M14 slice): files under `examples/http/routes` are discovered and registered as HTTP stubs.

Try:

```bash
curl -i http://127.0.0.1:8080/users/42
```

Expected body contains:

```
stub route matched: GET /users/:id<int>
```
