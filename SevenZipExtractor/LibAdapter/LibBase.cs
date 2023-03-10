using Microsoft.Win32.SafeHandles;
using SharpGen.Runtime;
using SharpGen.Runtime.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SevenZipExtractor.LibAdapter {
    /// <summary>
    /// Archive information
    /// </summary>
    public struct ArcInfo {
        public LibBase Lib;
        public uint FormatIndex;
        public string Name;
        public Guid ClassID;
        public bool NewInterface;
        public NArcInfoFlags Flags;
        public uint TimeFlags;
        public IsArcFunction? IsArc;
        public List<ArcExtensionInfo> Extensions;
        public bool UpdateEnabled;

        public List<byte[]> Signatures;
        public uint SignatureOffset;

        public void AddExtensionString(string ext, string addExt) {
            Extensions = Extensions ?? new List<ArcExtensionInfo>();

            var exts = ext.Split(" ");
            var addExts = addExt.Split(" ");

            Extensions.AddRange(exts.Select((ext, index) => {
                if (index < addExts.Length) {
                    var addExt = addExts[index] == "*" ? string.Empty : addExts[index];
                    return new ArcExtensionInfo() {
                        Ext = ext,
                        AddExt = addExt,
                    };
                }
                return new ArcExtensionInfo() {
                    Ext = ext,
                    AddExt = string.Empty,
                };
            }));
        }
    };

    /// <summary>
    /// Archive extension information
    /// </summary>
    public struct ArcExtensionInfo {
        public string Ext;
        public string AddExt;
    };

    /// <summary>
    /// Base class for 7zip libs
    /// </summary>
    public abstract class LibBase : SafeHandleZeroOrMinusOneIsInvalid {
        private CreateDecoderDelegate _CreateDecoder { get; init; }
        private CreateEncoderDelegate _CreateEncoder { get; init; }
        private GetMethodPropertyDelegate _GetMethodProperty { get; init; }
        private GetNumberOfMethodsDelegate _GetNumberOfMethods { get; init; }
        private GetHandlerPropertyDelegate? _GetHandlerProperty { get; init; }
        private GetHandlerProperty2Delegate? _GetHandlerProperty2 { get; init; }
        private GetNumberOfFormatsDelegate? _GetNumberOfFormats { get; init; }
        private GetHashersDelegate _GetHashers { get; init; }
        private SetCodecsDelegate? _SetCodecs { get; init; }
        private CreateObjectDelegate _CreateObject { get; init; }
        private SetCaseSensitiveDelegate? _SetCaseSensitive { get; init; }
        private SetLargePageModeDelegate? _SetLargePageMode { get; init; }
        private GetIsArcDelegate? _GetIsArc { get; init; }

        public string LibPath { get; init; }
        public string LibName { get; init; }

        private List<ArcInfo>? _Formats = null;
        public List<ArcInfo> Formats {
            get { 
                if (_Formats == null) {
                    // TODO maybe log if there was a previous value so that multitasking can be avoided/fixed
                    Interlocked.Exchange(ref _Formats, GetFormats());
                }
                return _Formats;
            }
        }

        private bool initHashers = false;
        private IHashers? _hashers = null;

        /// <summary>
        /// Gets the hashers object, not thread safe
        /// </summary>
        public IHashers Hashers {
            get {
                if (!initHashers) {
                    if (_GetHashers(out var hashersPtr) == HResults.S_OK) {
                        _hashers = new IHashersNative(hashersPtr);
                    } else {
                        _hashers = null;
                    }
                }
                return _hashers;
            }
        }

        public LibBase(string path) : base(true) {
            LibPath = path;
            LibName = Path.GetFileNameWithoutExtension(LibPath);
            handle = NativeLibrary.Load(path);
            if (IsInvalid) {
                throw new Win32Exception();
            }

            _CreateDecoder = LoadFunction<CreateDecoderDelegate>("CreateDecoder");
            _CreateEncoder = LoadFunction<CreateEncoderDelegate>("CreateEncoder");
            _GetMethodProperty = LoadFunction<GetMethodPropertyDelegate>("GetMethodProperty");
            _GetNumberOfMethods = LoadFunction<GetNumberOfMethodsDelegate>("GetNumberOfMethods");
            _GetHashers = LoadFunction<GetHashersDelegate>("GetHashers");
            _SetCodecs = LoadFunctionOrDefault<SetCodecsDelegate>("SetCodecs");
            _CreateObject = LoadFunction<CreateObjectDelegate>("CreateObject");
            _SetCaseSensitive = LoadFunctionOrDefault<SetCaseSensitiveDelegate>("SetCaseSensitive");
            _SetLargePageMode = LoadFunctionOrDefault<SetLargePageModeDelegate>("SetLargePageMode");
            _GetIsArc = LoadFunctionOrDefault<GetIsArcDelegate>("GetIsArc");

            _GetHandlerProperty = LoadFunctionOrDefault<GetHandlerPropertyDelegate>("GetHandlerProperty");
            _GetHandlerProperty2 = LoadFunctionOrDefault<GetHandlerProperty2Delegate>("GetHandlerProperty2");
            _GetNumberOfFormats = LoadFunctionOrDefault<GetNumberOfFormatsDelegate>("GetNumberOfFormats");
        }

        /// <summary>
        /// Load function if available otherwise return null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionName"></param>
        /// <returns></returns>
        protected T? LoadFunctionOrDefault<T>(string functionName) {
            try {
                var funcAddr = NativeLibrary.GetExport(handle, functionName);
                if (funcAddr == IntPtr.Zero) {
                    return default;
                }
                return Marshal.GetDelegateForFunctionPointer<T>(funcAddr);
            } catch (EntryPointNotFoundException) {
                return default;
            }
        }

        /// <summary>
        /// Load function, if not available exception will be thrown
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionName"></param>
        /// <returns></returns>
        /// <exception cref="SevenZipException"></exception>
        protected T LoadFunction<T>(string functionName) {
            var funcAddr = NativeLibrary.GetExport(handle, functionName);
            if (funcAddr == IntPtr.Zero) {
                throw new SevenZipException($"Function {functionName} was not found in lib {LibPath}");
            }
            return Marshal.GetDelegateForFunctionPointer<T>(funcAddr);
        }

        /// <summary>Release library handle</summary>
        /// <returns>true if the handle was released</returns>
        protected override bool ReleaseHandle() {
            NativeLibrary.Free(handle);
            return true;
        }

        protected override void Dispose(bool disposing) {
            // unset codecs
            SetCodecs(null);
            base.Dispose(disposing);
        }
        public int CreateDecoder(uint index, Guid classID, ref IntPtr obj) {
            var hresult = _CreateDecoder(index, ref classID, out IntPtr objT);
            if (hresult == HResults.S_OK) {
                obj = objT;
            }
            return hresult;
        }
        public IntPtr CreateDecoder(uint index, Guid classID) {
            var result = _CreateDecoder(index, ref classID, out IntPtr obj);
            CheckResult(result, "Error creating decoder");
            return obj;
        }
        public int CreateEncoder(uint index, Guid classID, ref IntPtr obj) {
            var hresult = _CreateEncoder(index, ref classID, out IntPtr objT);
            if (hresult == HResults.S_OK) {
                obj = objT;
            }
            return hresult;
        }
        public IntPtr CreateEncoder(uint index, Guid classID) {
            var result = _CreateEncoder(index, ref classID, out IntPtr obj);
            CheckResult(result, "Error creating encoder");
            return obj;
        }
        public T? GetMethodPropertyOrDefault<T>([In] uint index, [In] NMethodPropID propID) {
            PropVariant value = new PropVariant();
            var result = _GetMethodProperty(index, propID, ref value);
            return result ==
                HResults.S_OK &&
                value.ElementType != VariantElementType.Empty
                ? (T?)value.Value : default;
        }
        public bool TryGetMethodProperty<T>([In] uint index, [In] NMethodPropID propID, out T? outValue) {
            PropVariant value = new PropVariant();
            try {
                var result = _GetMethodProperty(index, propID, ref value);
                if (result == HResults.S_OK &&
                    value.ElementType != VariantElementType.Empty /*VT_EMPTY*/) {
                    outValue = (T?)value.Value;
                    return true;
                }
                outValue = default;
                return false;
            } finally {
                value.Clear();
            }
        }
        public object? GetMethodProperty([In] uint index, [In] NMethodPropID propID) {
            PropVariant value = new PropVariant();
            try {
                var result = _GetMethodProperty(index, propID, ref value);
                CheckResult(result, "Error getting property");
                return value.Value;
            } finally {
                value.Clear();
            }
        }
        public int GetMethodProperty(uint index, NMethodPropID propID, ref PropVariant value) {
            return _GetMethodProperty(index, propID, ref value);
        }

        public uint GetNumberOfMethods() {
            uint numMethods = 1;
            CheckResult(_GetNumberOfMethods(ref numMethods), "Unable to get number of methods");
            return numMethods;
        }

        public void CheckResult(int hresult, string errorMessage) {
            if (hresult != HResults.S_OK) {
                throw new SevenZipException(errorMessage, hresult);
            }
        }

        /// <summary>
        /// Sets the additional external codec libs, 
        /// </summary>
        /// <param name="codecInfo"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="SevenZipException"></exception>
        public void SetCodecs(ICompressCodecsInfo? codecInfo) {
            if (_SetCodecs == null) {
                return;
            }

            if (IsInvalid) {
                throw new ObjectDisposedException(nameof(LibBase));
            }

            var shadowObj = CppObject.ToCallbackPtr<ICompressCodecsInfo>(codecInfo);
            var hresult = _SetCodecs(shadowObj);
            CheckResult(hresult, "Unable to set codecs");
        }

        /// <summary>
        /// Creates a new archive object for the give class id
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="SevenZipException"></exception>
        public IInArchive CreateInArchive(Guid classId) {
            if (IsInvalid) {
                throw new ObjectDisposedException(nameof(LibBase));
            }

            IntPtr result;
            Guid interfaceId = typeof(IInArchive).GUID;
            var hresult = _CreateObject(ref classId, ref interfaceId, out result);
            CheckResult(hresult, "Unable to create InArchive object");
            var newArchive = new IInArchive(result);
            return newArchive;
        }

        public bool DecoderIsAssigned(uint index) {
            return GetMethodPropertyOrDefault<bool>(index, NMethodPropID.kDecoderIsAssigned);
        }

        public bool EncoderIsAssigned(uint index) {
            return GetMethodPropertyOrDefault<bool>(index, NMethodPropID.kEncoderIsAssigned);
        }

        public bool IsFilter(uint index, out bool value) {
            return TryGetMethodProperty(index, NMethodPropID.kIsFilter, out value);
        }

        public uint NumStreams(uint index) {
            PropVariant prop = new PropVariant();
            try {
                if (GetMethodProperty(index, NMethodPropID.kPackStreams, ref prop) != HResults.S_OK)
                    return 0;
                if (prop.ElementType == VariantElementType.UInt /*VT_UI4*/)
                    return (uint)(prop.Value ?? 1);
                if (prop.ElementType == VariantElementType.Empty)
                    return 1;
                return 0;
            } finally {
                prop.Clear();
            }
        }

        public ulong GetCodecId(uint index) {
            PropVariant prop = new PropVariant();
            try {
                var hresult = GetMethodProperty(index, NMethodPropID.kID, ref prop);
                if (hresult != HResults.S_OK) {
                    throw new SevenZipException("Could not get codec id", hresult);
                }
                if (prop.ElementType != VariantElementType.ULong /*VT_UI8*/) {

                    throw new SevenZipException("Codec id has the wrong format");
                }
                return (ulong)(prop.Value ?? default(ulong));
            } finally {
                prop.Clear();
            }
        }

        public string GetCodecName(uint index) {
            PropVariant prop = new PropVariant();
            try {
                var hresult = GetMethodProperty(index, NMethodPropID.kName, ref prop);
                if (hresult != HResults.S_OK) {
                    throw new SevenZipException("Could not get codec name", hresult);
                }
                if (prop.ElementType != VariantElementType.BinaryString /*VT_BSTR*/) {

                    throw new SevenZipException("Codec name has the wrong format");
                }
                return (string)(prop.Value ?? "Unknown Name");
            } finally {
                prop.Clear();
            }
        }

        public ulong GetHasherId(uint index) {
            PropVariant prop = new PropVariant();
            try {
                var hresult = Hashers.GetHasherProp(index, NMethodPropID.kID, ref prop);
                if (hresult != HResults.S_OK) {
                    return 0;
                }
                if (prop.ElementType == VariantElementType.ULong /*VT_UI8*/) {
                    return 0;
                }
                return (ulong)(prop.Value ?? default(ulong));
            } finally {
                prop.Clear();
            }
        }

        public string GetHasherName(uint index) {
            PropVariant prop = new PropVariant();
            try {
                var hresult = Hashers.GetHasherProp(index, NMethodPropID.kName, ref prop);
                if (hresult != HResults.S_OK) {
                    throw new SevenZipException("Could not get hasher name", hresult);
                }
                if (prop.ElementType == VariantElementType.BinaryString /*VT_BSTR*/) {

                    throw new SevenZipException("Hasher name has the wrong format");
                }
                return (string)(prop.Value ?? "Unknown Name");
            } finally {
                prop.Clear();
            }
        }

        public ulong GetHasherDigestSize(uint index) {
            PropVariant prop = new PropVariant();
            try {
                if (Hashers.GetHasherProp(index, NMethodPropID.kDigestSize, ref prop) != HResults.S_OK)
                    return 0;
                if (prop.ElementType == VariantElementType.UInt /*VT_UI4*/)
                    return (uint)(prop.Value ?? 0);
                return 0;
            } finally {
                prop.Clear();
            }
        }

        public int SetCaseSensitive(bool caseSensitive) {
            if (_SetCaseSensitive != null) {
                return _SetCaseSensitive(caseSensitive ? 1 : 0);
            }
            return HResults.S_FALSE;
        }

        public int SetLargePageMode() {
            if (_SetLargePageMode != null) {
                return _SetLargePageMode();
            }
            return HResults.S_FALSE;
        }

        private List<ArcInfo> GetFormats() {
            var formats = new List<ArcInfo>();

            // no formats function available in the lib
            if (_GetHandlerProperty == null && _GetHandlerProperty2 == null) {
                return formats;
            }

            uint numFormats = 1;
            if (_GetNumberOfFormats != null) {
                var hresult = _GetNumberOfFormats(ref numFormats);
                if (hresult != HResults.S_OK) {
                    numFormats = 1;
                }
            }
            for (uint i = 0; i < numFormats; i++) {
                ArcInfo newFormat = new ArcInfo();
                newFormat.Lib = this;
                newFormat.FormatIndex = i;
                newFormat.Name = GetHandlerProperty<string>(i, NHandlerPropID.kName);

                PropVariant classGuid = new PropVariant();
                try {
                    if (GetHandlerProperty(i, NHandlerPropID.kClassID, ref classGuid) != HResults.S_OK) {
                        continue;
                    }
                    if (classGuid.ElementType != VariantElementType.BinaryString) {
                        continue;
                    }
                    newFormat.ClassID = (Guid)classGuid;
                } finally {
                    classGuid.Clear();
                }

                var extensions = GetHandlerProperty<string>(i, NHandlerPropID.kExtension);
                var addExtensions = GetHandlerProperty<string>(i, NHandlerPropID.kAddExtension);
                newFormat.AddExtensionString(extensions, addExtensions);

                newFormat.UpdateEnabled = GetHandlerProperty<bool>(i, NHandlerPropID.kUpdate);
                newFormat.Flags = (NArcInfoFlags)GetHandlerProperty<uint>(i, NHandlerPropID.kFlags);
                newFormat.TimeFlags = GetHandlerProperty<uint>(i, NHandlerPropID.kTimeFlags);


                PropVariant sigVariant = new PropVariant();
                try {
                    var sig = GetHandlerPropertyRaw(i, NHandlerPropID.kSignature, sigVariant);
                    newFormat.Signatures = newFormat.Signatures ?? new List<byte[]>();
                    if (sig.Length != 0) {
                        newFormat.Signatures.Add(sig.ToArray());
                    } else {
                        sig = null;
                        sigVariant.Clear();
                        sig = GetHandlerPropertyRaw(i, NHandlerPropID.kMultiSignature, sigVariant);
                        ParseSignatures(sig, newFormat.Signatures);
                    }
                } finally {
                    sigVariant.Clear();
                }

                newFormat.SignatureOffset = GetHandlerProperty<uint>(i, NHandlerPropID.kSignatureOffset);
                if (_GetIsArc != null) {
                    var result = _GetIsArc(i, out IntPtr isArcFuncAddress);
                    if (isArcFuncAddress == IntPtr.Zero) {
                        newFormat.IsArc = null;
                    } else {
                        var func = Marshal.GetDelegateForFunctionPointer<IsArcDelegate>(isArcFuncAddress);
                        newFormat.IsArc = (ReadOnlySpan<byte> bytes) => IsArcCall(func, bytes);
                    }
                }

                formats.Add(newFormat);
            }
            return formats;
        }

        /// <summary>
        /// Archive test function, test if a byte sequence is the specific archive
        /// </summary>
        /// <param name="function"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private unsafe IsArcResult IsArcCall(IsArcDelegate function, ReadOnlySpan<byte> bytes) {
            if (bytes.Length == 0) {
                return 0;
            }

            fixed (byte* ptr = &MemoryMarshal.GetReference(bytes)) {
                return function(Unsafe.AsRef<byte>(ptr), (UIntPtr)bytes.Length);
            }
        }

        static bool ParseSignatures(ReadOnlySpan<byte> bytes, List<byte[]> signatures) {
            signatures.Clear();
            var size = bytes.Length;
            var pos = 0;
            while (size != 0) {
                var len = bytes[pos];
                pos++;
                size--;
                if (len > size)
                    return false;
                signatures.Add(bytes.Slice(pos, length: len).ToArray());
                pos += len;
                size -= len;
            }
            return true;
        }

        public int GetHandlerProperty(uint index, NHandlerPropID propID, ref PropVariant value) {
            if (_GetHandlerProperty2 != null) {
                return _GetHandlerProperty2(index, propID, ref value);
            }
            return _GetHandlerProperty!(propID, ref value);
        }

        /// <summary>
        /// Returns a raw byte representation of a binary string value
        /// </summary>
        /// <param name="index"></param>
        /// <param name="propID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ReadOnlySpan<byte> GetHandlerPropertyRaw(uint index, NHandlerPropID propID, PropVariant value) {
            if (GetHandlerProperty(index, propID, ref value) == HResults.S_OK) {
                if ((value.ElementType == VariantElementType.BinaryString
                        || value.ElementType == VariantElementType.Empty)) {
                    return (ReadOnlySpan<byte>)value;
                }

                var error = new SevenZipException("Handler property value should be empty or have the correct value type");
                error.Data.Add("LibName", LibPath);
                error.Data.Add("propID", propID);
                error.Data.Add("index", index);
                throw error;
            } else {
                var error = new SevenZipException("Could not get handler property");
                error.Data.Add("LibName", LibPath);
                error.Data.Add("propID", propID);
                error.Data.Add("index", index);
                throw error;
            }
        }

        public T GetHandlerProperty<T>(uint index, NHandlerPropID propID) {
            PropVariant value = new PropVariant();
            try {
                if (GetHandlerProperty(index, propID, ref value) == HResults.S_OK) {
                    if (typeof(T) == typeof(string)
                        && (value.ElementType == VariantElementType.BinaryString
                            || value.ElementType == VariantElementType.Empty)) {
                        return (T)(value.Value ?? string.Empty);
                    } else if (typeof(T) == typeof(uint)
                        && (value.ElementType == VariantElementType.UInt
                            || value.ElementType == VariantElementType.Empty)) {
                        return (T)(value.Value ?? (uint) 0);
                    } else if (typeof(T) == typeof(bool)
                        && (value.ElementType == VariantElementType.Bool
                            || value.ElementType == VariantElementType.Empty)) {
                        return (T)(value.Value ?? false);
                    }
                    var error = new SevenZipException("Handler property value should be empty or have the correct value type");
                    error.Data.Add("LibName", LibPath);
                    error.Data.Add("propID", propID);
                    error.Data.Add("index", index);
                    throw error;
                } else {
                    var error = new SevenZipException("Could not get handler property");
                    error.Data.Add("LibName", LibPath);
                    error.Data.Add("propID", propID);
                    error.Data.Add("index", index);
                    throw error;
                }
            } finally {
                value.Clear();
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int CreateObjectDelegate(
    [In] ref Guid classID,
    [In] ref Guid interfaceID,
    out IntPtr outObject);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetNumberOfMethodsDelegate(ref uint numMethods);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetHandlerPropertyDelegate([In] NHandlerPropID propID, ref PropVariant value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetHandlerProperty2Delegate([In] uint index, [In] NHandlerPropID propID, ref PropVariant value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetNumberOfFormatsDelegate(ref uint numMethods);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetMethodPropertyDelegate([In] uint index, [In] NMethodPropID propID, ref PropVariant value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int CreateDecoderDelegate([In] uint index, [In] ref Guid classID, out IntPtr outObject);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int CreateEncoderDelegate([In] uint index, [In] ref Guid classID, out IntPtr outObject);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetHashersDelegate([Out] out IntPtr hashers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int SetCodecsDelegate(
        [In] IntPtr codecInfo);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int SetCaseSensitiveDelegate(
        [In] int caseSensitive);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int SetLargePageModeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate int GetIsArcDelegate([In] UInt32 formatIndex, [Out] out IntPtr isArcFunc);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IsArcResult IsArcDelegate([In]in byte p, [In] nuint size);

    public delegate IsArcResult IsArcFunction(ReadOnlySpan<byte> p);
}
