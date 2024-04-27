// Version 1.5

using SevenZipExtractor.LibAdapter;
using SharpGen.Runtime;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    public class StreamWrapper : ComRefCount {

        protected Stream BaseStream;
        private bool disposedValue = false;
        private bool closeInnerStream;

        protected StreamWrapper(Stream baseStream, bool closeInnerStream, WeakReference<SevenZipHandle> libHandle): base(libHandle)
        {
            BaseStream = baseStream;
            this.closeInnerStream = closeInnerStream;
        }

        public virtual int Seek(long offset, uint seekOrigin, ref ulong newPosition)
        {
            long Position = BaseStream.Seek(offset, (SeekOrigin)seekOrigin);

            if (!Unsafe.IsNullRef(ref newPosition))
            {
                newPosition = (ulong)Position;
            }
            return HResults.S_OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // cleanup anything normal
                    if (closeInnerStream)
                    {
                        BaseStream.Close();
                    }
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }

    internal class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
    {

        public InStreamWrapper(Stream baseStream, bool closeInnerStream, WeakReference<SevenZipHandle> libHandle) : base(baseStream, closeInnerStream, libHandle)
        {
        }

        public uint Read(byte[] data, uint size)
        {
            return (uint)BaseStream.Read(data, 0, (int)size);
        }

        public int Read(IntPtr data, uint size, ref uint rocessedSizeRef)
        {
            if (size == 0 && !Unsafe.IsNullRef(ref rocessedSizeRef))
            {
                rocessedSizeRef = 0;
                return HResults.S_OK;
            }
            byte[] managedArray = new byte[size];
            var read = Read(managedArray, size);
            Marshal.Copy(managedArray, 0, data, (int)read);

            if (!Unsafe.IsNullRef(ref rocessedSizeRef))
            {
                rocessedSizeRef = read;
            }
            return HResults.S_OK;
        }
    }

    internal class OutStreamWrapper : StreamWrapper, ISequentialOutStream, IOutStream
    {
        public OutStreamWrapper(Stream baseStream, bool closeInnerStream, WeakReference<SevenZipHandle> libHandle) : base(baseStream, closeInnerStream, libHandle)
        {
        }

        public int SetSize(ulong newSize)
        {
            BaseStream.SetLength((long)newSize);
            return HResults.S_OK;
        }

        public int Write(byte[] data, uint size, ref uint processedSize)
        {
            BaseStream.Write(data, 0, (int)size);
            if (!Unsafe.IsNullRef(ref processedSize))
            {
                processedSize = size;
            }
            return HResults.S_OK;
        }

        public int Write(IntPtr data, uint size, ref uint rocessedSizeRef)
        {
            byte[] managedArray = new byte[size];
            Marshal.Copy(data, managedArray, 0, (int)size);
            return Write(managedArray, size, ref rocessedSizeRef);
        }
    }
}