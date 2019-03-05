﻿using RGiesecke.DllExport;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QModManager.Installer.Extensions
{
    public static class Extensions
    {
        [DllExport]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool PathsEqual([MarshalAs(UnmanagedType.BStr)] string path1, [MarshalAs(UnmanagedType.BStr)] string path2)
        {
            string path1parsed = Path.GetFullPath(path1.Trim('/', '\\'));
            string path2parsed = Path.GetFullPath(path2.Trim('/', '\\'));

            return string.Equals(path1parsed, path2parsed, StringComparison.OrdinalIgnoreCase);
        }
    }
}
