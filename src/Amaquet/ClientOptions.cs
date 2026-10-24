/*
 * Amaquet C# SDK - A Newfoundcodes project.
 *
 * Copyright (C) 2026 Jonathan Eldy Baldivicio
 *
 * Author: Jonathan Eldy Baldivicio
 * Contact: jonathaneldy.baldivicio@newfoundcodes.com
 *
 * SPDX-License-Identifier: MIT
 */

using System.Security.Cryptography;

namespace Amaquet;

/// <summary>ClientOptions: Provides the Client Options operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Configures client security, request timeouts, payload limits, and authentication.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class ClientOptions
{
    /// <summary>API key used to authenticate the native connection.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Requires the server to provide a verifiable signed identity.</summary>
    public bool RequireSignedIdentity { get; init; }

    /// <summary>Expected server Ed25519 public key, encoded as Base64.</summary>
    public string? ExpectedIdentityPublicKeyBase64 { get; init; }

    /// <summary>Expected server identity fingerprint.</summary>
    public string? ExpectedIdentityFingerprint { get; init; }

    /// <summary>Maximum time allowed to complete a native request.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum time allowed to establish a native connection.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum native protocol payload accepted by the client.</summary>
    public int MaxPayloadBytes { get; init; } = WireProtocol.MaxPayload;

    /// <summary>Maximum number of requests allowed to remain pending.</summary>
    public int MaxPendingRequests { get; init; } = 4096;

    // TLS uses the system trust store and validates the requested hostname.
    /// <summary>Validates timeout, payload, and pending-request limits.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public void Validate()
    {
        if (
            RequestTimeout <= TimeSpan.Zero
            || ConnectTimeout <= TimeSpan.Zero
            || MaxPayloadBytes < 1
            || MaxPayloadBytes > WireProtocol.MaxPayload
            || MaxPendingRequests < 1
            || MaxPendingRequests > 100000
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(ClientOptions),
                "Invalid client resource limits"
            );
        }
    }
}

/// <summary>AmaquetEndpoint: Provides the Amaquet Endpoint operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed record AmaquetEndpoint(string Host, int Port, bool Tls)
{
    /// <summary>Parse: Parse using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static AmaquetEndpoint Parse(string input)
    {
        if (
            !Uri.TryCreate(input, UriKind.Absolute, out var uri)
            || (uri.Scheme != "amaquet" && uri.Scheme != "amaquets")
            || string.IsNullOrWhiteSpace(uri.Host)
            || uri.UserInfo.Length > 0
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0
            || (uri.AbsolutePath != "/" && uri.AbsolutePath.Length > 0)
        )
        {
            throw new ArgumentException(
                "Endpoint must be an amaquet:// or amaquets:// origin without credentials or path"
            );
        }

        int port = uri.IsDefaultPort ? 13378 : uri.Port;
        if (port < 1 || port > 65535)
        {
            throw new ArgumentException("Invalid port");
        }

        return new AmaquetEndpoint(uri.Host, port, uri.Scheme == "amaquets");
    }
}
