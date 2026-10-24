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

/// <summary>Workflow 3: workflow_03_exact_integers. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow03ExactIntegers
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var number = System.Numerics.BigInteger.Parse(
            "1234567890123456789012345678901234567890"
        );
        await client.SetAsync(
            "example:workflow:bigint",
            "big_integer",
            number.ToString()
        );

        Console.WriteLine(
            (await client.GetAsync("example:workflow:bigint")).GetRawText()
        );
    }
}
