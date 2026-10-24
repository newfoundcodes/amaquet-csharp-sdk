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

/// <summary>Workflow 11: workflow_11_pubsub. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow11Pubsub
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var key = "example:workflow:pubsub";
        await client.Typed.Pubsub(key).CreateAsync();

        await using (var sub = await client.SubscribeAsync(key, "events"))
        {
            await client
                .Typed.Pubsub(key)
                .PublishAsync(
                    JsonWire.Map(
                        "channel",
                        "events",
                        "value",
                        JsonWire.Map("type", "utf8_string", "value", "hello")
                    )
                );

            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(5)
            );

            await foreach (var evt in sub.ReadAllAsync(timeout.Token))
            {
                Console.WriteLine(evt.GetRawText());
                break;
            }
        }
    }
}
