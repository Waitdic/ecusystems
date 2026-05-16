using System;
using System.Runtime.InteropServices;

namespace Helper;

public static class ComApi
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetCommTimeouts(IntPtr hFile, [In, Out] ref COMMTIMEOUTS lpCommTimeouts);

    public struct COMMTIMEOUTS
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}
