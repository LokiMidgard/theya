using System;
using System.Threading.Tasks;

namespace TileEditorTest;
internal class AsyncDisposable : IAsyncDisposable {
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