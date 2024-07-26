using System;
using System.IO;

namespace SkinnyJson
{
    /// <summary>
    /// Wrapper that tries to use either sync or async methods of a base stream.
    /// This prefers the synchronous methods, and falls back to a sync-runner-helper
    /// if not supported.
    /// </summary>
    internal class SyncStreamWrapper : Stream
    {
        private readonly Stream _src;
        public SyncStreamWrapper(Stream src) { _src = src; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return _src.Read(buffer, offset, count);
            }
            catch (Exception ex)
                when (ex is InvalidOperationException or NotSupportedException or NotImplementedException)
            {
                if (!_src.CanRead) throw;

                return Sync.Run(() => _src.ReadAsync(buffer, offset, count));
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                _src.Write(buffer, offset, count);
            }
            catch (Exception ex)
                when (ex is InvalidOperationException or NotSupportedException or NotImplementedException)
            {
                if (!_src.CanWrite) throw;

                Sync.Run(() => _src.WriteAsync(buffer, offset, count));
            }
        }

        #region direct pass-through

        public override void Flush()
        {
            _src.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) => _src.Seek(offset, origin);

        public override void SetLength(long value)
        {
            _src.SetLength(value);
        }

        public override bool CanRead => _src.CanRead;
        public override bool CanSeek => _src.CanSeek;
        public override bool CanWrite => _src.CanWrite;
        public override long Length => _src.Length;

        public override long Position
        {
            get => _src.Position;
            set => _src.Position = value;
        }

        #endregion direct pass-through
    }
}