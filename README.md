<div align="center">
  <img src="https://amaquet.newfoundcodes.com/amaquet.png" width="128" />
  <h1>Amaquet C# SDK</h1>
  <p>
    <a href="https://github.com/newfoundcodes/amaquet-csharp-sdk/actions"><img src="https://github.com/newfoundcodes/amaquet-csharp-sdk/actions/workflows/ci.yml/badge.svg" alt="Build Status"></a>
  </p>
</div>

A .NET 8 library for Amaquet native TCP/TLS data operations and the separate HTTP(S) administration API.

## Requirements

- .NET SDK 8.0 or later and a reachable Amaquet server compatible with SDK version 0.1.1.
- NuGet package `BouncyCastle.Cryptography` 2.4.0 for Ed25519 signed HELLO verification. All other library components use .NET framework APIs.
- TLS with normal certificate chain and hostname validation is recommended for remote production use. Raw `amaquet://` is not encrypted.

## Build and install

```bash
dotnet build src/Amaquet/Amaquet.csproj -c Release
dotnet pack src/Amaquet/Amaquet.csproj -c Release
```

Reference the source project from an application:

```xml
<ItemGroup>
  <ProjectReference Include="../amaquet-csharp-sdk/src/Amaquet/Amaquet.csproj" />
</ItemGroup>
```

After packaging, use the generated `.nupkg` in a local or private NuGet feed. This source distribution does not include a pre-built package.

## Data-plane connection

```csharp
using Amaquet;
using System.Numerics;

var options = new ClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("AMAQUET_API_KEY"),
    // Set this to a fingerprint obtained through a trusted channel:
    ExpectedIdentityFingerprint = Environment.GetEnvironmentVariable("AMAQUET_IDENTITY_FINGERPRINT"),
    RequestTimeout = TimeSpan.FromSeconds(30),
    MaxPendingRequests = 512
};

await using var client = await AmaquetClient.ConnectAsync(
    "amaquets://database.example.com:13378", options);

Console.WriteLine((await client.PingAsync()).GetRawText());
await client.SetAsync(
    "demo:count",
    "big_integer",
    BigInteger.Parse("123456789012345678901234567890").ToString()
);

var saved = await client.GetAsync("demo:count");
Console.WriteLine(saved.GetRawText());

var counter = client.Typed.AtomicCounter("demo:atomic");
await counter.CreateAsync(new { initial = 10 });

Console.WriteLine((await counter.AddAsync(JsonWire.Map("delta", 5))).GetRawText());
await counter.DeleteKeyAsync();
```

For a server without TLS, use `amaquet://127.0.0.1:13378` only on a trusted local network. Credentials embedded in the URI (userinfo or query) are rejected. Pass the API key in `ClientOptions`.

## Native TCP protocol

The protocol v1 frame uses a fixed **24-byte big-endian header**: magic `AMQT` (4), version (1), kind (1), flags (2), opcode (2), reserved (2), request ID (8), and payload size (4). Protocol opcodes: COMMAND=1, HELLO=2, AUTH=3, SUBSCRIBE=4, UNSUBSCRIBE=5, CANCEL=6. Payloads for these operations are JSON. The maximum frame payload is **64 MiB**, including limits checked before allocating. The native connection is separate from HTTP admin.

The client starts a dedicated async reader, associates responses with 64-bit request IDs, limits in-flight calls, serializes writes, and allows concurrent callers. `BeginCommandAsync` returns a `RequestTicket` so an application can hold the request ID and await `Response`. `CancelRequestAsync` sends the protocol CANCEL opcode. Cancellation/timeout **cannot undo a mutation that the server has already committed**. The client does not replay writes automatically.

```csharp
var pending = await client.BeginCommandAsync("INFO");

Console.WriteLine($"ID: {pending.Id}");
Console.WriteLine((await pending.Response).GetRawText());
```

`client.CommandAsync` applies `RequestTimeout`, and cancellation tokens are supported. `RequestTicket.Response` is the raw task and can be combined with `WaitAsync` for per-call timeouts.

