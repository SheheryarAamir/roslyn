﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static async Task FindLiteralReferencesAsync(
           object value,
           TypeCode typeCode,
           Solution solution,
           IStreamingFindLiteralReferencesProgress progress,
           CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    // Create a callback that we can pass to the server process to hear about the 
                    // results as it finds them.  When we hear about results we'll forward them to
                    // the 'progress' parameter which will then update the UI.
                    var serverCallback = new FindLiteralsServerCallback(solution, progress, cancellationToken);

                    var success = await client.TryRunRemoteAsync(
                        WellKnownServiceHubServices.CodeAnalysisService,
                        nameof(IRemoteSymbolFinder.FindLiteralReferencesAsync),
                        solution,
                        new object[] { value, typeCode },
                        cancellationToken,
                        serverCallback).ConfigureAwait(false);

                    if (success)
                    {
                        return;
                    }
                }

                await FindLiteralReferencesInCurrentProcessAsync(value, solution, progress, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static Task FindLiteralReferencesInCurrentProcessAsync(
            object value, Solution solution,
            IStreamingFindLiteralReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            var engine = new FindLiteralsSearchEngine(
                solution, progress, value, cancellationToken);
            return engine.FindReferencesAsync();
        }
    }
}
