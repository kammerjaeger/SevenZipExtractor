
using System;
using System.IO;
using System.Reflection.Metadata;
using SevenZipExtractor;
using SevenZipExtractor.LibAdapter;

namespace ConsoleApplication86
{
    class Program
    {
        static void Main(string[] args)
        {
            using (SevenZipHandle handle = SevenZipHandle.InitializeAndValidateLibrary()) {
                using (ArchiveFile archiveFile = new ArchiveFile(handle, @"Archive.arj")) {
                    // extract all
                    archiveFile.Extract("Output");
                }

                using (ArchiveFile archiveFile = new ArchiveFile(handle, "archive.arj")) {
                    foreach (Entry entry in archiveFile.Entries) {
                        Console.WriteLine(entry.FileName);

                        // extract to file
                        entry.Extract(entry.FileName ?? "NoFileName");

                        // extract to stream
                        MemoryStream memoryStream = new MemoryStream();
                        entry.Extract(memoryStream);
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
