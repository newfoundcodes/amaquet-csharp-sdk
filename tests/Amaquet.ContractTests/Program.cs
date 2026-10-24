using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Amaquet;
using Amaquet.ContractTests;

int passed = 0;
void Check(bool success, string message)
{
    if (!success) {
        throw new Exception("FAIL " + message);
    }

    passed++;
}

Console.WriteLine("Amaquet C# contract tests");
var random = new Random(22062026);

for (int i = 0; i < 2000; i++)
{
    int n = random.Next(0, 4000);
    var payload = new byte[n];
    random.NextBytes(payload);

    var id = (ulong)random.NextInt64(1, long.MaxValue);
    var encoded = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, id, payload)
    );

    using var input = new MemoryStream(encoded);
    var decoded = await WireProtocol.ReadAsync(input);

    Check(
        decoded.RequestId == id && decoded.Payload.SequenceEqual(payload),
        "frame roundtrip"
    );
    encoded[4] = 2;

    try
    {
        WireProtocol.DecodeHeader(encoded.AsSpan(0, 24));
        throw new Exception("bad header accepted");
    }
    catch (AmaquetProtocolException)
    {
        passed++;
    }
}

var big = BigInteger.Parse(
    "12345678901234567890123456789012345678901234567890123"
);

var bigintJson = Encoding.UTF8.GetString(
    JsonWire.Encode(JsonWire.Map("amount", big))
);

Check(
    bigintJson.Contains(big.ToString()) && !bigintJson.Contains('e'),
    "exact BigInteger JSON number"
);

Check(
    JsonWire
        .Decode(Encoding.UTF8.GetBytes(bigintJson))
        .GetProperty("amount")
        .GetRawText() == big.ToString(),
    "exact number decode"
);

foreach (
    var bad in new[]
    {
        "http://localhost:13378",
        "amaquet://user:pass@localhost",
        "amaquet://host/path",
        "amaquet://host/?api_key=secret",
        "amaquet://host/#x",
    }
)
{
    try
    {
        AmaquetEndpoint.Parse(bad);
        throw new Exception("accepted unsafe endpoint: " + bad);
    }
    catch (ArgumentException)
    {
        passed++;
    }
}

await using (var server = new MockServer())
{
    server.Start();

    await using var client = await AmaquetClient.ConnectAsync(
        $"amaquet://127.0.0.1:{server.Port}",
        new ClientOptions
        {
            ApiKey = "test",
            RequireSignedIdentity = true,
            ExpectedIdentityFingerprint = server.Fingerprint,
        }
    );

    Check(
        client.ServerInfo.GetProperty("protocol").GetInt32() == 1,
        "HELLO handshake with OpCommand response"
    );

    Check(
        (await client.CancelRequestAsync(ulong.MaxValue))
            .GetProperty("cancelled")
            .GetBoolean(),
        "CANCEL reply uses OpCommand response"
    );

    var pings = Enumerable
        .Range(0, 60)
        .Select(i =>
            client.CommandAsync(
                "PING",
                JsonWire.Map("delay_ms", i % 2 == 0 ? 30 : 0, "token", i)
            )
        )
        .ToArray();
    var answers = await Task.WhenAll(pings);

    for (int i = 0; i < answers.Length; i++) {
        Check(
            answers[i].GetProperty("args").GetProperty("token").GetInt32() == i,
            "multiplexed response matching"
        );
    }

    var subscription = await client.SubscribeAsync("pubsub", "example", 8, 1);
    await foreach (
        var evt in subscription
            .ReadAllAsync()
            .WithCancellation(
                new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token
            )
    )
    {
        Check(
            evt.GetProperty("data").GetString() == "early-event",
            "early subscription event"
        );
        break;
    }
    await subscription.DisposeAsync();

    byte[] blob = new byte[65537];
    random.NextBytes(blob);
    await client.UploadBlobAsync("blob_test", blob);

    var downloaded = await client.DownloadBlobBytesAsync("blob_test");
    Check(downloaded.SequenceEqual(blob), "binary upload/download exactness");

    var catalogue = JsonSerializer.Deserialize<
        Dictionary<string, List<string>>
    >(await File.ReadAllTextAsync("tools/catalog.source.json"))!;
    foreach (var (type, ops) in catalogue)
    {
        var name = string.Concat(
            type.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p[1..])
        );
        var factory = typeof(Types).GetMethod(name)!;
        var handle = (TypeHandle)
            factory.Invoke(client.Typed, new object[] { "test:" + type })!;
        var scenario = JsonWire.Decode(
            await File.ReadAllBytesAsync("examples/scenarios/" + type + ".json")
        );

        var construction = scenario
            .GetProperty("construction")
            .GetProperty("args");
        var constructionKind = scenario
            .GetProperty("construction")
            .GetProperty("command")
            .GetString()!;

        var created =
            constructionKind == "CREATE"
                ? await handle.CreateAsync(construction.GetProperty("options"))
                : await client.SetAsync(
                    handle.Key,
                    handle.WireType,
                    construction.GetProperty("value")
                );
        Check(
            created.GetProperty("command").GetString() == constructionKind,
            "type construction " + type
        );

        var documented = scenario
            .GetProperty("operations")
            .EnumerateArray()
            .ToList();
        Check(documented.Count == ops.Count, "type operation count " + type);

        foreach (var op in ops)
        {
            var methodName =
                string.Concat(
                    op.ToLowerInvariant()
                        .Split('_')
                        .Select(p => char.ToUpperInvariant(p[0]) + p[1..])
                ) + "Async";
            var method = handle.GetType().GetMethod(methodName)!;
            var documentedOperation = documented.Single(p =>
                p.GetProperty("args").GetProperty("operation").GetString() == op
            );
            var arguments = JsonSerializer.Deserialize<
                Dictionary<string, object?>
            >(
                documentedOperation
                    .GetProperty("args")
                    .GetProperty("args")
                    .GetRawText()
            )!;
            var invoke =
                (Task<JsonElement>)
                    method.Invoke(
                        handle,
                        new object?[] { arguments, CancellationToken.None }
                    )!;
            var result = await invoke;

            Check(
                result.GetProperty("command").GetString() == "OP"
                    && result
                        .GetProperty("args")
                        .GetProperty("operation")
                        .GetString() == op
                    && result.GetProperty("args").GetProperty("key").GetString()
                        == "test:" + type,
                "typed operation dispatch " + type + "." + op
            );
        }
    }

    Console.WriteLine(
        "Native contract: "
            + catalogue.Count
            + " types and "
            + catalogue.Sum(x => x.Value.Count)
            + " operations"
    );
}