## TLS and signed identity

The client checks TLS certificates and hostnames through `SslStream`. When the server advertises Ed25519 identity fields, the SDK verifies the signed HELLO challenge (`nonce`), validates the advertised fingerprint and checks optional public-key/fingerprint pins. `RequireSignedIdentity=true` rejects HELLO responses that omit identity fields. TLS transport verification is distinct from protocol identity verification. Do not trust an unverified fingerprint acquired from the same insecure connection.

## Exact numbers and typed results

All responses return owned `JsonElement` values, which preserve the original JSON numeric literal. Access large integral fields using `GetRawText` and parse into `BigInteger` rather than converting to `double`. Input `BigInteger` values are serialized as JSON integer tokens by `BigIntegerJsonConverter`. Big decimal wire types may require decimal strings; follow type documentation. No binary token amounts are automatically rounded to IEEE-754 floating point.

## Key commands

Top-level command wrappers include `PingAsync`, `SetAsync`, `CreateAsync`, `CasAsync`, `BatchAsync`, `GetAsync`, `DeleteAsync`, `ExistsAsync`, `TypeAsync`, `TtlAsync`, `ExpireAsync`, `ExpireAtAsync`, `PersistAsync`, `KeysAsync`, `ScanAsync`, `TypesAsync`, `InfoAsync`, `MemoryAsync` and `OperationAsync`. Binary commands have dedicated helpers. The generic command method can send all 23 documented commands. `BATCH` is sequential, **not** a rollback transaction, and `SCAN` is cursor based, **not** a snapshot.

## Large binary values

```csharp
byte[] content = File.ReadAllBytes("payload.bin");
await client.UploadBlobAsync("file:1", content);

byte[] restored = await client.DownloadBlobBytesAsync("file:1", maximum: 64 << 20);
```

For large files, use the `Stream` overloads `UploadBlobAsync(key, input, length, chunkSize, ...)` and `DownloadBlobAsync(key, output, chunkSize, maxBytes, ...)`. These avoid materializing the complete object in memory. Individual chunks default to 1 MiB and are constrained to 32 MiB. A staged binary object is limited to 1 GiB and 65,536 spans. A failed upload attempts `BLOB_ABORT`. Downloads validate range key, offset, size, version, canonical base64 and total length. They fail on inconsistent versions rather than silently combining different revisions.

## Pub/Sub and bounded buffering

```csharp
var pubsub = client.Typed.Pubsub("channel:bus");
await pubsub.CreateAsync();

await using var sub = await client.SubscribeAsync("channel:bus", "changes", localCapacity: 128);
await pubsub.PublishAsync(JsonWire.Map("channel", "changes", "message", "hello"));

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await foreach (var evt in sub.ReadAllAsync(timeout.Token))
{
    Console.WriteLine(evt.GetRawText());
    break;
}

Console.WriteLine($"Dropped events: {sub.DroppedEvents}");
```

Subscriptions are per-connection and event queues are bounded. On overflow, newly arriving events are dropped and the counter increments. Pub/Sub is not a durable replay mechanism. A subscription is registered before writing the SUBSCRIBE request so early events are preserved. Close subscriptions to release server resources.

## Connection pooling

```csharp
await using var pool = await ConnectionPool.ConnectAsync(
    "amaquet://127.0.0.1:13378", size: 4, options: new ClientOptions { ApiKey = "test-key" });
Console.WriteLine((await pool.CommandAsync("PING")).GetRawText());
```

Each pool member has its own server connection and request ID space. Operations are distributed round-robin; pooling does not add transactions or automatic retries.

## HTTP administration

Use the admin listener (typically separate from port 13378):

```csharp
using var admin = new AdminClient("https://admin.example.com:13379",
    new AdminOptions { BearerToken = Environment.GetEnvironmentVariable("AMAQUET_ADMIN_TOKEN") });

Console.WriteLine((await admin.HealthAsync()).GetRawText());
Console.WriteLine((await admin.OverviewAsync()).GetRawText());
```

