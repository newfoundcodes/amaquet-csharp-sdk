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
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Amaquet;

/// <summary>Identity: Provides the Identity operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Verifies signed server HELLO identity, public-key pins, or fingerprint pins; it does not replace TLS certificate verification.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public static class Identity
{
    /// <summary>Verify: Verify using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Parameter `options`: Call settings or documented construction flags, as accepted by this method.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static void Verify(
        JsonElement hello,
        string nonce,
        ClientOptions options
    )
    {
        bool required =
            options.RequireSignedIdentity
            || options.ExpectedIdentityPublicKeyBase64 is not null
            || options.ExpectedIdentityFingerprint is not null;
        var hasKey = hello.TryGetProperty(
            "identity_public_key",
            out var keyValue
        );

        var hasSig = hello.TryGetProperty("nonce_signature", out var sigValue);
        if (!hasKey && !hasSig)
        {
            if (required)
            {
                throw new AmaquetProtocolException(
                    "Server identity was required but not provided"
                );
            }

            return;
        }

        if (
            !hasKey
            || !hasSig
            || keyValue.ValueKind != JsonValueKind.String
            || sigValue.ValueKind != JsonValueKind.String
        )
        {
            throw new AmaquetProtocolException(
                "Incomplete signed server identity"
            );
        }

        try
        {
            var pub = DecodeCanonical(keyValue.GetString()!);
            var sig = DecodeCanonical(sigValue.GetString()!);

            if (pub.Length != 32 || sig.Length != 64)
            {
                throw new CryptographicException("Invalid Ed25519 lengths");
            }

            var fingerprint =
                "ed25519:"
                + Convert.ToHexString(SHA256.HashData(pub)).ToLowerInvariant();
            if (
                hello.TryGetProperty("identity_fingerprint", out var fp)
                && (
                    fp.ValueKind != JsonValueKind.String
                    || fp.GetString() != fingerprint
                )
            )
            {
                throw new CryptographicException(
                    "Server advertised an invalid fingerprint"
                );
            }

            if (
                options.ExpectedIdentityFingerprint is not null
                && !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(
                        options.ExpectedIdentityFingerprint
                    ),
                    Encoding.ASCII.GetBytes(fingerprint)
                )
            )
            {
                throw new CryptographicException(
                    "Identity fingerprint pin mismatch"
                );
            }

            if (
                options.ExpectedIdentityPublicKeyBase64 is not null
                && !CryptographicOperations.FixedTimeEquals(
                    DecodeCanonical(options.ExpectedIdentityPublicKeyBase64),
                    pub
                )
            )
            {
                throw new CryptographicException(
                    "Identity public-key pin mismatch"
                );
            }

            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(pub, 0));

            var challenge = Encoding.UTF8.GetBytes(nonce);
            verifier.BlockUpdate(challenge, 0, challenge.Length);

            if (!verifier.VerifySignature(sig))
            {
                throw new CryptographicException("Ed25519 signature mismatch");
            }
        }
        catch (Exception e)
            when (e
                    is CryptographicException
                        or FormatException
                        or ArgumentException
            )
        {
            throw new AmaquetProtocolException(
                "Signed server identity verification failed",
                e
            );
        }
    }

    private static byte[] DecodeCanonical(string text)
    {
        if (text.Length > 1024)
        {
            throw new FormatException("Base64 value too large");
        }

        var bytes = Convert.FromBase64String(text);
        if (
            Convert.ToBase64String(bytes) != text
            && Convert.ToBase64String(bytes).TrimEnd('=') != text
        )
        {
            throw new FormatException("Noncanonical Base64");
        }

        return bytes;
    }
}
