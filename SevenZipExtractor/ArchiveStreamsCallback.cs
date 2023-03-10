using SevenZipExtractor.LibAdapter;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveStreamsCallback : ComRefCount, IArchiveExtractCallback, IArchiveExtractCallbackMessage {

        private readonly IList<Stream?> streams;
        private bool disposedValue;

        public ArchiveStreamsCallback(IList<Stream?> streams, WeakReference<SevenZipHandle> libHandle): base(libHandle) {
            this.streams = streams;
        }

        public int SetTotal(ulong total) {
            return HResults.S_OK;
        }

        public int SetCompleted(ulong? completeValue) {
            return HResults.S_OK;
        }

        public int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode) {
            if (askExtractMode != AskMode.kExtract) {
                outStream = null;
                return HResults.S_OK;
            }

            if (this.streams == null) {
                outStream = null;
                return HResults.S_OK;
            }

            Stream? stream = this.streams[(int)index];

            if (stream == null) {
                outStream = null;
                return HResults.S_OK;
            }

            outStream = new OutStreamWrapper(stream, LibHandle);
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
                    // streams are not disposed here
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}