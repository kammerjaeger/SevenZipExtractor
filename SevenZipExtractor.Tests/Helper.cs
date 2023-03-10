using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenZipExtractor.Tests {
    /// <summary>
    /// Helper function of the old format detection
    /// </summary>
    internal static class Helper {
        public static bool GuessFormatFromExtension(string fileExtension, out SevenZipFormat format) {
            if (string.IsNullOrWhiteSpace(fileExtension)) {
                format = SevenZipFormat.Undefined;
                return false;
            }

            fileExtension = fileExtension.TrimStart('.').Trim().ToLowerInvariant();

            if (fileExtension.Equals("rar")) {
                // 7z has different GUID for Pre-RAR5 and RAR5, but they have both same extension (.rar)
                // If it is [0x52 0x61 0x72 0x21 0x1A 0x07 0x01 0x00] then file is RAR5 otherwise RAR.
                // https://www.rarlab.com/technote.htm

                // We are unable to guess right format just by looking at extension and have to check signature

                format = SevenZipFormat.Undefined;
                return false;
            }

            if (!FormatsHelper.ExtensionFormatMapping.ContainsKey(fileExtension)) {
                format = SevenZipFormat.Undefined;
                return false;
            }

            format = FormatsHelper.ExtensionFormatMapping[fileExtension];
            return true;
        }


        public static bool GuessFormatFromSignature(string filePath, out SevenZipFormat format) {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                return GuessFormatFromSignature(fileStream, out format);
            }
        }

        public static bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format) {
            int longestSignature = FormatsHelper.FileSignatures.Values.OrderByDescending(v => v.Length).First().Length;

            byte[] archiveFileSignature = new byte[longestSignature];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignature);

            stream.Position -= bytesRead; // go back o beginning

            if (bytesRead != longestSignature) {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, byte[]> pair in FormatsHelper.FileSignatures) {
                if (archiveFileSignature.Take(pair.Value.Length).SequenceEqual(pair.Value)) {
                    format = pair.Key;
                    return true;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }
    }

    public class FormatsHelper {
        internal static readonly Dictionary<string, SevenZipFormat> ExtensionFormatMapping = new Dictionary<string, SevenZipFormat>
        {
            {"7z", SevenZipFormat.SevenZip},
            {"gz", SevenZipFormat.GZip},
            {"tar", SevenZipFormat.Tar},
            {"rar", SevenZipFormat.Rar},
            {"zip", SevenZipFormat.Zip},
            {"lzma", SevenZipFormat.Lzma},
            {"lzh", SevenZipFormat.Lzh},
            {"arj", SevenZipFormat.Arj},
            {"bz2", SevenZipFormat.BZip2},
            {"cab", SevenZipFormat.Cab},
            {"chm", SevenZipFormat.Chm},
            {"deb", SevenZipFormat.Deb},
            {"iso", SevenZipFormat.Iso},
            {"rpm", SevenZipFormat.Rpm},
            {"wim", SevenZipFormat.Wim},
            {"udf", SevenZipFormat.Udf},
            {"mub", SevenZipFormat.Mub},
            {"xar", SevenZipFormat.Xar},
            {"hfs", SevenZipFormat.Hfs},
            {"dmg", SevenZipFormat.Dmg},
            {"z", SevenZipFormat.Lzw},
            {"xz", SevenZipFormat.XZ},
            {"flv", SevenZipFormat.Flv},
            {"swf", SevenZipFormat.Swf},
            {"exe", SevenZipFormat.PE},
            {"dll", SevenZipFormat.PE},
            {"vhd", SevenZipFormat.Vhd}
        };

        internal static Dictionary<SevenZipFormat, byte[]> FileSignatures = new Dictionary<SevenZipFormat, byte[]>
        {
            {SevenZipFormat.Rar5, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00}},
            {SevenZipFormat.Rar, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }},
            {SevenZipFormat.Vhd, new byte[] { 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78 }},
            {SevenZipFormat.Deb, new byte[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E }},
            {SevenZipFormat.Dmg, new byte[] { 0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60 }},
            {SevenZipFormat.SevenZip, new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }},
            {SevenZipFormat.Tar, new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 }},
            {SevenZipFormat.Iso, new byte[] { 0x43, 0x44, 0x30, 0x30, 0x31 }},
            {SevenZipFormat.Cab, new byte[] { 0x4D, 0x53, 0x43, 0x46 }},
            {SevenZipFormat.Rpm, new byte[] { 0xed, 0xab, 0xee, 0xdb }},
            {SevenZipFormat.Xar, new byte[] { 0x78, 0x61, 0x72, 0x21 }},
            {SevenZipFormat.Chm, new byte[] { 0x49, 0x54, 0x53, 0x46 }},
            {SevenZipFormat.BZip2, new byte[] { 0x42, 0x5A, 0x68 }},
            {SevenZipFormat.Flv, new byte[] { 0x46, 0x4C, 0x56 }},
            {SevenZipFormat.Swf, new byte[] { 0x46, 0x57, 0x53 }},
            {SevenZipFormat.GZip, new byte[] { 0x1f, 0x0b }},
            {SevenZipFormat.Zip, new byte[] { 0x50, 0x4b }},
            {SevenZipFormat.Arj, new byte[] { 0x60, 0xEA }},
            {SevenZipFormat.Lzh, new byte[] { 0x2D, 0x6C, 0x68 }},
            {SevenZipFormat.SquashFS, new byte[] {0x68, 0x73, 0x71, 0x73}},
            {SevenZipFormat.Mslz, new byte[] { 0x53, 0x5a, 0x44, 0x44 } },
        };
    }
}
