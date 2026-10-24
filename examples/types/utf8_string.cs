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

/// <summary>Source-backed utf8_string example. OP calls require --operations.</summary>
public static class TypeUtf8String
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Utf8String("example:utf8_string");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"Hello, Amaquet\"").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
