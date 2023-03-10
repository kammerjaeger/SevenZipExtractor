﻿using System;
using System.Collections.Generic;

namespace SevenZipExtractor
{
    public class Formats
    {
        internal static Dictionary<SevenZipFormat, Guid> FormatGuidMapping = new Dictionary<SevenZipFormat, Guid>
        {
            {SevenZipFormat.SevenZip, new Guid("23170f69-40c1-278a-1000-000110070000")},
            {SevenZipFormat.Arj, new Guid("23170f69-40c1-278a-1000-000110040000")},
            {SevenZipFormat.BZip2, new Guid("23170f69-40c1-278a-1000-000110020000")},
            {SevenZipFormat.Cab, new Guid("23170f69-40c1-278a-1000-000110080000")},
            {SevenZipFormat.Chm, new Guid("23170f69-40c1-278a-1000-000110e90000")},
            {SevenZipFormat.Compound, new Guid("23170f69-40c1-278a-1000-000110e50000")},
            {SevenZipFormat.Cpio, new Guid("23170f69-40c1-278a-1000-000110ed0000")},
            {SevenZipFormat.Deb, new Guid("23170f69-40c1-278a-1000-000110ec0000")},
            {SevenZipFormat.GZip, new Guid("23170f69-40c1-278a-1000-000110ef0000")},
            {SevenZipFormat.Iso, new Guid("23170f69-40c1-278a-1000-000110e70000")},
            {SevenZipFormat.Lzh, new Guid("23170f69-40c1-278a-1000-000110060000")},
            {SevenZipFormat.Lzma, new Guid("23170f69-40c1-278a-1000-0001100a0000")},
            {SevenZipFormat.Nsis, new Guid("23170f69-40c1-278a-1000-000110090000")},
            {SevenZipFormat.Rar, new Guid("23170f69-40c1-278a-1000-000110030000")},
            {SevenZipFormat.Rar5, new Guid("23170f69-40c1-278a-1000-000110CC0000")},
            {SevenZipFormat.Rpm, new Guid("23170f69-40c1-278a-1000-000110eb0000")},
            {SevenZipFormat.Split, new Guid("23170f69-40c1-278a-1000-000110ea0000")},
            {SevenZipFormat.Tar, new Guid("23170f69-40c1-278a-1000-000110ee0000")},
            {SevenZipFormat.Wim, new Guid("23170f69-40c1-278a-1000-000110e60000")},
            {SevenZipFormat.Lzw, new Guid("23170f69-40c1-278a-1000-000110050000")},
            {SevenZipFormat.Zip, new Guid("23170f69-40c1-278a-1000-000110010000")},
            {SevenZipFormat.Udf, new Guid("23170f69-40c1-278a-1000-000110E00000")},
            {SevenZipFormat.Xar, new Guid("23170f69-40c1-278a-1000-000110E10000")},
            {SevenZipFormat.Mub, new Guid("23170f69-40c1-278a-1000-000110E20000")},
            {SevenZipFormat.Hfs, new Guid("23170f69-40c1-278a-1000-000110E30000")},
            {SevenZipFormat.Dmg, new Guid("23170f69-40c1-278a-1000-000110E40000")},
            {SevenZipFormat.XZ, new Guid("23170f69-40c1-278a-1000-0001100C0000")},
            {SevenZipFormat.Mslz, new Guid("23170f69-40c1-278a-1000-000110D50000")},
            {SevenZipFormat.PE, new Guid("23170f69-40c1-278a-1000-000110DD0000")},
            {SevenZipFormat.Elf, new Guid("23170f69-40c1-278a-1000-000110DE0000")},
            {SevenZipFormat.Swf, new Guid("23170f69-40c1-278a-1000-000110D70000")},
            {SevenZipFormat.Vhd, new Guid("23170f69-40c1-278a-1000-000110DC0000")},
            {SevenZipFormat.Flv, new Guid("23170f69-40c1-278a-1000-000110D60000")},
            {SevenZipFormat.SquashFS, new Guid("23170f69-40c1-278a-1000-000110D20000")},
            {SevenZipFormat.Lzma86, new Guid("23170f69-40c1-278a-1000-0001100B0000")},
            {SevenZipFormat.Ppmd, new Guid("23170f69-40c1-278a-1000-0001100D0000")},
            {SevenZipFormat.TE, new Guid("23170f69-40c1-278a-1000-000110CF0000")},
            {SevenZipFormat.UEFIc, new Guid("23170f69-40c1-278a-1000-000110D00000")},
            {SevenZipFormat.UEFIs, new Guid("23170f69-40c1-278a-1000-000110D10000")},
            {SevenZipFormat.CramFS, new Guid("23170f69-40c1-278a-1000-000110D30000")},
            {SevenZipFormat.APM, new Guid("23170f69-40c1-278a-1000-000110D40000")},
            {SevenZipFormat.Swfc, new Guid("23170f69-40c1-278a-1000-000110D80000")},
            {SevenZipFormat.Ntfs, new Guid("23170f69-40c1-278a-1000-000110D90000")},
            {SevenZipFormat.Fat, new Guid("23170f69-40c1-278a-1000-000110DA0000")},
            {SevenZipFormat.Mbr, new Guid("23170f69-40c1-278a-1000-000110DB0000")},
            {SevenZipFormat.MachO, new Guid("23170f69-40c1-278a-1000-000110DF0000")}
        };
    }
}