`AdminOptions.AdminToken` sends `X-Amaquet-Admin-Token`, while `BearerToken` sends `Authorization: Bearer ...`. Login sessions can retain the `amaquet_session` cookie. HTTP redirects are not followed; request paths are validated for traversal and response bytes are bounded before parsing. Error responses throw `AmaquetHttpException` with HTTP status, code and response body.

### All 33 documented method/path combinations

| HTTP method | Path |
|---|---|
| `DELETE` | `/api/api-keys/{id}` |
| `DELETE` | `/api/data/key` |
| `DELETE` | `/api/identity/mfa` |
| `DELETE` | `/api/members/{id}` |
| `DELETE` | `/api/session` |
| `GET` | `/api/api-keys` |
| `GET` | `/api/audit` |
| `GET` | `/api/auth/check` |
| `GET` | `/api/data/key` |
| `GET` | `/api/data/keys` |
| `GET` | `/api/health` |
| `GET` | `/api/members` |
| `GET` | `/api/organization` |
| `GET` | `/api/overview` |
| `GET` | `/api/rbac` |
| `GET` | `/api/ready` |
| `GET` | `/api/system` |
| `GET` | `/metrics` |
| `GET` | `/openapi.json` |
| `PATCH` | `/api/members/{id}` |
| `POST` | `/api/api-keys` |
| `POST` | `/api/data/command` |
| `POST` | `/api/identity/accept-invite` |
| `POST` | `/api/identity/mfa` |
| `POST` | `/api/members` |
| `POST` | `/api/members/{id}/invite` |
| `POST` | `/api/persistence/checkpoint` |
| `POST` | `/api/persistence/compact` |
| `POST` | `/api/session` |
| `PUT` | `/api/identity/mfa` |
| `PUT` | `/api/organization` |
| `PUT` | `/api/rbac` |
| `PUT` | `/api/system` |

## Errors and security

- `AmaquetProtocolException`: malformed frames, protocol mismatch, invalid ranges, or failed signed identity.
- `AmaquetTransportException`: socket failures, failed writes and connection shutdown.
- `AmaquetRequestException`: server-reported `code` and `message`.
- `AmaquetHttpException`: HTTP status, server error code and response body.

Timeout/cancellation is best effort. Authentication tokens must be stored outside source control. Do not use plaintext TCP/HTTP for remote production credentials. Do not share a single subscription key/channel twice on one client connection. Never assume all type-specific operations are side-effect free; several require setup or may block.

## Running the examples

The `examples/` project contains 91 type examples based on the source documentation and 42 standalone workflow samples. By default type examples create a disposable example key and call top-level GET. `--operations` additionally runs the documented OP cases and **may block or mutate state**. Use a dedicated test server for that option.

```bash
export AMAQUET_ENDPOINT=amaquet://127.0.0.1:13378
export AMAQUET_API_KEY=<your-api-key>

dotnet run --project examples/Amaquet.Examples.csproj -- list
dotnet run --project examples/Amaquet.Examples.csproj -- type:atomic_counter
dotnet run --project examples/Amaquet.Examples.csproj -- type:atomic_counter --operations
dotnet run --project examples/Amaquet.Examples.csproj -- workflow:01
```

The C# examples use `AMAQUET_ENDPOINT` for the native URI. The equivalent Go and Java examples use `AMAQUET_URI` and `AMAQUET_URL` respectively.

## Tests and fuzzing

```bash
dotnet build src/Amaquet/Amaquet.csproj -c Release
dotnet build examples/Amaquet.Examples.csproj -c Release

dotnet run --project tests/Amaquet.ContractTests/Amaquet.ContractTests.csproj -c Release
dotnet run --project fuzz/Amaquet.Fuzz/Amaquet.Fuzz.csproj -c Release -- 2000
```

