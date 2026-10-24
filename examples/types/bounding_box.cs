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

/// <summary>Source-backed bounding_box example. OP calls require --operations.</summary>
public static class TypeBoundingBox
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.BoundingBox("example:bounding_box");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse(
                    "{\"MinLat\":14.0,\"MinLon\":120.0,\"MaxLat\":15.0,\"MaxLon\":122.0}"
                )
                .RootElement
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
                        "{\"point\":{\"Lat\":14.5,\"Lon\":121.0}}"
                    )!
                )
            ).GetRawText()
        );
    }
}
