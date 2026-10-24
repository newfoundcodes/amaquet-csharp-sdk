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

/// <summary>Source-backed record example. OP calls require --operations.</summary>
public static class TypeRecord
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Record("example:record");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"schema\":[{\"name\":\"name\",\"type\":\"utf8_string\",\"nullable\":false},{\"name\":\"age\",\"type\":\"integer\",\"nullable\":true}]}"
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
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"name\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alice\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"name\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