The contract runner checks the wire codec, strict headers, exact integer JSON, URI validation, mock signed-identity HELLO fields/signatures, concurrent out-of-order replies, early subscription events, 91 construction scenarios, all 242 OP methods, binary integrity, and 33 admin routes. It runs independently of a real Amaquet deployment, using controlled servers. The fuzz runner supplies 13 deterministic targets with 2,000 mutations each by default (26,000 inputs total). Run high-volume campaigns with CI or your fuzzing environment after initial compilation succeeds.

### Live-server integration

A disposable instance is required. This test creates and removes keys and exercises the real binary API:

```bash
export AMAQUET_TEST_ENDPOINT=amaquet://127.0.0.1:13378
export AMAQUET_TEST_API_KEY=your-test-key

dotnet run --project tests/Amaquet.LiveTests/Amaquet.LiveTests.csproj -c Release
```

The optional `AMAQUET_TEST_RUN_OPERATIONS=1` also attempts each documented OP request. It can block, change state, require prerequisites, or fail by design; do not enable it on production. `AMAQUET_TEST_IDENTITY_FINGERPRINT` pins the HELLO public key. Running local mock tests is **not a substitute** for testing against the actual server.

## Type API catalog (from reference documentation)

| Wire type | Named type-specific operations |
|---|---|
| `adjacency_set` | `ADD`, `NEIGHBORS`, `REMOVE` |
| `aggregated_series` | `AGGREGATE`, `RANGE` |
| `array` | `APPEND`, `GET`, `LEN`, `SET`, `VALUES` |
| `atomic_counter` | `ADD`, `CAS`, `LOAD` |
| `barrier` | `WAIT` |
| `big_decimal` | `ADD`, `CMP`, `SUB` |
| `big_integer` | `ADD` |
| `binary_string` | None; use top-level key operations |
| `bit_field` | `SET`, `GET` |
| `bit_set` | `SET`, `GETBIT`, `COUNT` |
| `bitmap` | `SET`, `GETBIT`, `COUNT` |
| `blocking_queue` | `ENQUEUE`, `DEQUEUE`, `CLOSE`, `LEN` |
| `bloom_filter` | `ADD`, `CONTAINS` |
| `boolean` | `TOGGLE` |
| `bounding_box` | `CONTAINS` |
| `btree_index` | `INSERT`, `SEARCH`, `DELETE`, `RANGE` |
| `cbor` | `GET` |
| `cidr_network` | `CONTAINS` |
| `consumer_group` | `SNAPSHOT` |
| `count_min_sketch` | `ADD`, `ESTIMATE` |
| `counter_series` | `ADD`, `LAST`, `AGGREGATE`, `LEN`, `RANGE` |
| `counting_bloom_filter` | `ADD`, `CONTAINS`, `REMOVE` |
| `cuckoo_filter` | `ADD`, `CONTAINS`, `REMOVE` |
| `decimal` | `ADD`, `CMP`, `SUB` |
| `delayed_queue` | `ENQUEUE`, `POP_READY`, `LEN` |
| `dense_vector` | `DISTANCE` |
| `deque` | `PUSH_BACK`, `PUSH_FRONT`, `LEN`, `POP_FRONT`, `POP_BACK`, `VALUES` |
| `duration` | `ADD` |
| `event_log` | `APPEND`, `LEN`, `READ` |
| `fifo_queue` | `ENQUEUE`, `PEEK`, `DEQUEUE`, `LEN` |
| `flat_vector_index` | `ADD`, `SEARCH`, `REMOVE`, `LEN` |
| `float` | `ADD` |
| `full_text_document` | `ADD_TEXT`, `ADD_TAG`, `ADD_NUMERIC`, `SNAPSHOT`, `GET` |
| `gauge_series` | `SET`, `LAST`, `RANGE`, `AGGREGATE`, `LEN` |
| `geo_point` | `DISTANCE` |
| `geo_spatial_index` | `ADD`, `RADIUS`, `REMOVE`, `BOX`, `POLYGON` |
| `geospatial_secondary_index` | `ADD`, `RADIUS`, `REMOVE`, `BOX`, `POLYGON` |
| `hash_index` | `ADD`, `LOOKUP`, `REMOVE` |
| `hashmap` | `SET`, `GET`, `DELETE`, `KEYS` |
| `histogram` | `OBSERVE`, `SNAPSHOT` |
| `hnsw_index` | `ADD`, `SEARCH`, `REMOVE` |
| `hyperloglog` | `ADD`, `COUNT` |
| `integer` | `ADD` |
| `inverted_index` | `INDEX`, `SEARCH`, `REMOVE` |
| `json` | `SET`, `GET` |
| `lease` | `ACQUIRE`, `HELD`, `RELEASE`, `RENEW` |
| `lifo_stack` | `PUSH`, `PEEK`, `POP`, `LEN` |
| `list` | `PUSH_BACK`, `PUSH_FRONT`, `LEN`, `RANGE`, `POP_FRONT`, `POP_BACK` |
| `lock` | `TRY_LOCK`, `UNLOCK`, `RENEW` |
| `matrix` | `GET`, `SET`, `LEN` |
| `messagepack` | `GET` |
| `multimap` | `ADD`, `GET`, `REMOVE` |
| `multiset` | `ADD`, `COUNT`, `REMOVE` |
| `null` | None; use top-level key operations |
| `numeric_field` | `SET`, `GET` |
| `ordered_map` | `SET`, `GET`, `DELETE`, `ENTRIES` |
| `ordered_set` | `ADD`, `VALUES`, `REMOVE` |
| `persistent_topic` | `PUBLISH`, `READ` |
| `polygon` | `CONTAINS` |
| `priority_queue` | `ENQUEUE`, `LEN`, `DEQUEUE` |
| `pubsub` | `PUBLISH`, `STATS` |
| `quantized_vector` | `DEQUANTIZE` |
| `radix_tree` | `INSERT`, `PREFIX`, `EXACT` |
| `rate_limit_bucket` | `ALLOW`, `REMAINING` |
| `record` | `SET`, `GET` |
| `reliable_queue` | `ENQUEUE`, `CLAIM`, `ACK`, `NACK`, `STATS` |
| `ring_buffer` | `PUSH`, `LEN`, `VALUES` |
| `roaring_bitmap` | `ADD`, `CONTAINS`, `CARDINALITY`, `REMOVE`, `VALUES` |
| `secondary_index` | `ADD`, `LOOKUP` |
| `semaphore` | `TRY_ACQUIRE`, `STATS`, `RELEASE`, `ACQUIRE` |
| `set` | `ADD`, `CONTAINS`, `REMOVE`, `MEMBERS`, `LEN` |
| `sorted_set` | `ADD`, `SCORE`, `REMOVE`, `RANGE` |
| `sparse_vector` | None; use top-level key operations |
| `stream` | `ADD`, `RANGE`, `GROUP_CREATE`, `GROUP_READ`, `ACK`, `PENDING`, `LEN` |
| `symbol` | None; use top-level key operations |
| `t_digest` | `ADD`, `QUANTILE` |
| `tag_field` | `SET`, `GET` |
| `tensor` | `GET`, `SET`, `LEN` |
| `text_field` | `SET`, `GET` |
| `timeseries` | `ADD`, `LEN`, `RANGE`, `LAST`, `AGGREGATE` |
| `timeseries_labels` | `SET`, `GET` |
| `timestamp` | None; use top-level key operations |
| `top_k` | `ADD`, `ITEMS` |
| `trie` | `INSERT`, `PREFIX`, `EXACT` |
| `tuple` | `LEN`, `GET`, `VALUES` |
| `unsigned_integer` | `ADD` |
| `url` | `HOST`, `SCHEME` |
| `utf8_string` | None; use top-level key operations |
| `uuid` | None; use top-level key operations |
| `vector_metadata` | `SET`, `GET`, `DELETE` |
| `vector_set` | `ADD`, `SEARCH`, `REMOVE`, `LEN` |

## License

Amaquet C# SDK is licensed under the MIT License. See [LICENSE](LICENSE) for the complete license text.
