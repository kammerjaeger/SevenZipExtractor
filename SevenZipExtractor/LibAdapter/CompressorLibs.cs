using Microsoft.VisualBasic;
using SevenZipExtractor.LibAdapter;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipExtractor.LibAdapter 
    {
    internal struct CoderInfo {
        public LibBase lib;
        public uint CoderIndex;
    }

    internal struct HasherInfo {
        public LibBase lib;
        public uint hasherIndex;
    }

    /// <summary>
    /// C# implementation inspired by LoadCodecs.cpp from the 7zip project
    /// </summary>
    public class CompressorLibs : ICompressCodecsInfo, IHashers {
        public ShadowContainer Shadow { get; set; }
        private WeakReference<SevenZipHandle> libHandle;
        private uint refCount { get; set; } = 0;

        public uint TotalNumMethods => (uint)coders.Count;

        private List<CoderInfo> coders = new List<CoderInfo>();
        private List<HasherInfo> hashers = new List<HasherInfo>();
        private List<ArcInfo> formats = new List<ArcInfo>();

        private List<LibBase> compressorLibs = new List<LibBase>();
        public SevenZipLib SevenZipLib { get; init; }

        public CompressorLibs(string sevenZipPath, string libPath, string LibraryExt, WeakReference<SevenZipHandle> libHandle) {
            this.libHandle = libHandle;

            SevenZipLib = new SevenZipLib(sevenZipPath);
            LoadLib(SevenZipLib);

            if (!Directory.Exists(libPath)) {
                return;
            }
            string searchPattern = $"*{LibraryExt}";
            string[] filePaths = Directory.GetFiles(libPath, searchPattern,
                                         SearchOption.TopDirectoryOnly);

            foreach (var lib in filePaths)
            {
                if (File.Exists(lib)) {
                    var libObj = new CompressorLib(lib);
                    LoadLib(libObj);
                }
            }

            // set lib codec loader
            foreach (var lib in compressorLibs) {
                lib.SetCodecs(this);
            }
        }

        public void LoadLib(LibBase libObj) {
            compressorLibs.Add(libObj);
            // load coders
            var numCoders = libObj.GetNumberOfMethods();
            for (uint i = 0; i < numCoders; i++) {
                coders.Add(new CoderInfo() {
                    lib = libObj,
                    CoderIndex = i,
                });
            }

            // load hashers
            var hashersObj = libObj.Hashers;
            var numHasher = hashersObj?.GetNumHashers() ?? 0;
            for (uint i = 0; i < numHasher; i++) {
                hashers.Add(new HasherInfo() {
                    lib = libObj,
                    hasherIndex = i,
                });
            }

            // load formats
            // Todo only if lib has CreateObject
            LoadFromats(libObj);
        }

        private void LoadFromats(LibBase lib) {
            foreach(var format in lib.Formats) {
                formats.Add(format);
            }
        }

        public IInArchive CreateInArchive(Guid classId) {
            return SevenZipLib.CreateInArchive(classId);
        }

        public int CreateDecoder(uint index, Guid iid, ref IntPtr coder)
        {
            if (coders.Count <= index) {
                coder = IntPtr.Zero;
                return HResults.E_FAIL;
            }
            var offset = coders[(int) index];
            return offset.lib.CreateDecoder(offset.CoderIndex, iid, ref coder);
        }

        public int CreateEncoder(uint index, Guid iid, ref IntPtr coder) 
        {
            if (coders.Count <= index) {
                coder = IntPtr.Zero;
                return HResults.E_FAIL;
            }
            var offset = coders[(int)index];
            return offset.lib.CreateEncoder(offset.CoderIndex, iid, ref coder);
        }

        public void Dispose()
        {
            foreach (var lib in compressorLibs) {
                lib.Dispose();
            }
            coders.Clear();
            compressorLibs.Clear();
        }

        public int GetNumMethods(out uint numMethods)
        {
            numMethods = TotalNumMethods;
            return HResults.S_OK;
        }

        public int GetProperty(uint index, NMethodPropID propID, ref Variant value) {
            if (coders.Count <= index) {
                return HResults.E_FAIL;
            }
            var offset = coders[(int)index];
            return offset.lib.GetMethodProperty(offset.CoderIndex, propID, ref value);
        }
        public T? GetPropertyOrDefault<T>(uint index, NMethodPropID propID) {
            if (coders.Count <= index) {
                return default;
            }
            var offset = coders[(int)index];
            return offset.lib.GetMethodPropertyOrDefault<T>(offset.CoderIndex, propID);
        }
        public bool TryGetMethodProperty<T>(uint index, NMethodPropID propID, out T? value) {
            if (coders.Count <= index) {
                value = default;
                return false;
            }
            var offset = coders[(int)index];
            return offset.lib.TryGetMethodProperty<T>(offset.CoderIndex, propID, out value);
        }        

        public uint AddRef() {
            if (refCount == 0) {
                if (libHandle.TryGetTarget(out var handle)) {
                    handle.AddKeepAlive(this);
                }
            }
            refCount++;
            return refCount;
        }
        public uint Release() {
            refCount--;
            if (refCount == 0) {
                if (libHandle.TryGetTarget(out var handle)) {
                    handle.RemoveKeepAlive(this);
                }
            }
            return refCount;
        }

        public uint GetNumHashers() {
            return (uint)hashers.Count;
        }

        public int GetHasherProp(uint index, NMethodPropID propID, ref Variant value) {
            var hashinfo = hashers[(int) index];
            return hashinfo.lib.Hashers.GetHasherProp(hashinfo.hasherIndex, propID, ref value);
        }

        public int CreateHasher(uint index, out IHasher hasher) {
            var hashinfo = hashers[(int)index];
            return hashinfo.lib.Hashers.CreateHasher(hashinfo.hasherIndex, out hasher);
        }

        public ArcInfo? FindFormatForArchiveName(string archivePath) {
            var ext = Path.GetExtension(archivePath);
            var dotPos = ext.LastIndexOf('.');
            if (dotPos != -1) {
                ext = ext.Substring(dotPos + 1);
            }
            if (string.IsNullOrEmpty(ext) || ext.Equals("exe", StringComparison.InvariantCultureIgnoreCase)){
                return null;
            }
            return formats.FirstOrDefault(frm => frm.Extensions.Any(currExt => currExt.Ext.Equals(ext, StringComparison.InvariantCultureIgnoreCase)));
        }

        public ArcInfo? FindFormatForExtension(string ext) {
            var dotPos = ext.LastIndexOf('.');
            if (dotPos != -1) {
                ext = ext.Substring(dotPos + 1);
            }

            if (string.IsNullOrEmpty(ext) || ext.Equals("exe", StringComparison.InvariantCultureIgnoreCase)){
                return null;
            }
            return formats.FirstOrDefault(frm => frm.Extensions.Any(currExt => currExt.Ext.Equals(ext, StringComparison.InvariantCultureIgnoreCase)));
        }

        public ArcInfo? FindFormatForArchiveType(string type) {
            if (string.IsNullOrEmpty(type)) {
                return null;
            }
            return formats.FirstOrDefault(frm => frm.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Finds all formats that fit the data and filters 
        /// </summary>
        /// <param name="probableFormats"></param>
        /// <param name="unliklyFormats"></param>
        /// <param name="stream"></param>
        /// <param name="extension"></param>
        public void FindFormatForDataAndExt(out List<ArcInfo> probableFormats, out List<ArcInfo> unliklyFormats, Stream stream, string? extension = null) {
            FindFormatFromData(out probableFormats, out unliklyFormats, stream);

            // if filename / extension is not set return with current result
            if (extension == null) {
                return;
            }

            var dotPos = extension.LastIndexOf('.');
            if (dotPos != -1) {
                extension = extension.Substring(dotPos + 1);
            }

            var moreLikely = probableFormats.FindAll(format => format.Extensions.Any(x => x.Ext.Equals(extension, StringComparison.InvariantCultureIgnoreCase)));
            // check if any found format have the correct ending
            if (moreLikely.Any()) {
                foreach (var format in probableFormats) {
                    if (!moreLikely.Contains(format)) {
                        unliklyFormats.Add(format);
                    }
                }
                probableFormats = moreLikely;
            }
        }

        /// <summary>
        /// Gets the p
        /// </summary>
        /// <param name="possibleFormats"></param>
        /// <param name="stream"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public void FindFormatFromData(out List<ArcInfo> probableFormats, out List<ArcInfo> unliklyFormats, Stream stream) {
            const int maxSize = 1 << 20; // it must be larger than max signature offset or IsArcFunc offset ((1 << 19) + x for UDF)
            bool endOfFile;

            byte[] archiveFileSignature = new byte[maxSize];
            int bytesRead = 0;
            int lastRead = stream.Read(archiveFileSignature, 0, maxSize);
            while (lastRead != 0) {
                bytesRead += lastRead;
                lastRead = stream.Read(archiveFileSignature, bytesRead, maxSize - bytesRead);
            }
            endOfFile = bytesRead < maxSize;

            ReadOnlySpan<byte> archiveFileSignatureSpan = new ReadOnlySpan<byte> ( archiveFileSignature, 0, bytesRead );

            stream.Position -= bytesRead; // go back to the beginning

            probableFormats = new List<ArcInfo>();
            unliklyFormats = new List<ArcInfo>();
            foreach (var format in formats) {
                if (format.IsArc != null) {
                    var arc = format.IsArc(archiveFileSignatureSpan);
                    if (arc == IsArcResult.k_IsArc_Res_NO ||
                        arc == IsArcResult.k_IsArc_Res_NEED_MORE && endOfFile) {
                        continue;
                    } else {
                        probableFormats.Add(format);
                        continue;
                    }
                }
                bool tryOpen = !format.Signatures.Any()
                      || format.Flags.HasFlag(NArcInfoFlags.kPureStartOpen)
                      || format.Flags.HasFlag(NArcInfoFlags.kStartOpen)
                      || format.Flags.HasFlag(NArcInfoFlags.kBackwardOpen);

                if (format.Signatures.Any()) {
                    foreach(var sig in format.Signatures) {
                        if (archiveFileSignatureSpan.Length < format.SignatureOffset + sig.Length) {
                            // sig is longer then read bytes, have to check directly
                            tryOpen = true; 
                            continue;
                        }
                        var sigSpan = new ReadOnlySpan<byte>(sig);
                        if (archiveFileSignatureSpan
                            .Slice((int)format.SignatureOffset, sig.Length)
                            .SequenceEqual(sigSpan)) {
                            tryOpen = false;
                            probableFormats.Add(format);
                            break;
                        }
                    }
                }
                if (tryOpen) {
                    unliklyFormats.Add(format);
                }
            }
        }
    }
}
