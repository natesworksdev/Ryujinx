using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.Exceptions
{
    public class InvalidStructLayoutException : Exception 
    {
        public InvalidStructLayoutException(string message) : base(message) {}
        
        public InvalidStructLayoutException(Type structType, int expectedSize) : 
            base($"Type {structType.Name} has the wrong size. Expected: {expectedSize} bytes, Got: {Marshal.SizeOf(structType)} bytes") {}
    }
}