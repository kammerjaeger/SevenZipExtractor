using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenZipExtractor.LibAdapter {
    public class ComRefCount : IUnknown {
        public ShadowContainer? Shadow { get; set; }
        protected WeakReference<SevenZipHandle> LibHandle { get; init; }

        private bool disposedValue = false;
        private uint refCount = 0;

        /// <summary>
        /// If true, object will be disposed when ref count reaches 0.
        /// Set it to true if you want to handle the object directly
        /// </summary>
        public bool DisposeOnRelease { get; set; } = true;

        public ComRefCount(WeakReference<SevenZipHandle> libHandle) {
            this.LibHandle = libHandle;

        }
        
        public uint AddRef() {
            var newVal = Interlocked.Increment(ref refCount);
            if (newVal == 1) {
                if (LibHandle.TryGetTarget(out var handle)) {
                    handle.AddKeepAlive(this);
                }
            }
            return newVal;
        }
        public uint Release() {
            var newVal = Interlocked.Decrement(ref refCount);
            if (newVal == 0) {
                if (LibHandle.TryGetTarget(out var handle)) {
                    handle.RemoveKeepAlive(this);
                }
                if (DisposeOnRelease) {
                    Dispose(true);
                }
            }
            return newVal;
        }
        ~ComRefCount() {
            Dispose(disposing: false);
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // cleanup anything normal
                }

                Shadow?.Dispose();
                disposedValue = true;
            }
        }
    }
}