var handler = new FakeAdminHandler();
using (
    var admin = new AdminClient(
        "http://localhost:16789",
        new AdminOptions { BearerToken = "a-secret" },
        handler
    )
)
{
    // Test every supported OpenAPI method/path pair without an actual HTTP listener.
    var calls = new (string Method, string Path, Func<Task> Run)[]
    {
        (
            "GET",
            "/openapi.json",
            async () =>
            {
                await admin.OpenApiAsync();
            }
        ),
        (
            "GET",
            "/api/health",
            async () =>
            {
                await admin.HealthAsync();
            }
        ),
        (
            "GET",
            "/api/ready",
            async () =>
            {
                await admin.ReadyAsync();
            }
        ),
        (
            "GET",
            "/metrics",
            async () =>
            {
                await admin.MetricsAsync();
            }
        ),
        (
            "GET",
            "/api/auth/check",
            async () =>
            {
                await admin.CheckAuthAsync();
            }
        ),
        (
            "POST",
            "/api/session",
            async () =>
            {
                await admin.CreateSessionAsync("token");
            }
        ),
        (
            "DELETE",
            "/api/session",
            async () =>
            {
                await admin.DeleteSessionAsync();
            }
        ),
        (
            "POST",
            "/api/identity/accept-invite",
            async () =>
            {
                await admin.AcceptInviteAsync("invite");
            }
        ),
        (
            "POST",
            "/api/identity/mfa",
            async () =>
            {
                await admin.BeginMfaAsync();
            }
        ),
        (
            "PUT",
            "/api/identity/mfa",
            async () =>
            {
                await admin.ConfirmMfaAsync("012345");
            }
        ),
        (
            "DELETE",
            "/api/identity/mfa",
            async () =>
            {
                await admin.DisableMfaAsync("012345");
            }
        ),
        (
            "GET",
            "/api/overview",
            async () =>
            {
                await admin.OverviewAsync();
            }
        ),
        (
            "GET",
            "/api/organization",
            async () =>
            {
                await admin.OrganizationAsync();
            }
        ),
        (
            "PUT",
            "/api/organization",
            async () =>
            {
                await admin.UpdateOrganizationAsync("test", "test");
            }
        ),
        (
            "GET",
            "/api/members",
            async () =>
            {
                await admin.MembersAsync();
            }
        ),
        (
            "POST",
            "/api/members",
            async () =>
            {
                await admin.AddMemberAsync("n", "e", "viewer");
            }
        ),
        (
            "PATCH",
            "/api/members/a%20b",
            async () =>
            {
                await admin.PatchMemberAsync("a b", "viewer");
            }
        ),
        (
            "DELETE",
            "/api/members/a%20b",
            async () =>
            {
                await admin.DeleteMemberAsync("a b");
            }
        ),
        (
            "POST",
            "/api/members/a%20b/invite",
            async () =>
            {
                await admin.InviteMemberAsync("a b", 2);
            }
        ),
        (
            "GET",
            "/api/rbac",
            async () =>
            {
                await admin.RbacAsync();
            }
        ),
        (
            "PUT",
            "/api/rbac",
            async () =>
            {
                await admin.UpdateRbacAsync(new { });
            }
        ),
        (
            "GET",
            "/api/api-keys",
            async () =>
            {
                await admin.ApiKeysAsync();
            }
        ),
        (
            "POST",
            "/api/api-keys",
            async () =>
            {
                await admin.CreateApiKeyAsync("n", "viewer");
            }
        ),
        (
            "DELETE",
            "/api/api-keys/a%20b",
            async () =>
            {
                await admin.RevokeApiKeyAsync("a b");
            }
        ),
        (
            "GET",
            "/api/system",
            async () =>
            {
                await admin.SystemAsync();
            }
        ),
        (
            "PUT",
            "/api/system",
            async () =>
            {
                await admin.UpdateSystemAsync(new { });
            }
        ),
        (
            "POST",
            "/api/persistence/checkpoint",
            async () =>
            {
                await admin.CheckpointAsync("test");
            }
        ),
        (
            "POST",
            "/api/persistence/compact",
            async () =>
            {
                await admin.CompactAsync();
            }
        ),
        (
            "GET",
            "/api/data/keys?cursor=&prefix=&count=2",
            async () =>
            {
                await admin.DataKeysAsync(count: 2);
            }
        ),
        (
            "GET",
            "/api/data/key?key=a%26b",
            async () =>
            {
                await admin.DataKeyAsync("a&b");
            }
        ),
        (
            "DELETE",
            "/api/data/key?key=a%26b",
            async () =>
            {
                await admin.DeleteDataKeyAsync("a&b");
            }
        ),
        (
            "POST",
            "/api/data/command",
            async () =>
            {
                await admin.DataCommandAsync("PING");
            }
        ),
        (
            "GET",
            "/api/audit?limit=2",
            async () =>
            {
                await admin.AuditAsync(2);
            }
        ),
    };

    foreach (var (verb, path, call) in calls)
    {
        await call();
        Check(
            handler.Requests.Last() == verb + " " + path,
            "admin route " + verb + " " + path
        );
    }

    Check(handler.Requests.Count == 33, "all 33 HTTP routes");
    Check(handler.HasAuthorization, "admin bearer token");

    foreach (
        var invalid in new[]
        {
            "/../secret",
            "//other/",
            "/%2e%2e/other",
            "/a%2fb",
        }
    )
    {
        try
        {
            await admin.RequestAsync(HttpMethod.Get, invalid);
            throw new Exception("unsafe path accepted");
        }
        catch (ArgumentException)
        {
            passed++;
        }
    }
}

Console.WriteLine(
    $"PASS {passed} local assertions (mock server and HTTP handler). Live-server checks are opt-in."
);

internal sealed class FakeAdminHandler : HttpMessageHandler
{
    public List<string> Requests { get; } = new();
    public bool HasAuthorization { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        Requests.Add(
            request.Method.Method + " " + request.RequestUri!.PathAndQuery
        );
        HasAuthorization |=
            request.Headers.Authorization?.ToString() == "Bearer a-secret";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                request.RequestUri.AbsolutePath == "/metrics"
                    ? "metrics 1"
                    : "{}",
                Encoding.UTF8,
                request.RequestUri.AbsolutePath == "/metrics"
                    ? "text/plain"
                    : "application/json"
            ),
        };

        if (request.RequestUri.AbsolutePath == "/api/session") {
            response.Headers.Add(
                "Set-Cookie",
                "amaquet_session=testcookie; HttpOnly; SameSite=Strict"
            );
        }

        return Task.FromResult(response);
    }
}
