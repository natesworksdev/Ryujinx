using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Exceptions
{
    public class InvalidStructLayoutException : Exception 
    {
        public InvalidStructLayoutException(string message) : base(message) {}
        
        public InvalidStructLayoutException(Type structType, int expectedSize) : 
            base($"Type {structType.Name} is the wrong size! Expected:{expectedSize}Bytes Got:{Marshal.SizeOf(structType)}Bytes") {}
    }
}