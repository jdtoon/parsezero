using System.Buffers;
using System.Runtime.CompilerServices;

namespace ParseZero.Memory;

/// <summary>
/// A pooled buffer that returns memory to ArrayPool when disposed.
/// </summary>
internal sealed class PooledBuffer<T> : IDisposable
{
    private T[]? _array;
    private int _length;
    private bool _disposed;

    /// <summary>
    /// Creates a new pooled buffer with the specified minimum size.
    /// </summary>
    public PooledBuffer(int minimumSize)
    {
        _array = ArrayPool<T>.Shared.Rent(minimumSize);
        _length = 0;
    }

    /// <summary>
    /// Gets the underlying array.
    /// </summary>
    public T[] Array => _array ?? throw new ObjectDisposedException(nameof(PooledBuffer<T>));

    /// <summary>
    /// Gets the current length of valid data in the buffer.
    /// </summary>
    public int Length
    {
        get => _length;
        set
        {
            if (value < 0 || value > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _length = value;
        }
    }

    /// <summary>
    /// Gets the total capacity of the buffer.
    /// </summary>
    public int Capacity => _array?.Length ?? 0;

    /// <summary>
    /// Gets a span of the valid data.
    /// </summary>
    public Span<T> Span => Array.AsSpan(0, _length);

    /// <summary>
    /// Gets a read-only span of the valid data.
    /// </summary>
    public ReadOnlySpan<T> ReadOnlySpan => Array.AsSpan(0, _length);

    /// <summary>
    /// Gets a memory of the valid data.
    /// </summary>
    public Memory<T> Memory => Array.AsMemory(0, _length);

    /// <summary>
    /// Gets a span of available space for writing.
    /// </summary>
    public Span<T> AvailableSpan => Array.AsSpan(_length);

    /// <summary>
    /// Gets the available space in the buffer.
    /// </summary>
    public int AvailableSpace => Capacity - _length;

    /// <summary>
    /// Ensures the buffer has at least the specified capacity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int minimumCapacity)
    {
        if (_array is null)
        {
            throw new ObjectDisposedException(nameof(PooledBuffer<T>));
        }

        if (_array.Length >= minimumCapacity)
        {
            return;
        }

        var newArray = ArrayPool<T>.Shared.Rent(minimumCapacity);
        _array.AsSpan(0, _length).CopyTo(newArray);
        ArrayPool<T>.Shared.Return(_array);
        _array = newArray;
    }

    /// <summary>
    /// Advances the length by the specified count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _length += count;

        if (_length > Capacity)
        {
            _length = Capacity;
            throw new InvalidOperationException("Buffer overflow.");
        }
    }

    /// <summary>
    /// Resets the buffer length to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _length = 0;
    }

    /// <summary>
    /// Clears the buffer content and resets length.
    /// </summary>
    public void Clear()
    {
        if (_array is not null)
        {
            System.Array.Clear(_array, 0, _length);
        }
        _length = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_array is not null)
        {
            ArrayPool<T>.Shared.Return(_array);
            _array = null;
        }
    }
}

/// <summary>
/// Extension methods for working with pooled buffers.
/// </summary>
internal static class PooledBufferExtensions
{
    /// <summary>
    /// Writes data to the buffer and advances the position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this PooledBuffer<T> buffer, ReadOnlySpan<T> data)
    {
        buffer.EnsureCapacity(buffer.Length + data.Length);
        data.CopyTo(buffer.AvailableSpan);
        buffer.Advance(data.Length);
    }
}
