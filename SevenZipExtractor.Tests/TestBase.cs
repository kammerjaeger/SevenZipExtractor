﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SevenZipExtractor.LibAdapter;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenZipExtractor.Tests {
    public abstract class TestBase {
        protected IList<TestFileEntry> TestEntriesWithFolder = new List<TestFileEntry>()
        {
                new TestFileEntry { Name = "image1.jpg", IsFolder = false, MD5 = "b3144b66569ab0052b4019a2b4c07a31"},
                new TestFileEntry { Name = "image2.jpg", IsFolder = false, MD5 = "8fdd4013edcf04b335ac3a9ce0c13887"},
                new TestFileEntry { Name = "testFolder", IsFolder = true},
                new TestFileEntry { Name = $"testFolder{Path.DirectorySeparatorChar}image3.jpg", IsFolder = false, MD5 = "24ffd227340432596fe61ef6300098ad"},
        };

        protected IList<TestFileEntry> TestEntriesWithoutFolder = new List<TestFileEntry>()
        {
                new TestFileEntry { Name = "image1.jpg", IsFolder = false, MD5 = "b3144b66569ab0052b4019a2b4c07a31"},
                new TestFileEntry { Name = "image2.jpg", IsFolder = false, MD5 = "8fdd4013edcf04b335ac3a9ce0c13887"},
                new TestFileEntry { Name = $"testFolder{Path.DirectorySeparatorChar}image3.jpg", IsFolder = false, MD5 = "24ffd227340432596fe61ef6300098ad"},
        };

        protected IList<TestFileEntry> TestSingleFile = new List<TestFileEntry>()
{
                new TestFileEntry { Name = "image1.jpg", IsFolder = false, MD5 = "b3144b66569ab0052b4019a2b4c07a31"},
        };

        /// <summary>
        /// Test new format guessing against old method
        /// </summary>
        /// <param name="archiveBytes"></param>
        /// <param name="fileName"></param>
        protected void TestFormatDetection(byte[] archiveBytes, string? fileName = null) {

            MemoryStream memoryStream = new MemoryStream(archiveBytes);

            using (SevenZipHandle handl = SevenZipHandle.InitializeAndValidateLibrary()) {
                handl.Libs.FindFormatForDataAndExt(out var possFromat, out var liklyFormat, memoryStream, fileName);
                var found = Helper.GuessFormatFromSignature(memoryStream, out var format1);
                var guui = Formats.FormatGuidMapping[format1];
                Assert.IsTrue(possFromat.Any(x => x.ClassID.Equals(guui)));
                Assert.IsFalse(liklyFormat.Any(x => x.ClassID.Equals(guui)));
            }
        }

        protected void TestExtractToStream(byte[] archiveBytes, IList<TestFileEntry> expected, SevenZipFormat? sevenZipFormat = null, string? fileName = null) {
            MemoryStream memoryStream = new MemoryStream(archiveBytes);

            using (SevenZipHandle handl = SevenZipHandle.InitializeAndValidateLibrary()) {
                using (ArchiveFile archiveFile = new ArchiveFile(handl, memoryStream, sevenZipFormat, fileName)) {
                    foreach (TestFileEntry testEntry in expected) {
                        Entry? entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);

                        Assert.IsNotNull(entry, "Entry not found: " + testEntry.Name);

                        if (testEntry.IsFolder) {
                            continue;
                        }

                        using (MemoryStream entryMemoryStream = new MemoryStream()) {
                            entry!.Extract(entryMemoryStream);

                            if (testEntry.MD5 != null) {
                                Assert.AreEqual(testEntry.MD5, entryMemoryStream.ToArray().MD5String(), "MD5 does not match: " + entry.FileName);
                            }

                            if (testEntry.CRC32 != null) {
                                Assert.AreEqual(testEntry.CRC32, entryMemoryStream.ToArray().CRC32String(), "CRC32 does not match: " + entry.FileName);
                            }
                        }
                    }
                }
            }

        }
    }
}