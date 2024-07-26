using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SkinnyJson.Unit.Tests
{
    internal sealed class NastyHttpStream : Stream
    {
        private readonly Stream _src;

        public NastyHttpStream(Stream src)
        {
            _src = src;
        }

        public override bool CanSeek => false;

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _src.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("NotAllowed");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("NotAllowed");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _src.WriteAsync(buffer, offset, count, cancellationToken);
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}