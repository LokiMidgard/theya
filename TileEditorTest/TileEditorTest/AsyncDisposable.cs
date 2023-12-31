﻿using System;
using System.Threading.Tasks;

namespace TileEditorTest;
internal sealed class AsyncDisposable : IAsyncDisposable {
    private readonly Func<Task> value;
    private readonly bool disposed;

    public AsyncDisposable(Func<Task> value) {
        this.value = value;
    }

    public async ValueTask DisposeAsync() {
        if (disposed) {
            return;
        }
        await value();
    }
}

internal sealed class Disposable : IDisposable {
    private readonly Action value;
    private readonly bool disposed;

    public Disposable(Action value) {
        this.value = value;
    }

    public void Dispose() {
        if (disposed) {
            return;
        }
        value();
    }
}