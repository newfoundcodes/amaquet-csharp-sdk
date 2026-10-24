namespace Amaquet;

/// <summary>AmaquetException: Represents an Amaquet amaquet exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
/// <remarks>Defines typed errors for protocol, authorization, HTTP, timeouts, and closed connections.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public class AmaquetException : Exception
{
    /// <summary>AmaquetException: Represents an Amaquet amaquet exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>AmaquetProtocolException: Represents an Amaquet amaquet protocol exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
/// <remarks>Defines typed errors for protocol, authorization, HTTP, timeouts, and closed connections.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AmaquetProtocolException : AmaquetException
{
    /// <summary>AmaquetProtocolException: Represents an Amaquet amaquet protocol exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetProtocolException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>AmaquetTransportException: Represents an Amaquet amaquet transport exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
/// <remarks>Defines typed errors for protocol, authorization, HTTP, timeouts, and closed connections.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AmaquetTransportException : AmaquetException
{
    /// <summary>AmaquetTransportException: Represents an Amaquet amaquet transport exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetTransportException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>AmaquetRequestException: Represents an Amaquet amaquet request exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
/// <remarks>Defines typed errors for protocol, authorization, HTTP, timeouts, and closed connections.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AmaquetRequestException : AmaquetException
{
    /// <summary>Server-provided request error code.</summary>
    public string Code { get; }

    /// <summary>AmaquetRequestException: Represents an Amaquet amaquet request exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetRequestException(string code, string message)
        : base(message) => Code = code;
}

/// <summary>AmaquetHttpException: Represents an Amaquet amaquet http exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
/// <remarks>Defines typed errors for protocol, authorization, HTTP, timeouts, and closed connections.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AmaquetHttpException : AmaquetException
{
    /// <summary>HTTP status code returned by the administration endpoint.</summary>
    public int StatusCode { get; }

    /// <summary>Server-provided HTTP error code.</summary>
    public string Code { get; }

    /// <summary>Raw response body returned by the administration endpoint.</summary>
    public string ResponseBody { get; }

    /// <summary>AmaquetHttpException: Represents an Amaquet amaquet http exception with diagnostic information for callers; avoid logging credentials or private payloads.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetHttpException(
        int status,
        string code,
        string message,
        string response
    )
        : base(message)
    {
        StatusCode = status;
        Code = code;
        ResponseBody = response;
    }
}
