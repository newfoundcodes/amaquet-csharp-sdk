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

/// <summary>Workflow 35: workflow_35_rate_limit. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow35RateLimit
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.RateLimitBucket(
            "example:workflow:rate_limit_bucket"
        );

        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"capacity\":5,\"refill_per_second\":2}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AllowAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"cost\":1}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
