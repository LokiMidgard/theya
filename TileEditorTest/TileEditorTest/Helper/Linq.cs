global using TileEditorTest.Helper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TileEditorTest.Helper;

internal static class Linq {
    public static async IAsyncEnumerable<T> Where<T>(this IEnumerable<T> en, Func<T, Task<bool>> predicate) {
        foreach (var item in en) {
            if (await predicate(item)) {
                yield return item;
            }
        }
    }
    public static async IAsyncEnumerable<T> NotNull<T>(this IEnumerable<Task<T?>> en) where T : notnull {
        foreach (var item in en) {
            var i = await item;
            if (i is not null) {
                yield return i;
            }
        }
    }
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> en) where T : notnull {
        foreach (var i in en) {
            if (i is not null) {
                yield return i;
            }
        }
    }
    public static async IAsyncEnumerable<T> NotNull<T>(this IAsyncEnumerable<T?> en) where T : notnull {
        await foreach (var i in en) {
            if (i is not null) {
                yield return i;
            }
        }
    }

    public static void ForEach<T>(this IEnumerable<T> en, Action<T> forEachElement) {
        foreach (var item in en) {
            forEachElement(item);
        }
    }
    public static async Task ForEach<T>(this IEnumerable<T> en, Func<T, Task> forEachElement) {
        foreach (var item in en) {
            await forEachElement(item);
        }
    }
    public static Task ForEachAsync<T>(this IEnumerable<Task<T>> en, Action<T> forEachElement) {
        return Task.WhenAll(en.Select(async x => forEachElement(await x)));
    }
    public static Task ForEachAsync<T>(this IEnumerable<Task<T>> en, Func<T, Task> forEachElement) {
        return Task.WhenAll(en.Select(async x => await forEachElement(await x)));
    }
    public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> en, Action<T> forEachElement) {
        await foreach (var item in en) {
            forEachElement(item);
        }
    }
    public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> en, Func<T, Task> forEachElement) {
        await foreach (var item in en) {
            await forEachElement(item);
        }
    }
    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<Task<T>> en) {
        foreach (var item in en) {
            yield return await item;
        }
    }
}
