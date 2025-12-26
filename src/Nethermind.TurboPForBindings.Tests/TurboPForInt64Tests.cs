// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.TurboPForBindings.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixtureSource(nameof(Algorithms))]
public class TurboPForInt64Tests(TurboPForTestsBase<long>.Algorithm algorithm) : TurboPForTestsBase<long>(algorithm)
{
    private static Algorithm[] Algorithms() =>
    [
        // https://github.com/powturbo/TurboPFor-Integer-Compression/issues/106#issuecomment-1551558511
        new("p4nd1*64", TurboPFor.p4nd1enc64, TurboPFor.p4nd1dec64, BlockSize: 128)
    ];
}
