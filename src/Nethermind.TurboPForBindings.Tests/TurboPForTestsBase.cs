// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Nethermind.TurboPForBindings.Tests;

public abstract class TurboPForTestsBase<T>(TurboPForTestsBase<T>.Algorithm algorithm)
    where T : IBinaryInteger<T>, IMinMaxValue<T>
{
    public delegate nuint CompressFunc(ReadOnlySpan<T> @in, nuint n, Span<byte> @out);

    public delegate nuint DecompressFunc(ReadOnlySpan<byte> @in, nuint n, Span<T> @out);

    private static readonly int TBits = int.CreateChecked(T.PopCount(T.AllBitsSet));
    private static readonly int TBytes = TBits / 8;

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public record Algorithm(string Name, CompressFunc Compress, DecompressFunc Decompress, int BlockSize)
    {
        public override string ToString() => Name;
    }

    private static IEnumerable<T> Starts()
    {
        T half = T.MaxValue >> 1;
        T halfSize = T.MaxValue >> (TBits / 2);

        if (T.IsNegative(T.MinValue))
        {
            yield return T.MinValue + halfSize;
            yield return -half;
            yield return -halfSize;
        }

        yield return T.Zero;
        yield return halfSize;
        yield return half;
        yield return T.MaxValue - halfSize;
    }

    private static IEnumerable<int> Lengths()
    {
        yield return 1;
        yield return 10;

        for (var i = 32; i <= 2048; i <<= 1)
        {
            yield return i - 1;
            yield return i;
            yield return i + 1;
        }
    }

    private static IEnumerable<int> Deltas()
    {
        yield return 10;
        yield return 20;
        yield return 50;
        yield return 100;
        yield return 1000;
    }

    [Test]
    public void Increasing_Consecutive(
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length
    )
    {
        T[] values = Range(start, length).ToArray();
        Verify(values);
    }

    [Test]
    public void Increasing_Consecutive_Negative(
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length
    )
    {
        T[] values = Range(start, length).Reverse().Select(x => -x).ToArray();
        Verify(values);
    }

    [Test]
    public void Increasing_Random(
        [Values(42, 4242, 424242)] int seed,
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length,
        [ValueSource(nameof(Deltas))] int maxDelta
    )
    {
        T[] values = RandomIncreasingRange(new Random(seed), start, length, maxDelta).ToArray();
        Verify(values);
    }

    [Test]
    public void Increasing_Random_Negative(
        [Values(42, 4242, 424242)] int seed,
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length,
        [ValueSource(nameof(Deltas))] int maxDelta
    )
    {
        T[] values = RandomIncreasingRange(new Random(seed), start, length, maxDelta).Reverse().Select(x => -x).ToArray();
        Verify(values);
    }

    [Test]
    public void Decreasing_Consecutive(
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length
    )
    {
        T[] values = Range(start, length).Reverse().ToArray();
        Verify(values);
    }

    [Test]
    public void Decreasing_Random(
        [Values(42, 4242, 424242)] int seed,
        [ValueSource(nameof(Starts))] T start,
        [ValueSource(nameof(Lengths))] int length,
        [ValueSource(nameof(Deltas))] int maxDelta
    )
    {
        T[] values = RandomIncreasingRange(new Random(seed), start, length, maxDelta).Reverse().ToArray();
        Verify(values);
    }

    private static IEnumerable<T> Range(T start, int length)
    {
        for (T i = start; i < checked(start + T.CreateChecked(length)); i++)
            yield return i;
    }

    private static IEnumerable<T> RandomIncreasingRange(Random random, T start, int length, int maxDelta)
    {
        T value = start;

        for (var i = 0; i < length; i++)
        {
            try
            {
                value = checked(value + T.CreateChecked(random.Next(maxDelta)));
            }
            catch (OverflowException)
            {
                Assert.Ignore("Range overflow");
            }

            yield return value;
        }
    }

    private void Verify(T[] values)
    {
        if (!TurboPFor.Supports256Blocks && algorithm.BlockSize == 256)
            Assert.Ignore("256 blocks are not supported on this platform.");

        var compressed = Compress(values, algorithm.Compress);
        var decompressed = Decompress(compressed, values.Length, algorithm.Decompress);

        Assert.That(decompressed, Is.EqualTo(values));
    }

    private static byte[] Compress(T[] values, CompressFunc compressFunc)
    {
        var buffer = new byte[values.Length * TBytes + 1024];

        var resultLength = (int)compressFunc(values, (nuint)values.Length, buffer);

        TestContext.Out.WriteLine($"Compressed: {values.Length * TBytes} -> {resultLength} bytes");
        return buffer[..resultLength];
    }

    private static T[] Decompress(byte[] data, int count, DecompressFunc decompressFunc)
    {
        var buffer = new T[count + 1];
        Array.Fill(buffer, -T.One, count, buffer.Length - count);

        _ = decompressFunc(data, (nuint)count, buffer);

        // Verify bytes outside the decompressed range are not touched.
        for (var i = count; i < buffer.Length; i++)
            Assert.That(buffer[i], Is.EqualTo(-T.One));

        return buffer[..count];
    }
}
