using Microsoft.Win32.SafeHandles;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SevenZipExtractor.LibAdapter
{
    public sealed class SevenZipHandle : IDisposable {
        public const uint LinuxBstrSize_Max = uint.MaxValue;
        public const uint LinuxBstrCharSize = 4;
        public const uint LinuxBstrSizeTypeSize = 4;

        public static int FuncOffset = 0;
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static SevenZipHandle? currentInstance = null;
        private static object initLock = new object();
        //public static SevenZipHandle? GetInstance { get { 
        //    if (Singleton == null) {
        //            InitializeAndValidateLibrary();
        //        }
        //    return Singleton;
        //    } }

        static SevenZipHandle() {
            // Linux p7zip lib is using two destructures that are virtual and are part of the vtable (Function table of a c++ object)
            // Windows does not do that so the functions are not part of the vtable
            if (IsLinux) {
                FuncOffset = 2;
            }
        }

        public CompressorLibs Libs { get; init; }

        private HashSet<IUnknown> keepAliveSet = new HashSet<IUnknown>();
        private bool disposedValue;


        /// <summary>
        /// Initializes or returns the current lib instance, not really thread safe but makes sure that multiple calls to it give the same result
        /// </summary>
        /// <param name="libraryFilePath"></param>
        /// <returns></returns>
        /// <exception cref="SevenZipException"></exception>
        public static SevenZipHandle InitializeAndValidateLibrary(string? libraryFilePath = null) {
            lock (initLock) {
                if (currentInstance != null) {
                    return currentInstance;
                }

                string LibraryExt;
                if (SevenZipHandle.IsWindows) {
                    LibraryExt = ".dll";
                } else if (SevenZipHandle.IsLinux) {

                    LibraryExt = ".so";
                } else {
                    throw new SevenZipException("OS not supported");
                }

                if (string.IsNullOrWhiteSpace(libraryFilePath)) {
                    string currentArchitecture = IntPtr.Size == 4 ? "x86" : "x64"; // magic check
                    var libName = $"7z{LibraryExt}";

                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + LibraryExt))) {
                        libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + LibraryExt);
                    } else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + LibraryExt))) {
                        libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + LibraryExt);
                    } else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, libName))) {
                        libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, libName);
                    } else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, libName))) {
                        libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, libName);
                    } else if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", libName))) {
                        libraryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", libName);
                    }
                }

                if (string.IsNullOrWhiteSpace(libraryFilePath)) {
                    throw new SevenZipException("libraryFilePath not set");
                }

                if (!File.Exists(libraryFilePath)) {
                    throw new SevenZipException("7z.dll not found");
                }

                try {
                    return new SevenZipHandle(libraryFilePath, LibraryExt);
                } catch (Exception e) {
                    throw new SevenZipException("Unable to initialize SevenZipHandle", e);
                }
            }
        }

        private SevenZipHandle(string sevenZipLibPath, string extension) {
            var dirInfo = new DirectoryInfo(sevenZipLibPath);
            string libPath = Path.Combine(dirInfo.Parent.FullName, "Codecs");
            Libs = new CompressorLibs(sevenZipLibPath, libPath, extension, new WeakReference<SevenZipHandle>(this));
            currentInstance = this;
        }

        public void AddKeepAlive(IUnknown obj)
        {
            keepAliveSet.Add(obj);
        }
        public void RemoveKeepAlive(IUnknown obj)
        {
            keepAliveSet.Remove(obj);
        }

        private void Dispose(bool disposing) {

            if (!disposedValue) {
                if (disposing) {
                    if (keepAliveSet.Any()) {
                        // log error
                    }
                    Libs.Dispose();
                }

                keepAliveSet.Clear();
                disposedValue = true;
                currentInstance = null;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region static functions
        public static int PropVariantClear(ref Variant pvar)
        {
            if (IsWindows)
            {
                return WindowsPropVariantClear(ref pvar);
            }
            else if (IsLinux)
            {
                if (currentInstance == null)
                {
                    return HResults.E_FAIL;
                }
                return currentInstance.Libs.SevenZipLib.LinuxPropVariantClear(ref pvar);
            }
            else
            {
                throw new SevenZipException("OS not supported");
            }
        }

        [DllImport("ole32.dll", EntryPoint = "PropVariantClear")]
        private static extern int WindowsPropVariantClear(ref Variant pvar);

        public static IntPtr StringToBSTR(string s)
        {
            if (IsWindows)
            {
                return Marshal.StringToBSTR(s);
            }
            else if (IsLinux) {
                if (currentInstance == null) {
                    return IntPtr.Zero;
                }
                return currentInstance.Libs.SevenZipLib.LinuxStringToBSTR(s);
            }
            else
            {
                throw new SevenZipException("OS not supported");
            }
        }

        /// <summary>
        /// Converts a Ptr to a BSTR string into a normal string.
        /// 7Zip uses 32 bit char in linux, not sure if it is UTF32 or Unicode 16 with 4 bytes
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        /// <exception cref="SevenZipException"></exception>
        public static unsafe string PtrToStringBSTR(IntPtr ptr)
        {
            if (IsWindows)
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            else if (IsLinux)
            {
                return new string((sbyte*)ptr, 0, (int)SysStringByteLen(ptr), Encoding.UTF32);
            }
            else
            {
                throw new SevenZipException("OS not supported");
            }
        }

        /// <summary>
        /// Gets the BSTR string len in bytes
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static unsafe uint SysStringByteLen(IntPtr s)
        {
            return *((uint*)s - 1);
        }

        #endregion static functions
    }
}