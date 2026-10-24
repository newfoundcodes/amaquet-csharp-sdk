using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Amaquet;

/// <summary>AdminOptions: Provides the Admin Options operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Control-plane requests use a separate HTTP(S) base URL and may require administrative authorization.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AdminOptions
{
    public string? BearerToken { get; init; }
    public string? AdminToken { get; init; }

    /// <summary>FromSeconds: Provides the From Seconds operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxResponseBytes { get; init; } = 16 << 20;
    public bool UseSessionCookies { get; init; } = true;
}

/// <summary>HTTP control plane. Uses the administration listener, not the native TCP port.</summary>
/// <summary>AdminClient: Provides the Admin Client operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Control-plane requests use a separate HTTP(S) base URL and may require administrative authorization.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AdminClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri _origin;
    private readonly AdminOptions _options;
    private readonly object _cookieLock = new();
    private string? _cookie;

    /// <summary>AdminClient: Provides the Admin Client operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Control-plane requests use a separate HTTP(S) base URL and may require administrative authorization.</remarks>
    /// <remarks>Parameter `options`: Call settings or documented construction flags, as accepted by this method.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="origin">Administrative HTTP(S) endpoint.</param>
    /// <param name="options">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="handler">Optional HTTP message handler.</param>
    public AdminClient(
        string origin,
        AdminOptions? options = null,
        HttpMessageHandler? handler = null
    )
    {
        _options = options ?? new AdminOptions();
        if (
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (
                uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps
            )
            || string.IsNullOrWhiteSpace(uri.Host)
            || uri.UserInfo.Length != 0
            || uri.Query.Length != 0
            || uri.Fragment.Length != 0
            || uri.AbsolutePath != "/"
        )
        {
            throw new ArgumentException(
                "Admin URL must be a plain HTTP(S) origin without credentials, path, or query"
            );
        }

        if (
            _options.RequestTimeout <= TimeSpan.Zero
            || _options.MaxResponseBytes < 1
            || (_options.BearerToken?.Contains('\n') ?? false)
            || (_options.BearerToken?.Contains('\r') ?? false)
            || (_options.AdminToken?.Contains('\n') ?? false)
            || (_options.AdminToken?.Contains('\r') ?? false)
        )
        {
            throw new ArgumentException("Invalid admin client options");
        }

        _origin = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/");
        _http = new HttpClient(
            handler
                ?? new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false,
                },
            disposeHandler: true
        )
        {
            Timeout = _options.RequestTimeout,
        };
    }

    /// <summary>ClearSession: Provides the Clear Session operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public void ClearSession()
    {
        lock (_cookieLock)
        {
            _cookie = null;
        }
    }

    /// <summary>PathId: Provides the Path Id operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static string PathId(string id)
    {
        if (
            string.IsNullOrEmpty(id)
            || id is "." or ".."
            || id.Contains('/')
            || id.Contains('\\')
            || id.Any(char.IsControl)
        )
        {
            throw new ArgumentException(
                "Invalid resource identifier",
                nameof(id)
            );
        }

        return Uri.EscapeDataString(id);
    }

    private static string Query(string value) => Uri.EscapeDataString(value);

    private static void ValidatePath(string path)
    {
        if (
            string.IsNullOrEmpty(path)
            || !path.StartsWith('/')
            || path.StartsWith("//")
            || path.Contains('\\')
            || path.Contains('#')
            || path.Any(char.IsControl)
        )
        {
            throw new ArgumentException("Unsafe HTTP path");
        }

        var pathname = path.Split('?', 2)[0];
        foreach (var part in pathname.Split('/'))
        {
            string decoded = Uri.UnescapeDataString(part);
            if (
                decoded is "." or ".."
                || decoded.Contains('/')
                || decoded.Contains('\\')
                || decoded.Any(char.IsControl)
            )
            {
                throw new ArgumentException("Path traversal is not allowed");
            }
        }
    }

    private async Task<byte[]> RequestBytesAsync(
        HttpMethod method,
        string path,
        object? body,
        bool hasBody,
        bool text,
        CancellationToken ct
    )
    {
        ValidatePath(path);

        var uri = new Uri(_origin, path);
        if (uri.Scheme != _origin.Scheme || uri.Authority != _origin.Authority)
        {
            throw new ArgumentException("Cross-origin URL not allowed");
        }

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                text ? "text/plain" : "application/json"
            )
        );

        if (_options.BearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.BearerToken
            );
        }

        if (_options.AdminToken is not null)
        {
            request.Headers.Add("X-Amaquet-Admin-Token", _options.AdminToken);
        }

        string? cookie;
        lock (_cookieLock)
        {
            cookie = _cookie;
        }

        if (cookie is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                "amaquet_session=" + cookie
            );
        }

        if (hasBody)
        {
            byte[] json = JsonWire.Encode(body);
            if (json.Length > WireProtocol.MaxPayload)
            {
                throw new ArgumentOutOfRangeException(nameof(body));
            }

            request.Content = new ByteArrayContent(json);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                "application/json"
            );
        }

        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        if (
            response.IsSuccessStatusCode
            && _options.UseSessionCookies
            && method == HttpMethod.Post
            && path == "/api/session"
            && response.Headers.TryGetValues("Set-Cookie", out var setCookies)
        )
        {
            foreach (var header in setCookies)
            {
                var first = header.Split(';', 2)[0];
                if (
                    first.StartsWith(
                        "amaquet_session=",
                        StringComparison.Ordinal
                    )
                    && first.Length <= 4112
                    && !first.Any(char.IsControl)
                )
                {
                    lock (_cookieLock)
                    {
                        _cookie = first.Substring("amaquet_session=".Length);
                    }

                    break;
                }
            }
        }

        if (
            response.IsSuccessStatusCode
            && method == HttpMethod.Delete
            && path == "/api/session"
        )
        {
            ClearSession();
        }

        await using var stream = await response
            .Content.ReadAsStreamAsync(ct)
            .ConfigureAwait(false);
        using var buffer = new MemoryStream();

        byte[] block = new byte[8192];
        while (true)
        {
            int n = await stream
                .ReadAsync(
                    block.AsMemory(
                        0,
                        (int)
                            Math.Min(
                                block.Length,
                                (long)_options.MaxResponseBytes
                                    + 1
                                    - buffer.Length
                            )
                    ),
                    ct
                )
                .ConfigureAwait(false);

            if (n == 0)
            {
                break;
            }

            buffer.Write(block, 0, n);
            if (buffer.Length > _options.MaxResponseBytes)
            {
                throw new AmaquetProtocolException(
                    "Administration response size limit exceeded"
                );
            }
        }

        byte[] bytes = buffer.ToArray();
        if (!response.IsSuccessStatusCode)
        {
            var raw = Encoding.UTF8.GetString(bytes);
            string code = "HTTP_ERROR",
                message = raw;

            try
            {
                var json = JsonWire.Decode(bytes);
                if (json.ValueKind == JsonValueKind.Object)
                {
                    if (
                        json.TryGetProperty("code", out var c)
                        && c.ValueKind == JsonValueKind.String
                    )
                    {
                        code = c.GetString()!;
                    }

                    if (
                        json.TryGetProperty("message", out var m)
                        && m.ValueKind == JsonValueKind.String
                    )
                    {
                        message = m.GetString()!;
                    }

                    if (json.TryGetProperty("error", out var e))
                    {
                        if (e.ValueKind == JsonValueKind.String)
                        {
                            message = e.GetString()!;
                        }
                        else if (e.ValueKind == JsonValueKind.Object)
                        {
                            if (
                                e.TryGetProperty("code", out var ec)
                                && ec.ValueKind == JsonValueKind.String
                            )
                            {
                                code = ec.GetString()!;
                            }

                            if (
                                e.TryGetProperty("message", out var em)
                                && em.ValueKind == JsonValueKind.String
                            )
                            {
                                message = em.GetString()!;
                            }
                        }
                    }
                }
            }
            catch (AmaquetProtocolException) { }

            throw new AmaquetHttpException(
                (int)response.StatusCode,
                code,
                message,
                raw
            );
        }

        return bytes;
    }

    /// <summary>RequestAsync: Provides the Request Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="method">HTTP method used for the request.</param>
    /// <param name="path">Relative request path.</param>
    /// <param name="body">Optional request body.</param>
    /// <param name="hasBody">Whether the request includes a body.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<JsonElement> RequestAsync(
        HttpMethod method,
        string path,
        object? body = null,
        bool hasBody = false,
        CancellationToken ct = default
    )
    {
        var bytes = await RequestBytesAsync(
                method,
                path,
                body,
                hasBody,
                false,
                ct
            )
            .ConfigureAwait(false);
        return bytes.Length == 0
            ? JsonWire.Decode("null"u8)
            : JsonWire.Decode(bytes);
    }

    /// <summary>RequestTextAsync: Provides the Request Text Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="method">HTTP method used for the request.</param>
    /// <param name="path">Relative request path.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<string> RequestTextAsync(
        HttpMethod method,
        string path,
        CancellationToken ct = default
    ) =>
        Encoding.UTF8.GetString(
            await RequestBytesAsync(method, path, null, false, true, ct)
                .ConfigureAwait(false)
        );

    private Task<JsonElement> Get(
        string path,
        CancellationToken ct = default
    ) => RequestAsync(HttpMethod.Get, path, ct: ct);

    private Task<JsonElement> Post(
        string path,
        object? body = null,
        bool hasBody = false,
        CancellationToken ct = default
    ) => RequestAsync(HttpMethod.Post, path, body, hasBody, ct);

    private Task<JsonElement> Put(
        string path,
        object? body,
        CancellationToken ct = default
    ) => RequestAsync(HttpMethod.Put, path, body, true, ct);

    private Task<JsonElement> Patch(
        string path,
        object? body,
        CancellationToken ct = default
    ) => RequestAsync(HttpMethod.Patch, path, body, true, ct);

    private Task<JsonElement> Delete(
        string path,
        object? body = null,
        bool hasBody = false,
        CancellationToken ct = default
    ) => RequestAsync(HttpMethod.Delete, path, body, hasBody, ct);

    /// <summary>OpenApiAsync: Provides the Open Api Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> OpenApiAsync(CancellationToken ct = default) =>
        Get("/openapi.json", ct);

    /// <summary>HealthAsync: Provides the Health Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> HealthAsync(CancellationToken ct = default) =>
        Get("/api/health", ct);

    /// <summary>ReadyAsync: Ready Async using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ReadyAsync(CancellationToken ct = default) =>
        Get("/api/ready", ct);

    /// <summary>MetricsAsync: Provides the Metrics Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<string> MetricsAsync(CancellationToken ct = default) =>
        RequestTextAsync(HttpMethod.Get, "/metrics", ct);

    /// <summary>CheckAuthAsync: Provides the Check Auth Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CheckAuthAsync(CancellationToken ct = default) =>
        Get("/api/auth/check", ct);

    /// <summary>CreateSessionAsync: Provides the Create Session Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `token`: Administrative token; avoid sending it over untrusted HTTP.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="token">Administrative session token.</param>
    /// <param name="mfaCode">Optional multi-factor authentication code.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CreateSessionAsync(
        string token,
        string? mfaCode = null,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/session",
            JsonWire.Map("token", token, "mfa_code", mfaCode),
            true,
            ct
        );

    /// <summary>DeleteSessionAsync: Provides the Delete Session Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DeleteSessionAsync(
        CancellationToken ct = default
    ) => Delete("/api/session", ct: ct);

    /// <summary>AcceptInviteAsync: Provides the Accept Invite Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="invite">Invitation token.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> AcceptInviteAsync(
        string invite,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/identity/accept-invite",
            JsonWire.Map("invite", invite),
            true,
            ct
        );

    /// <summary>BeginMfaAsync: Provides the Begin Mfa Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> BeginMfaAsync(CancellationToken ct = default) =>
        Post("/api/identity/mfa", ct: ct);

    /// <summary>ConfirmMfaAsync: Provides the Confirm Mfa Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="code">Multi-factor authentication code.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ConfirmMfaAsync(
        string code,
        CancellationToken ct = default
    ) => Put("/api/identity/mfa", JsonWire.Map("code", code), ct);

    /// <summary>DisableMfaAsync: Provides the Disable Mfa Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="code">Multi-factor authentication code.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DisableMfaAsync(
        string code,
        CancellationToken ct = default
    ) => Delete("/api/identity/mfa", JsonWire.Map("code", code), true, ct);

    /// <summary>OverviewAsync: Provides the Overview Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> OverviewAsync(CancellationToken ct = default) =>
        Get("/api/overview", ct);

    /// <summary>OrganizationAsync: Provides the Organization Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> OrganizationAsync(
        CancellationToken ct = default
    ) => Get("/api/organization", ct);

    /// <summary>UpdateOrganizationAsync: Provides the Update Organization Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `name`: Native command or type operation name sent to the server.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="name">Organization name.</param>
    /// <param name="slug">Organization slug.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> UpdateOrganizationAsync(
        string name,
        string slug,
        CancellationToken ct = default
    ) => Put("/api/organization", JsonWire.Map("name", name, "slug", slug), ct);

    /// <summary>MembersAsync: Provides the Members Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> MembersAsync(CancellationToken ct = default) =>
        Get("/api/members", ct);

    /// <summary>AddMemberAsync: Provides the Add Member Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `name`: Native command or type operation name sent to the server.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="name">Member name.</param>
    /// <param name="email">Member email address.</param>
    /// <param name="role">Member role.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> AddMemberAsync(
        string name,
        string email,
        string role,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/members",
            JsonWire.Map("name", name, "email", email, "role", role),
            true,
            ct
        );

    /// <summary>PatchMemberAsync: Provides the Patch Member Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="id">Member identifier.</param>
    /// <param name="role">Updated member role.</param>
    /// <param name="status">Optional updated member status.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> PatchMemberAsync(
        string id,
        string role,
        string? status = null,
        CancellationToken ct = default
    ) =>
        Patch(
            "/api/members/" + PathId(id),
            JsonWire.Map("role", role, "status", status),
            ct
        );

    /// <summary>DeleteMemberAsync: Provides the Delete Member Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="id">Member identifier.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DeleteMemberAsync(
        string id,
        CancellationToken ct = default
    ) => Delete("/api/members/" + PathId(id), ct: ct);

    /// <summary>InviteMemberAsync: Provides the Invite Member Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="id">Member identifier.</param>
    /// <param name="hours">Invitation lifetime in hours.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> InviteMemberAsync(
        string id,
        int hours,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/members/" + PathId(id) + "/invite",
            JsonWire.Map("ttl_hours", hours),
            true,
            ct
        );

    /// <summary>RbacAsync: Provides the Rbac Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> RbacAsync(CancellationToken ct = default) =>
        Get("/api/rbac", ct);

    /// <summary>UpdateRbacAsync: Provides the Update Rbac Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="policy">RBAC policy configuration.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> UpdateRbacAsync(
        object policy,
        CancellationToken ct = default
    ) => Put("/api/rbac", policy, ct);

    /// <summary>ApiKeysAsync: Provides the Api Keys Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ApiKeysAsync(CancellationToken ct = default) =>
        Get("/api/api-keys", ct);

    /// <summary>CreateApiKeyAsync: Provides the Create Api Key Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `name`: Native command or type operation name sent to the server.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="name">API key name.</param>
    /// <param name="role">API key role.</param>
    /// <param name="expiresAt">Optional API key expiration timestamp.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CreateApiKeyAsync(
        string name,
        string role,
        string? expiresAt = null,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/api-keys",
            JsonWire.Map("name", name, "role", role, "expires_at", expiresAt),
            true,
            ct
        );

    /// <summary>RevokeApiKeyAsync: Provides the Revoke Api Key Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="id">API key identifier.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> RevokeApiKeyAsync(
        string id,
        CancellationToken ct = default
    ) => Delete("/api/api-keys/" + PathId(id), ct: ct);

    /// <summary>SystemAsync: Provides the System Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> SystemAsync(CancellationToken ct = default) =>
        Get("/api/system", ct);

    /// <summary>UpdateSystemAsync: Provides the Update System Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="config">System configuration.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> UpdateSystemAsync(
        object config,
        CancellationToken ct = default
    ) => Put("/api/system", config, ct);

    /// <summary>CheckpointAsync: Provides the Checkpoint Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `name`: Native command or type operation name sent to the server.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="name">Checkpoint name.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CheckpointAsync(
        string name,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/persistence/checkpoint",
            JsonWire.Map("name", name),
            true,
            ct
        );

    /// <summary>CompactAsync: Provides the Compact Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CompactAsync(CancellationToken ct = default) =>
        Post("/api/persistence/compact", ct: ct);

    /// <summary>DataKeysAsync: Provides the Data Keys Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="cursor">Cursor returned by a previous scan.</param>
    /// <param name="prefix">Optional key prefix filter.</param>
    /// <param name="count">Maximum number of keys to return.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DataKeysAsync(
        string cursor = "",
        string prefix = "",
        int count = 100,
        CancellationToken ct = default
    ) =>
        Get(
            $"/api/data/keys?cursor={Query(cursor)}&prefix={Query(prefix)}&count={count}",
            ct
        );

    /// <summary>DataKeyAsync: Provides the Data Key Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DataKeyAsync(
        string key,
        CancellationToken ct = default
    ) => Get("/api/data/key?key=" + Query(key), ct);

    /// <summary>DeleteDataKeyAsync: Provides the Delete Data Key Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DeleteDataKeyAsync(
        string key,
        CancellationToken ct = default
    ) => Delete("/api/data/key?key=" + Query(key), ct: ct);

    /// <summary>DataCommandAsync: Provides the Data Command Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="args">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="command">Administrative command to execute.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DataCommandAsync(
        string command,
        object? args = null,
        CancellationToken ct = default
    ) =>
        Post(
            "/api/data/command",
            JsonWire.Map("command", command, "args", args ?? new { }),
            true,
            ct
        );

    /// <summary>AuditAsync: Provides the Audit Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="limit">Maximum number of audit entries to return.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> AuditAsync(
        int limit = 100,
        CancellationToken ct = default
    ) => Get("/api/audit?limit=" + limit, ct);

    /// <summary>Dispose: Provides the Dispose operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public void Dispose() => _http.Dispose();
}
