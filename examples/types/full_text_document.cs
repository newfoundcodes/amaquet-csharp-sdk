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

/// <summary>Source-backed full_text_document example. OP calls require --operations.</summary>
public static class TypeFullTextDocument
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.FullTextDocument(
            "example:full_text_document"
        );
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"id\":\"doc-1\",\"metadata\":{\"source\":\"import\"}}"
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
                await handle.AddTextAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"name\":\"body\",\"value\":\"hello amaquet world\",\"weight\":2}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AddTagAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"name\":\"tags\",\"values\":[\"go\",\"memory\"]}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AddNumericAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"name\":\"score\",\"value\":9.5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SnapshotAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
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
