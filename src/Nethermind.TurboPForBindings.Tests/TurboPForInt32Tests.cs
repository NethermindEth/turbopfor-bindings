// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.TurboPForBindings.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixtureSource(nameof(Algorithms))]
public class TurboPForInt32Tests(TurboPForTestsBase<int>.Algorithm algorithm) : TurboPForTestsBase<int>(algorithm)
{
    private static Algorithm[] Algorithms() =>
    [
        new("p4nd1*128v32", TurboPFor.p4nd1enc128v32, TurboPFor.p4nd1dec128v32, BlockSize: 128),
        new("p4nd1*256v32", TurboPFor.p4nd1enc256v32, TurboPFor.p4nd1dec256v32, BlockSize: 256),

        // Mixed version - don't work
        //new("p4nd1enc256v32 / p4nd1dec128v32", TurboPFor.p4nd1enc256v32, TurboPFor.p4nd1dec128v32),
        //new("p4nd1enc128v32 / p4nd1dec256v32", TurboPFor.p4nd1enc128v32, TurboPFor.p4nd1dec256v32)
    ];
}
