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

/// <summary>Source-backed multiset example. OP calls require --operations.</summary>
public static class TypeMultiset
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Multiset("example:multiset");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"},\"count\":3}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.CountAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"},\"count\":1}"
                    )!
                )
            ).GetRawText()
        );
    }
}
