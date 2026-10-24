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

/// <summary>Source-backed numeric_field example. OP calls require --operations.</summary>
public static class TypeNumericField
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.NumericField("example:numeric_field");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"name\":\"score\",\"value\":42.5}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"name\":\"score\",\"value\":99.5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
