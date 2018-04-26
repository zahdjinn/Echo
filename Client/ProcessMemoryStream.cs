﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Echo
{
    internal sealed class ProcessMemoryStream : Stream, IDisposable
    {
        private ProcessAccessFlags Access;
        private bool Disposed;
        internal IntPtr AccessHandle;

        public override long Position { get; set; }
        public int ProcessId { get; set; }

        public override bool CanRead => (Access & ProcessAccessFlags.VmRead) > ProcessAccessFlags.None;
        public override bool CanSeek => true;
        public override bool CanWrite => (Access & (ProcessAccessFlags.VmOperation | ProcessAccessFlags.VmWrite)) > ProcessAccessFlags.None;
        public override long Length => throw new NotSupportedException("Length is not supported.");
        public override void Flush() => throw new NotSupportedException("Flush is not supported.");
        public override void SetLength(long value) => throw new NotSupportedException("Cannot set the length for this stream.");

        internal ProcessMemoryStream(int processId, ProcessAccessFlags access)
        {
            Access = access;
            ProcessId = processId;
            AccessHandle = NativeMethods.OpenProcess(access, false, processId);

            if (AccessHandle == IntPtr.Zero)
                throw new ArgumentException("Unable to open the process.");
        }

        ~ProcessMemoryStream()
        {
            Dispose(false);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Disposed)
                throw new ObjectDisposedException("ProcessMemoryStream");

            if (AccessHandle == IntPtr.Zero)
                throw new InvalidOperationException("Process is not open.");

            IntPtr num = Marshal.AllocHGlobal(count);
            if (num == IntPtr.Zero)
                throw new InvalidOperationException("Unable to allocate memory.");


            NativeMethods.ReadProcessMemory(AccessHandle, (IntPtr)Position, num, (IntPtr)count, out int bytesRead);
            Position += bytesRead;
            Marshal.Copy(num, buffer, offset, count);
            Marshal.FreeHGlobal(num);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (Disposed)
                throw new ObjectDisposedException("ProcessMemoryStream");

            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    throw new NotSupportedException("SeekOrigin.End not supported.");
            }

            return Position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Disposed)
                throw new ObjectDisposedException("ProcessMemoryStream");

            if (AccessHandle == IntPtr.Zero)
                throw new InvalidOperationException("Process is not open.");

            IntPtr num = Marshal.AllocHGlobal(count);
            if (num == IntPtr.Zero)
                throw new InvalidOperationException("Unable to allocate memory.");

            Marshal.Copy(buffer, offset, num, count);

            NativeMethods.WriteProcessMemory(AccessHandle, (IntPtr)Position, num, (IntPtr)count, out int bytesWritten);
            Position += bytesWritten;

            Marshal.FreeHGlobal(num);
        }

        public override void WriteByte(byte value) => Write(new byte[1] { value }, 0, 1);

        public void WriteString(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            Write(bytes, 0, bytes.Length);
        }

        public override void Close()
        {
            if (Disposed)
                return;
            if (AccessHandle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(AccessHandle);
                AccessHandle = IntPtr.Zero;
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (AccessHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(AccessHandle);
                    AccessHandle = IntPtr.Zero;
                }
                base.Dispose(disposing);
            }
            Disposed = true;
        }
    }
}
