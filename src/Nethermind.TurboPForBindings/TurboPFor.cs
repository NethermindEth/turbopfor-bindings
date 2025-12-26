using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Loader;

namespace Nethermind.TurboPForBindings
{
    // https://github.com/brettwooldridge/TurboPFor#function-syntax
    // https://github.com/brettwooldridge/TurboPFor/blob/master/java/jic.java
    // https://github.com/brettwooldridge/TurboPFor/blob/master/vp4.h
    public static partial class TurboPFor
    {
        private const string LibraryName = "ic";
        private static string? _libraryFallbackPath;

        /// <summary>
        /// If <c>false</c>, methods using 256 blocks will throw an exception.
        /// </summary>
        public static bool Supports256Blocks => Avx2.IsSupported;

        static TurboPFor() => AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnResolvingUnmanagedDll;

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc128v32(ReadOnlySpan<int> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec128v32(ReadOnlySpan<byte> @in, nuint n, Span<int> @out);

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc128v32(ReadOnlySpan<uint> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec128v32(ReadOnlySpan<byte> @in, nuint n, Span<uint> @out);

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc256v32(ReadOnlySpan<int> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec256v32(ReadOnlySpan<byte> @in, nuint n, Span<int> @out);

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc256v32(ReadOnlySpan<uint> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec256v32(ReadOnlySpan<byte> @in, nuint n, Span<uint> @out);

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc64(ReadOnlySpan<long> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec64(ReadOnlySpan<byte> @in, nuint n, Span<long> @out);

        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1enc64(ReadOnlySpan<ulong> @in, nuint n, Span<byte> @out);
        [LibraryImport(LibraryName)]
        public static partial nuint p4nd1dec64(ReadOnlySpan<byte> @in, nuint n, Span<ulong> @out);

        private static IntPtr OnResolvingUnmanagedDll(Assembly context, string name)
        {
            if (!LibraryName.Equals(name, StringComparison.Ordinal))
                return nint.Zero;

            if (_libraryFallbackPath is null)
            {
                string platform;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    name = $"lib{name}.so";
                    platform = "linux";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    name = $"lib{name}.dylib";
                    platform = "osx";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    name = $"{name}.dll";
                    platform = "win";
                }
                else
                    throw new PlatformNotSupportedException();

                var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

                _libraryFallbackPath = Path.Combine("runtimes", $"{platform}-{arch}", "native", name);
            }

            return NativeLibrary.Load(_libraryFallbackPath, context, default);
        }
    }
}
