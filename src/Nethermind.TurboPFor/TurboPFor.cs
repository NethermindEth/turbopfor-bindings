using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Nethermind.TurboPFor
{
    // https://github.com/brettwooldridge/TurboPFor#function-syntax
    // https://github.com/brettwooldridge/TurboPFor/blob/master/java/jic.java
    // https://github.com/brettwooldridge/TurboPFor/blob/master/vp4.h
    public static partial class TurboPFor
    {
        private const string LibraryName = "ic";
        private static string? _libraryFallbackPath;

        static TurboPFor() => AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnResolvingUnmanagedDll;

        [LibraryImport(LibraryName)]
        public static unsafe partial nuint p4nd1enc128v32(int* @in, nuint n, byte* @out);
        [LibraryImport(LibraryName)]
        public static unsafe partial nuint p4nd1dec128v32(byte* @in, nuint n, int* @out);

        [LibraryImport(LibraryName)]
        public static unsafe partial nuint p4nd1enc256v32(int* @in, nuint n, byte* @out);
        [LibraryImport(LibraryName)]
        public static unsafe partial nuint p4nd1dec256v32(byte* @in, nuint n, int* @out);

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
