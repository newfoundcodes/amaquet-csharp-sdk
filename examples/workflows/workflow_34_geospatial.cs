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

/// <summary>Workflow 34: workflow_34_geospatial. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow34Geospatial
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.GeoSpatialIndex(
            "example:workflow:geo_spatial_index"
        );
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"id\":\"manila\",\"point\":{\"Lat\":14.5995,\"Lon\":120.9842},\"metadata\":{\"country\":\"PH\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
