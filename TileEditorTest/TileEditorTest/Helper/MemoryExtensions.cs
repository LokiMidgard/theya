global using TileEditorTest.Helper.MemoryExtension;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileEditorTest.Helper.MemoryExtension;



internal static partial class MemoryExtensions {

    public static SpanSplitEnumerator<char> Spliterator(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator)
        => new(span, separator);
}

internal ref struct SpanSplitEnumerator<T> where T : IEquatable<T> {
    private readonly ReadOnlySpan<T> toSplit;
    private readonly ReadOnlySpan<T> separator;
    private int offset;
    private int index;

    public readonly SpanSplitEnumerator<T> GetEnumerator() => this;

    public SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator) {
        toSplit = span;
        this.separator = separator;
        index = 0;
        offset = 0;
    }

    public readonly ReadOnlySpan<T> Current => toSplit.Slice(offset, index - 1);

    public bool MoveNext() {
        if (toSplit.Length - offset < index) { return false; }
        var slice = toSplit.Slice(offset += index);

        var nextIndex = slice.IndexOf(separator);
        index = (nextIndex != -1 ? nextIndex + separator.Length : slice.Length + 1);
        return true;
    }
}