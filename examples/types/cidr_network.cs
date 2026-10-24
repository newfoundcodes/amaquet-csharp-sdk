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

using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed cidr_network example. OP calls require --operations.</summary>
public static class TypeCidrNetwork
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.CidrNetwork("example:cidr_network");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"10.0.0.0/8\"").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.ContainsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"ip\":\"10.1.2.3\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
