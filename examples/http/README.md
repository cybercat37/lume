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
Dynamic segment values are available in handlers via `route_param("name")`,
`route_param_int("name")`, and `route_param_float("name")`.
Handlers can also set explicit status/body via `respond(status, body)`.
Request metadata is available via `request_method()` and `request_path()`.
Query values are available via `query_param("name")`, `query_param_int("name")`, and
`query_param_float("name")`.

Try:

```bash
curl -i http://127.0.0.1:8080/users/42
curl -i http://127.0.0.1:8080/missing
curl -i http://127.0.0.1:8080/request/info
curl -i "http://127.0.0.1:8080/search?q=axom&page=2"
```

Expected body:

```
42
user by id route
```

Expected `missing` response:

```text
HTTP/1.1 404 Not Found
...

missing route example
```

Expected `request/info` body:

```text
GET
/request/info
```

Expected `search` body:

```text
axom
2
```
