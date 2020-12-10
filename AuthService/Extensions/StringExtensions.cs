using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System
{
    public static class Strings
    {

        public static byte[] ToByteArray(this string value) => Convert.FromBase64String(value);
    }
}
