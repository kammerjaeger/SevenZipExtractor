using Microsoft.Extensions.Logging;
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
        private readonly ILogger logger;

        public ArchiveStreamsCallback(IList<Stream?> streams, WeakReference<SevenZipHandle> libHandle, ILogger logger): base(libHandle) {
            this.streams = streams;
            this.logger = logger;
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

            outStream = new OutStreamWrapper(stream, false, LibHandle);
            outStream.AddRef();
            return HResults.S_OK;
        }

        public int PrepareOperation(AskMode askExtractMode) {
            return HResults.S_OK;
        }

        public int SetOperationResult(OperationResult opRes) {
            if (opRes != OperationResult.kOK) {
                logger.LogWarning("Error {opRes} while setting result items", opRes);
            }
            return HResults.S_OK;
        }
        public int ReportExtractResult(NEventIndexType indexType, uint index, OperationResult opRes) {
            if (opRes != OperationResult.kOK) {
                logger.LogWarning("Error {opRes} extracting item {index} error {indexType}", opRes, index, indexType);
            }
            return HResults.S_OK;
        }
        protected override void Dispose(bool disposing) {
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