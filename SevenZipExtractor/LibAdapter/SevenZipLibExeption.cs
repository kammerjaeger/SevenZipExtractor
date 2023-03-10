using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipExtractor.LibAdapter {
    public class SevenZipLibExeption : SevenZipException {
        public SevenZipLibExeption(): base() {
        }

        public SevenZipLibExeption(string message) : base(message) {
        }
        public SevenZipLibExeption(string message, int hResult) : base(message) {
            HResult = hResult;
        }

        public SevenZipLibExeption(string message, Exception innerException) : base(message, innerException) {
        }

        protected SevenZipLibExeption(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
