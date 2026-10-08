// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Nethermind.TurboPForBindings.Tests;

/// <summary>
/// Pins the encoded format of the shipped binaries so that a TurboPFor update can't silently
/// break data encoded by an earlier version. The vectors must only be regenerated deliberately,
/// after verifying that the new binaries decode the existing ones.
/// </summary>
[Parallelizable(ParallelScope.All)]
public class TurboPForGoldenVectorTests
{
    private const string FileName = "GoldenVectors.json";
    private const string Revision = "da4fa61f6dedee61a3c0c624fc8656d84b820392";

    private static readonly Lazy<GoldenVectors> Expected = new(Load);

    public record Codec(string Name, int Bits, int BlockSize)
    {
        public byte[] Encode(ulong[] values)
        {
            var buffer = new byte[values.Length * (Bits / 8) + 1024];
            var n = (nuint)values.Length;
            uint[] values32 = values.Select(v => (uint)v).ToArray();

            nuint length = Name switch
            {
                P4nd1_128v32 => TurboPFor.p4nd1enc128v32(values32, n, buffer),
                P4nd1_256v32 => TurboPFor.p4nd1enc256v32(values32, n, buffer),
                P4nd1_64 => TurboPFor.p4nd1enc64(values, n, buffer),
                _ => throw new NotSupportedException(Name)
            };

            return buffer[..(int)length];
        }

        public (ulong[] Values, int BytesRead) Decode(byte[] data, int count)
        {
            var n = (nuint)count;
            var values32 = new uint[count];
            var values64 = new ulong[count];

            nuint read = Name switch
            {
                P4nd1_128v32 => TurboPFor.p4nd1dec128v32(data, n, values32),
                P4nd1_256v32 => TurboPFor.p4nd1dec256v32(data, n, values32),
                P4nd1_64 => TurboPFor.p4nd1dec64(data, n, values64),
                _ => throw new NotSupportedException(Name)
            };

            return (Bits == 32 ? values32.Select(v => (ulong)v).ToArray() : values64, (int)read);
        }

        public override string ToString() => Name;
    }

    public record Case(Codec Codec, string Name, int Length, ulong Start, Func<SplitMix64, ulong> NextGap)
    {
        public string Key => $"{Codec}/{Name}";

        public ulong[] Values()
        {
            var random = new SplitMix64(42);
            var values = new ulong[Length];
            ulong value = Start;

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = value;
                value += NextGap(random);
            }

            return values;
        }

        public override string ToString() => Key;
    }

    public sealed class SplitMix64(ulong seed)
    {
        private ulong _state = seed;

        /// <summary>Returns a value in [0, max).</summary>
        public ulong Next(ulong max)
        {
            ulong z = _state += 0x9E3779B97F4A7C15;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return (z ^ (z >> 31)) % max;
        }
    }

    public record GoldenVectors(string Revision, SortedDictionary<string, string> Vectors);

    private const string P4nd1_128v32 = "p4nd1*128v32";
    private const string P4nd1_256v32 = "p4nd1*256v32";
    private const string P4nd1_64 = "p4nd1*64";

    private static readonly Codec[] Codecs =
    [
        new(P4nd1_128v32, Bits: 32, BlockSize: 128),
        new(P4nd1_256v32, Bits: 32, BlockSize: 256),
        new(P4nd1_64, Bits: 64, BlockSize: 128)
    ];

    public static IEnumerable<Case> Cases()
    {
        foreach (Codec codec in Codecs)
        {
            ulong max = codec.Bits == 32 ? uint.MaxValue : ulong.MaxValue;

            yield return new(codec, "delta1", 1000, 0, _ => 1);
            yield return new(codec, "small-gaps", 1000, 0, r => 1 + r.Next(16));
            // Rare large gaps exercise the PFor exception path.
            yield return new(codec, "exceptions", 1000, 0, r => r.Next(32) == 0 ? 1 + r.Next(1 << 20) : 1 + r.Next(8));
            yield return new(codec, "wide-gaps", 500, 0, r => 1 + r.Next(1UL << (codec.Bits - 10)));
            yield return new(codec, "high-start", 1000, max - 0xFFFF, r => 1 + r.Next(16));

            foreach (var length in (int[])[1, 2, 127, 128, 129, 255, 256, 257])
                yield return new(codec, $"length-{length}", length, 1000, r => 1 + r.Next(16));
        }
    }

    [TestCaseSource(nameof(Cases))]
    public void Decodes_golden_vector(Case @case)
    {
        IgnoreIfUnsupported(@case.Codec);

        byte[] encoded = Convert.FromHexString(Expected.Value.Vectors[@case.Key]);
        (ulong[] values, int bytesRead) = @case.Codec.Decode(encoded, @case.Length);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(values, Is.EqualTo(@case.Values()));
            Assert.That(bytesRead, Is.EqualTo(encoded.Length));
        }
    }

    // Byte equality can't be asserted: the encoder bit-packs from a partially uninitialized stack
    // buffer (_in in _p4enc, lib/vp4c.c), so the padding bits after the last value vary between calls.
    [TestCaseSource(nameof(Cases))]
    public void Encodes_compatibly_with_golden_vector(Case @case)
    {
        IgnoreIfUnsupported(@case.Codec);

        ulong[] values = @case.Values();
        byte[] encoded = @case.Codec.Encode(values);
        byte[] expected = Convert.FromHexString(Expected.Value.Vectors[@case.Key]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encoded, Has.Length.EqualTo(expected.Length));
            Assert.That(@case.Codec.Decode(encoded, @case.Length).Values, Is.EqualTo(values));
        }
    }

    [Test]
    public void Golden_vectors_match_cases()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Expected.Value.Revision, Is.EqualTo(Revision));
            Assert.That(Expected.Value.Vectors.Keys, Is.EquivalentTo(Cases().Select(c => c.Key)));
        }
    }

    [Test]
    [Explicit("Regenerates the golden vectors from the current binaries")]
    public void Regenerate_golden_vectors()
    {
        if (!TurboPFor.Supports256Blocks)
            Assert.Fail("Regenerating requires 256 block support.");

        var vectors = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (Case @case in Cases())
            vectors.Add(@case.Key, Convert.ToHexString(@case.Codec.Encode(@case.Values())));

        var json = JsonSerializer.Serialize(new GoldenVectors(Revision, vectors), JsonOptions);
        File.WriteAllText(Path.Combine(SourceDirectory(), FileName), json + "\n");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static GoldenVectors Load()
    {
        using Stream stream = typeof(TurboPForGoldenVectorTests).Assembly.GetManifestResourceStream(FileName)
            ?? throw new InvalidOperationException($"{FileName} is not embedded.");

        GoldenVectors vectors = JsonSerializer.Deserialize<GoldenVectors>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"{FileName} is empty.");

        return vectors with { Vectors = new(vectors.Vectors, StringComparer.Ordinal) };
    }

    private static void IgnoreIfUnsupported(Codec codec)
    {
        if (!TurboPFor.Supports256Blocks && codec.BlockSize == 256)
            Assert.Ignore("256 blocks are not supported on this platform.");
    }

    private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
