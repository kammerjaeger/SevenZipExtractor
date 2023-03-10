using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipExtractor.LibAdapter {
    public class SevenZipLib : LibBase {
        private SysAllocStringLenDelegate LinuxSysAllocStringLen { get; init; }
        public PropVariantClearDelegate LinuxPropVariantClear { get; init; }

        public SevenZipLib(string sevenZipLibPath) : base(sevenZipLibPath) {
            IntPtr functionPtr = NativeLibrary.GetExport(handle, "GetHandlerProperty");

            // Not valid dll
            if (functionPtr == IntPtr.Zero) {
                Dispose();
                throw new ArgumentException();
            }

            // setup com object functions on linux
            if (SevenZipHandle.IsLinux) {
                IntPtr LinuxSysAllocStringLenAddress = NativeLibrary.GetExport(handle, "SysAllocStringLen");
                LinuxSysAllocStringLen = Marshal.GetDelegateForFunctionPointer<SysAllocStringLenDelegate>(LinuxSysAllocStringLenAddress);

                IntPtr LinuxPropVariantClearAddress = NativeLibrary.GetExport(handle, "VariantClear");
                LinuxPropVariantClear = Marshal.GetDelegateForFunctionPointer<PropVariantClearDelegate>(LinuxPropVariantClearAddress);
            } else {
                LinuxSysAllocStringLen = (_, __) => throw new NotSupportedException("Only supported on Linux");
                LinuxPropVariantClear = (ref Variant _) => throw new NotSupportedException("Only supported on Linux");
            }
        }
        public unsafe IntPtr LinuxStringToBSTR(string s) {
            int nb = Encoding.UTF32.GetMaxByteCount(s.Length);

            IntPtr ptr = Marshal.AllocHGlobal(nb + 1);
            try {

                int nbWritten;
                byte* pbMem = (byte*)ptr;

                fixed (char* firstChar = s) {
                    nbWritten = Encoding.UTF32.GetBytes(firstChar, s.Length, pbMem, nb);
                }

                pbMem[nbWritten] = 0;
                return LinuxSysAllocStringLen(ptr, (uint)nbWritten / 4);
            } finally {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IntPtr SysAllocStringLenDelegate([In] IntPtr s, uint len);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate int PropVariantClearDelegate(ref Variant pvar);
}
