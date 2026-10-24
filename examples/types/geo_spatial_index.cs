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

/// <summary>Source-backed geo_spatial_index example. OP calls require --operations.</summary>
public static class TypeGeoSpatialIndex
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.GeoSpatialIndex("example:geo_spatial_index");
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
                        "{\"id\":\"manila\",\"point\":{\"Lat\":14.5995,\"Lon\":120.9842},\"metadata\":{\"country\":\"PH\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RadiusAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"center\":{\"Lat\":14.5995,\"Lon\":120.9842},\"radius_meters\":1000,\"limit\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"manila\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.BoxAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"box\":{\"MinLat\":14.0,\"MinLon\":120.0,\"MaxLat\":15.0,\"MaxLon\":122.0}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PolygonAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"points\":[{\"Lat\":14.5,\"Lon\":120.9},{\"Lat\":14.7,\"Lon\":120.9},{\"Lat\":14.6,\"Lon\":121.1}]}"
                    )!
                )
            ).GetRawText()
        );
    }
}
