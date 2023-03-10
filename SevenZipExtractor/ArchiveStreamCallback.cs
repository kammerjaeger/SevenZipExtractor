using SevenZipExtractor.LibAdapter;
using SharpGen.Runtime;
using System;
using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveStreamCallback : ComRefCount, IArchiveExtractCallback, IArchiveExtractCallbackMessage {
        private bool disposedValue;

        private readonly uint fileNumber;
        private readonly Stream stream;

        public ArchiveStreamCallback(uint fileNumber, Stream stream, WeakReference<SevenZipHandle> libHandle) : base(libHandle) {
            this.fileNumber = fileNumber;
            this.stream = stream;
        }

        public int SetTotal(ulong total) {
            return HResults.S_OK;
        }

        public int SetCompleted(ulong? completeValue) {
            return HResults.S_OK;
        }

        public int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode) {
            if ((index != this.fileNumber) || (askExtractMode != AskMode.kExtract)) {
                outStream = null;
                return HResults.S_OK;
            }

            outStream = new OutStreamWrapper(this.stream, LibHandle);
            outStream.AddRef();

            return HResults.S_OK;
        }

        public int PrepareOperation(AskMode askExtractMode) {
            return HResults.S_OK;
        }

        public int SetOperationResult(OperationResult resultEOperationResult) {
            if (resultEOperationResult != OperationResult.kOK) {
                Console.WriteLine($"Error ${resultEOperationResult} extracting items");
            }
            return HResults.S_OK;
        }
        public int ReportExtractResult(NEventIndexType indexType, uint index, OperationResult opRes) {
            if (opRes != OperationResult.kOK) {
                Console.WriteLine($"Error ${opRes} extracting item ${index} error ${indexType}");
            }
            return HResults.S_OK;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // stream has to be disposed by creator of the class
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}