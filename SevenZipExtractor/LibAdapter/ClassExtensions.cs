using SharpGen.Runtime;
using System;
using System.Runtime.InteropServices;

namespace SevenZipExtractor.LibAdapter {

    [Guid("23170F69-40C1-278A-0000-000600600000")]
    public partial class IInArchive : ComObject {
    }
    [Guid("23170F69-40C1-278A-0000-000600610000")]
    public partial class IArchiveOpenSeq : ComObject {
    }

    [Guid("23170F69-40C1-278A-0000-000000050000")]
    public partial interface IProgress {
    }

    [Guid("23170F69-40C1-278A-0000-000600100000")]
    public partial interface IArchiveOpenCallback {
    }

    [Guid("23170F69-40C1-278A-0000-000600300000")]
    public partial interface IArchiveOpenVolumeCallback {
    }

    [Guid("23170F69-40C1-278A-0000-000500100000")]
    public partial interface ICryptoGetTextPassword {
    }

    [Guid("23170F69-40C1-278A-0000-000600400000")]
    public partial interface IInArchiveGetStream {
    }

    [Guid("23170F69-40C1-278A-0000-000300010000")]
    public partial interface ISequentialInStream {
    }

    [Guid("23170F69-40C1-278A-0000-000300020000")]
    public partial interface ISequentialOutStream {
    }

    [Guid("23170F69-40C1-278A-0000-000300030000")]
    public partial interface IInStream {
    }

    [Guid("23170F69-40C1-278A-0000-000300040000")]
    public partial interface IOutStream {
    }

    [Guid("23170F69-40C1-278A-0000-000600200000")]
    public partial interface IArchiveExtractCallback {

    }

    [Guid("23170F69-40C1-278A-0000-000600210000")]
    public partial interface IArchiveExtractCallbackMessage {

    }

    [Guid("23170F69-40C1-278A-0000-000400600000")]
    public partial interface ICompressCodecsInfo {

    }

    [Guid("23170F69-40C1-278A-0000-000400C10000")]
    public partial interface IHashers {

    }

    [Guid("23170F69-40C1-278A-0000-000400C00000")]
    public partial class IHasher {

    }

    /// <summary>
    /// Archive password ask modes
    /// </summary>
    public enum AskMode : int {
        kExtract = 0,
        kTest,
        kSkip,
        kReadExternal
    }

    /// <summary>
    /// Archive operation results
    /// </summary>
    public enum OperationResult : int {
        kOK = 0,
        kUnSupportedMethod,
        kDataError,
        kCRCError,
        kUnavailable,
        kUnexpectedEnd,
        kDataAfterEnd,
        kIsNotArc,
        kHeadersError,
        kWrongPassword
    }

    /// <summary>
    /// Item properties
    /// </summary>
    public enum ItemPropId : uint {
        kpidNoProperty = 0,
        kpidMainSubfile,
        kpidHandlerItemIndex = 2,
        kpidPath,
        kpidName,
        kpidExtension,
        kpidIsFolder,
        kpidSize,
        kpidPackedSize,
        kpidAttributes,
        kpidCreationTime,
        kpidLastAccessTime,
        kpidLastWriteTime,
        kpidSolid,
        kpidCommented,
        kpidEncrypted,
        kpidSplitBefore,
        kpidSplitAfter,
        kpidDictionarySize,
        kpidCRC,
        kpidType,
        kpidIsAnti,
        kpidMethod,
        kpidHostOS,
        kpidFileSystem,
        kpidUser,
        kpidGroup,
        kpidBlock,
        kpidComment,
        kpidPosition,
        kpidPrefix,

