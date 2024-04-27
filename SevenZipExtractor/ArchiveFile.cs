using SevenZipExtractor.LibAdapter;
using SharpGen.Runtime.Win32;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SevenZipExtractor
{
    public class ArchiveFile : IDisposable, IArchiveOpenVolumeCallback, IArchiveOpenCallback {
        private SevenZipHandle sevenZipHandle;
        private readonly IInArchive archive;
        private readonly InStreamWrapper archiveStream;
        private IList<Entry>? entries;
        private readonly ILogger logger;

        public ShadowContainer? Shadow { get; set; }
        private uint refCount { get; set; } = 0;
        /// <summary>
        /// The current archive name, can change for multi volume archives
        /// </summary>
        public string? CurrentArchiveName { get; set; }
        public string? ArchivePath { get; set; }

        public ArchiveFile(SevenZipHandle sevenZipHandle, string archiveFilePath, ILogger? logger = null) {
            this.sevenZipHandle = sevenZipHandle;
            this.logger = logger ?? NullLogger.Instance;
            if (!File.Exists(archiveFilePath)) {
                throw new SevenZipException("Archive file not found");
            }
            ArchivePath = archiveFilePath;
            CurrentArchiveName = Path.GetFileName(archiveFilePath);

            Guid uuid;
            using (var tmpStream = File.OpenRead(archiveFilePath)) {
                sevenZipHandle.Libs.FindFormatForDataAndExt(out var probableFormats, out var unliklyFormats, tmpStream, CurrentArchiveName);
                if (!probableFormats.Any() && !unliklyFormats.Any()) {
                    throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");
                }
                uuid = probableFormats.Any() ? probableFormats.First().ClassID : unliklyFormats.First().ClassID;
            }

            this.archive = this.sevenZipHandle!.Libs.CreateInArchive(uuid);
            this.archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath), true, new WeakReference<SevenZipHandle>(sevenZipHandle));
            archiveStream.AddRef();
        }

        public ArchiveFile(SevenZipHandle sevenZipHandle, Stream archiveStream, SevenZipFormat? format = null, string? fileName = null, ILogger? logger = null) {
            this.sevenZipHandle = sevenZipHandle;
            this.logger = logger ?? NullLogger.Instance;
            if (archiveStream == null) {
                throw new SevenZipException("archiveStream is null");
            }
            CurrentArchiveName = fileName;

            Guid uuid;
            if (format == null) {
                List<ArcInfo> probableFormats;
                List<ArcInfo> unliklyFormats;
                sevenZipHandle.Libs.FindFormatForDataAndExt(out probableFormats, out unliklyFormats, archiveStream, CurrentArchiveName);
                if (!probableFormats.Any() && !unliklyFormats.Any()) {
                    throw new SevenZipException("Unable to guess format automatically");
                }
                uuid = probableFormats.Any() ? probableFormats.First().ClassID : unliklyFormats.First().ClassID;
            } else {
                uuid = Formats.FormatGuidMapping[format.Value];
            }

            this.archive = this.sevenZipHandle!.Libs.CreateInArchive(uuid);
            this.archiveStream = new InStreamWrapper(archiveStream, false, new WeakReference<SevenZipHandle>(sevenZipHandle));
            this.archiveStream.AddRef();
        }
        public ArchiveFile(SevenZipHandle sevenZipHandle, Stream archiveStream, Guid uuid, string? fileName = null, ILogger? logger = null) {
            this.sevenZipHandle = sevenZipHandle;
            this.logger = logger ?? NullLogger.Instance;
            if (archiveStream == null) {
                throw new SevenZipException("archiveStream is null");
            }
            CurrentArchiveName = fileName;

            this.archive = this.sevenZipHandle!.Libs.CreateInArchive(uuid);
            this.archiveStream = new InStreamWrapper(archiveStream, false, new WeakReference<SevenZipHandle>(sevenZipHandle));
            this.archiveStream.AddRef();
        }

        public void Extract(string outputFolder, bool overwrite = false) {
            this.Extract(entry => {
                string fileName = Path.Combine(outputFolder, entry.FileName ?? "NoName");

                if (entry.IsFolder) {
                    return fileName;
                }

                if (!File.Exists(fileName) || overwrite) {
                    return fileName;
                }

                return null;
            });
        }

        public void Extract(Func<Entry, string?> getOutputPath) {
            IList<Stream?> fileStreams = new List<Stream?>();

            try {
                foreach (Entry entry in Entries) {
                    string? outputPath = getOutputPath(entry);

                    if (outputPath == null) // getOutputPath = null means SKIP
                    {
                        fileStreams.Add(null);
                        continue;
                    }

                    if (entry.IsFolder) {
                        Directory.CreateDirectory(outputPath);
                        fileStreams.Add(null);
                        continue;
                    }

                    string? directoryName = Path.GetDirectoryName(outputPath!);

                    if (!string.IsNullOrWhiteSpace(directoryName)) {
                        Directory.CreateDirectory(directoryName);
                    }

                    fileStreams.Add(File.Create(outputPath));
                }
                var callback = new ArchiveStreamsCallback(fileStreams, new WeakReference<SevenZipHandle>(sevenZipHandle), logger);
                callback.AddRef();
                this.archive.Extract(null, uint.MaxValue, 0, callback);
            } finally {
                foreach (Stream? stream in fileStreams) {
                    if (stream != null) {
                        stream.Dispose();
                    }
                }
            }
        }

        public IList<Entry> Entries {
            get {
                if (this.entries != null) {
                    return this.entries;
                }

                ulong checkPos = 32 * 1024;

                int open = this.archive.Open(this.archiveStream, checkPos, CurrentArchiveName != null ? this : null);

                if (open != HResults.S_OK) {
                    throw new SevenZipException("Unable to open archive");
                }
                uint itemsCount = 0;
                this.archive.GetNumberOfItems(ref itemsCount);

                this.entries = new List<Entry>();

                for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++) {
                    string? fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                    bool isEncrypted = this.GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                    ulong size = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                    ulong packedSize = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                    DateTime creationTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastAccessTime);
                    uint crc = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidCRC);
                    uint attributes = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidAttributes);
                    string? comment = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidComment);
                    string? hostOS = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidHostOS);
                    string? method = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidMethod);

                    bool isSplitBefore = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                    bool isSplitAfter = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitAfter);

                    this.entries.Add(new Entry(this.archive, fileIndex, new WeakReference<SevenZipHandle>(sevenZipHandle)) {
                        FileName = fileName,
                        IsFolder = isFolder,
                        IsEncrypted = isEncrypted,
                        Size = size,
                        PackedSize = packedSize,
                        CreationTime = creationTime,
                        LastWriteTime = lastWriteTime,
                        LastAccessTime = lastAccessTime,
                        CRC = crc,
                        Attributes = attributes,
                        Comment = comment,
                        HostOS = hostOS,
                        Method = method,
                        IsSplitBefore = isSplitBefore,
                        IsSplitAfter = isSplitAfter
                    });
                }

                return this.entries;
            }
        }

        private T? GetPropertySafe<T>(uint fileIndex, ItemPropId name) {
            try {
                return this.GetProperty<T>(fileIndex, name);
            } catch (InvalidCastException) {
                return default(T);
            }
        }

        private T? GetProperty<T>(uint fileIndex, ItemPropId name) {
            LibAdapter.Variant propVariant = new LibAdapter.Variant();
            this.archive.GetProperty(fileIndex, name, ref propVariant);
            object? value = propVariant.Value;

            if (propVariant.ElementType == VariantElementType.Empty) {
                propVariant.Clear();
                return default(T);
            }

            propVariant.Clear();

            if (value == null) {
                return default(T);
            }

            Type type = typeof(T);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type underlyingType = isNullable ? Nullable.GetUnderlyingType(type)! : type;

            T? result = (T?)Convert.ChangeType(value.ToString(), underlyingType);

            return result;
        }

        ~ArchiveFile() {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing) {
            if (this.archive != null) {
                archive.Release();
            }

            if (this.archiveStream != null) {
                this.archiveStream.Dispose();
            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int GetProperty(ItemPropId propID, ref LibAdapter.Variant value) {
            if (propID == ItemPropId.kpidName && CurrentArchiveName != null) {
                value.Clear();
                value.Type = SharpGen.Runtime.Win32.VariantType.Default;
                value.ElementType = VariantElementType.BinaryString;
                value.Value = CurrentArchiveName;
                return HResults.S_OK;
            }
            return HResults.E_FAIL;
        }

        public int GetStream(string name, out IInStream? inStream) {
            inStream = null;
            return HResults.E_FAIL;
        }

        public uint AddRef() {
            if (refCount == 0) {
                sevenZipHandle.AddKeepAlive(this);
            }
            refCount++;
            return refCount;
        }
        public uint Release() {
            refCount--;
            if (refCount == 0) {
                sevenZipHandle.RemoveKeepAlive(this);
            }
            return refCount;
        }

        public int SetTotal(ulong? files, ulong? bytes) {
            return HResults.S_OK;
        }

        public int SetCompleted(ulong? files, ulong? bytes) {
            return HResults.S_OK;
        }
    }
}
