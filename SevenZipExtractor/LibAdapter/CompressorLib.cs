using Microsoft.Win32.SafeHandles;
using SevenZipExtractor;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SevenZipExtractor.LibAdapter {
    internal struct DllCodecInfo {
        uint LibIndex;
        uint CodecIndex;
        bool? IsFilter;
        Guid? Encoder;
        Guid? Decoder;
    };
    


    internal class CompressorLib : LibBase {


        public CompressorLib(string path) : base(path) {

        }
    }
}