        kpidNumSubDirs,
        kpidNumSubFiles,
        kpidUnpackVer,
        kpidVolume,
        kpidIsVolume,
        kpidOffset,
        kpidLinks,
        kpidNumBlocks,
        kpidNumVolumes,
        kpidTimeType,
        kpidBit64,
        kpidBigEndian,
        kpidCpu,
        kpidPhySize,
        kpidHeadersSize,
        kpidChecksum,
        kpidCharacts,
        kpidVa,
        kpidId,
        kpidShortName,
        kpidCreatorApp,
        kpidSectorSize,
        kpidPosixAttrib,
        kpidSymLink,
        kpidError,
        kpidTotalSize,
        kpidFreeSpace,
        kpidClusterSize,
        kpidVolumeName,
        kpidLocalName,
        kpidProvider,
        kpidNtSecure,
        kpidIsAltStream,
        kpidIsAux,
        kpidIsDeleted,
        kpidIsTree,
        kpidSha1,
        kpidSha256,
        kpidErrorType,
        kpidNumErrors,
        kpidErrorFlags,
        kpidWarningFlags,
        kpidWarning,
        kpidNumStreams,
        kpidNumAltStreams,
        kpidAltStreamsSize,
        kpidVirtualSize,
        kpidUnpackSize,
        kpidTotalPhySize,
        kpidVolumeIndex,
        kpidSubType,
        kpidShortComment,
        kpidCodePage,
        kpidIsNotArcType,
        kpidPhySizeCantBeDetected,
        kpidZerosTailIsAllowed,
        kpidTailSize,
        kpidEmbeddedStubSize,
        kpidNtReparse,
        kpidHardLink,
        kpidINode,
        kpidStreamId,
        kpidReadOnly,
        kpidOutName,
        kpidCopyLink,
        kpidArcFileName,
        kpidIsHash,
        kpidChangeTime,
        kpidUserId,
        kpidGroupId,
        kpidDeviceMajor,
        kpidDeviceMinor,

        //kpidTotalSize = 0x1100,
        //kpidFreeSpace,
        //kpidClusterSize,
        //kpidVolumeName,

        //kpidLocalName = 0x1200,
        //kpidProvider,

        kpidUserDefined = 0x10000
    }

    /// <summary>
    /// Handler properties
    /// </summary>
    public enum NHandlerPropID : uint {
        kName = 0,        // VT_BSTR
        kClassID,         // binary GUID in VT_BSTR
        kExtension,       // VT_BSTR
        kAddExtension,    // VT_BSTR
        kUpdate,          // VT_BOOL
        kKeepName,        // VT_BOOL
        kSignature,       // binary in VT_BSTR
        kMultiSignature,  // binary in VT_BSTR
        kSignatureOffset, // VT_UI4
        kAltStreams,      // VT_BOOL
        kNtSecure,        // VT_BOOL
        kFlags,           // VT_UI4
        kTimeFlags,        // VT_UI4
    }

    /// <summary>
    /// Event types
    /// </summary>
    public enum NEventIndexType : uint {
        kNoIndex = 0,
        kInArcIndex,
        kBlockIndex,
        kOutArcIndex,
    }

    /// <summary>
    /// Method properties
    /// </summary>
    public enum NMethodPropID : uint {
        kID,
        kName,
        kDecoder,
        kEncoder,
        kPackStreams,
        kUnpackStreams,
        kDescription,
        kDecoderIsAssigned,
        kEncoderIsAssigned,
        kDigestSize,
        kIsFilter,
    }

    /// <summary>
    /// Archive handler info flags
    /// </summary>
    [Flags]
    public enum NArcInfoFlags : uint {
        None             = 0,
        kKeepName        = 1 << 0,  // keep name of file in archive name
        kAltStreams      = 1 << 1,  // the handler supports alt streams
        kNtSecure        = 1 << 2,  // the handler supports NT security
        kFindSignature   = 1 << 3,  // the handler can find start of archive
        kMultiSignature  = 1 << 4,  // there are several signatures
        kUseGlobalOffset = 1 << 5,  // the seek position of stream must be set as global offset
        kStartOpen       = 1 << 6,  // call handler for each start position
        kPureStartOpen   = 1 << 7,  // call handler only for start of file
        kBackwardOpen    = 1 << 8,  // archive can be open backward
        kPreArc          = 1 << 9,  // such archive can be stored before real archive (like SFX stub)
        kSymLinks        = 1 << 10, // the handler supports symbolic links
        kHardLinks       = 1 << 11, // the handler supports hard links
        kByExtOnlyOpen   = 1 << 12, // call handler only if file extension matches
        kHashHandler     = 1 << 13, // the handler contains the hashes (checksums)
        kCTime           = 1 << 14,
        kCTime_Default   = 1 << 15,
        kATime           = 1 << 16,
        kATime_Default   = 1 << 17,
        kMTime           = 1 << 18,
        kMTime_Default   = 1 << 19,
    }

    /// <summary>
    /// Archive test function results
    /// </summary>
    public enum IsArcResult : uint {
        k_IsArc_Res_NO = 0,
        k_IsArc_Res_YES = 1,
        k_IsArc_Res_NEED_MORE = 2,
        //k_IsArc_Res_YES_LOW_PROB = 3,
    }
}
