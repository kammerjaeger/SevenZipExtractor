using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipExtractor.LibAdapter {
    internal sealed class HResults {
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;

        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_NOINTERFACE = unchecked((int)0x80004002);
        internal const int E_POINTER = unchecked((int)0x80004003);
        internal const int E_ABORT = unchecked((int)0x80004004);
        internal const int E_FAIL = unchecked((int)0x80004005);

        internal const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

        internal const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);
        internal const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);

        internal const int E_FILENOTFOUND = unchecked((int)0x80070002);
        internal const int E_PATHNOTFOUND = unchecked((int)0x80070003);
        internal const int E_ACCESSDENIED = unchecked((int)0x80070005);
        internal const int E_HANDLE = unchecked((int)0x80070006);
        internal const int E_INVALID_DATA = unchecked((int)0x8007000D);
        internal const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int E_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);

        internal const int WSAECONNABORTED = unchecked((int)0x80072745);
        internal const int WSAECONNRESET = unchecked((int)0x80072746);
        internal const int ERROR_TOO_MANY_CMDS = unchecked((int)0x80070038);
        internal const int ERROR_NOT_SUPPORTED = unchecked((int)0x80070032);

        internal const int ERROR_TOO_MANY_POSTS = unchecked((int)0x8007012A);
        internal const int ERROR_INVALID_REPARSE_DATA = unchecked((int)0x80071128);
        internal const int ERROR_REPARSE_TAG_INVALID = unchecked((int)0x80071129);
        private HResults() { }
    }
}
