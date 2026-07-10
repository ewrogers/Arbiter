namespace Arbiter.IO.Archives;

internal sealed class ArchiveEntryStream : Stream
{
    private readonly FileStream _stream;
    private readonly long _start;
    private readonly long _length;
    private long _position;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public ArchiveEntryStream(string filePath, long offset, long length)
    {
        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _start = offset;
        _length = length;
        _stream.Position = _start;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset > buffer.Length - count)
        {
            throw new ArgumentException("The buffer range is invalid.");
        }

        var bytesToRead = (int)Math.Min(count, _length - _position);
        if (bytesToRead <= 0)
        {
            return 0;
        }

        var bytesRead = _stream.Read(buffer, offset, bytesToRead);
        _position += bytesRead;
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesToRead = (int)Math.Min(buffer.Length, _length - _position);
        if (bytesToRead <= 0)
        {
            return 0;
        }

        var bytesRead = _stream.Read(buffer[..bytesToRead]);
        _position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (target < 0 || target > _length)
        {
            throw new IOException("Cannot seek outside the archive entry bounds.");
        }

        _stream.Position = checked(_start + target);
        _position = target;
        return _position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }

        base.Dispose(disposing);
    }
}
