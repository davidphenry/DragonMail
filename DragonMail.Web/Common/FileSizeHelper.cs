using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DragonMail.Web
{
    public static class FileSizeHelper
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

       
        public static string GetReadableFileSize(this int size) // Size in bytes
        {
            int unitIndex = 0;
            while (size >= 1024)
            {
                size /= 1024;
                ++unitIndex;
            }

            string unit = Units[unitIndex];
            return string.Format("{0:0.#} {1}", size, unit);
        }
    }
}