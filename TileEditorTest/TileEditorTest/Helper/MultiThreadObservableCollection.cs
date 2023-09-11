using CommunityToolkit.Common.Collections;

using Microsoft.Toolkit.Collections;
using Microsoft.UI.Dispatching;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TileEditorTest.Helper;

public static class Grouping {

    public static ReadOnlyObservableCollection<TValue> SelectObservable<TSource, TValue>(this ReadOnlyObservableCollection<TSource> collection, Func<TSource, TValue> selector) {
        return SelectObservableInternal(collection, selector);
    }
    public static ReadOnlyObservableCollection<TValue> SelectObservable<TSource, TValue>(this ObservableCollection<TSource> collection, Func<TSource, TValue> selector) {
        return SelectObservableInternal(collection, selector);
    }
    private static ReadOnlyObservableCollection<TValue> SelectObservableInternal<TSource, TValue, TCollection>(this TCollection collection, Func<TSource, TValue> selector)
    where TCollection : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged {
        ObservableCollection<TValue> observable = new(collection.Select(selector));
        collection.CollectionChanged += (sender, e) => {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null) {
                if (e.NewStartingIndex != -1) {
                    for (int i = e.NewItems.Count - (1); i >= 0; i--) {
                        TValue item = selector((TSource)e.NewItems[i]!);
                        observable.Insert(e.NewStartingIndex, item);
                    }
                } else {
                    foreach (TSource newItem in e.NewItems) {
                        observable.Add(selector(newItem));
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Reset) {
                observable.Clear();
                foreach (var newItem in collection) {
                    observable.Add(selector(newItem));
                }
            } else if (e.Action == NotifyCollectionChangedAction.Move) {
                if (e.OldItems is null || e.OldItems.Count == 1) {
                    observable.Move(e.OldStartingIndex, e.NewStartingIndex);
                } else {
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        observable.RemoveAt(e.OldStartingIndex);
                    }
                    for (int i = e.OldItems.Count - (1); i >= 0; i--) {
                        observable.Insert(e.NewStartingIndex, selector((TSource)e.OldItems[i]!));// TValue is notnull
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < (e.OldItems?.Count ?? 1); i++) {
                        observable.RemoveAt(e.OldStartingIndex);
                    }
                } else {
                    foreach (TSource item1 in e.OldItems ?? throw new InvalidOperationException($"At least one of must be set: {nameof(e.OldStartingIndex)}, {nameof(e.OldItems)}")) {
                        observable.Remove(selector(item1));
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (e.NewItems is null) {
                    throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                }
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        observable[i + e.OldStartingIndex] = selector((TSource)e.NewItems[i]!);
                    }
                } else {
                    if (e.OldItems is null) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                    }
                    if (e.OldItems.Count != e.NewItems.Count) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} and {nameof(e.OldItems)} must have the same amount of Elements.");
                    }
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        var value = selector((TSource)e.OldItems[0]!);
                        var index = observable.IndexOf(value);
                        observable[index] = value;
                    }
                }
            } else {
                throw new NotImplementedException($"I forgot action {e.Action}");
            }
        };
        return new(observable);
    }

    public static GroupingConfiguration<ReadOnlyObservableCollection<TSource>, TSource> ToGrouping<TSource>(this ReadOnlyObservableCollection<TSource> collection)
        where TSource : notnull {
        return new(collection);
    }
    public static GroupingConfiguration<ObservableCollection<TSource>, TSource> ToGrouping<TSource>(this ObservableCollection<TSource> collection)
        where TSource : notnull {
        return new(collection);
    }
    public static GroupingConfiguration<TCollectionKey, TSource> ToGrouping<TCollectionKey, TSource>(this TCollectionKey collection)
        where TSource : notnull
        where TCollectionKey : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged {
        return new(collection);
    }

    private static ReadOnlyObservableGroupe<TKey, TValue> ToGroupingTaskInternal<TSource, TValue, TKey, TCollectionKey, TCollectionValue>(TCollectionKey collection, Func<TSource, Task<TCollectionValue>> valueSelector, Func<TSource, TKey> keySelector)
    where TCollectionKey : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged
    where TCollectionValue : IList<TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    where TSource : notnull
    where TKey : notnull {
        
        ObservableGroupedCollection<TKey, TValue> observable = new();
        Dictionary<TKey, Disposable> disposes = new();

        foreach (var item in collection) {
            ObservableGroup<TKey, TValue> group = new(keySelector(item));
            valueSelector(item).ContinueWith(task => {
                var subCollection = task.Result;
                foreach (var subItem in subCollection) {
                    group.Add(subItem);
                }
                observable.Add(group);
                NotifyCollectionChangedEventHandler changedMethod;
                changedMethod = (sender, e) => {
                    if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null) {
                        foreach (TSource item in e.NewItems) {
                            valueSelector(item).ContinueWith(task => {
                                var subCollection = task.Result;
                                var key = keySelector(item);
                                ObservableGroup<TKey, TValue> group = new(key);
                                foreach (var subItem in subCollection) {
                                    group.Add(subItem);
                                }
                                disposes.Add(key, RegisterSubCollection(group, subCollection));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    } else if (e.Action == NotifyCollectionChangedAction.Reset) {
                        foreach (var disposable in disposes.Values) {
                            disposable.Dispose();
                        }
                        disposes.Clear();
                        foreach (var group in observable) {
                            group.Clear();
                        }
                        observable.Clear();
                        foreach (var item in collection) {
                            var key = keySelector(item);
                            ObservableGroup<TKey, TValue> group = new(key);
                            valueSelector(item).ContinueWith(task => {
                                var subCollection = task.Result;
                                foreach (var subItem in subCollection) {
                                    group.Add(subItem);
                                }
                                observable.Add(group);
                                disposes.Add(key, RegisterSubCollection(group, subCollection));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    } else if (e.Action == NotifyCollectionChangedAction.Move) {
                        if (e.OldItems is null || e.OldItems.Count == 1) {
                            observable.Move(e.OldStartingIndex, e.NewStartingIndex);
                        } else {
                            for (int i = 0; i < e.OldItems.Count; i++) {
                                observable.RemoveAt(e.OldStartingIndex);
                            }
                            for (int i = e.OldItems.Count - (1); i >= 0; i--) {
                                observable.Insert(e.NewStartingIndex, (ObservableGroup<TKey, TValue>)e.OldItems[i]!);// TValue is notnull
                            }
                        }
                    } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                        if (e.OldStartingIndex != -1) {
                            for (int i = 0; i < (e.OldItems?.Count ?? 1); i++) {
                                var toRemove = observable[e.OldStartingIndex];
                                disposes[toRemove.Key].Dispose();
                                disposes.Remove(toRemove.Key);
                                observable.RemoveAt(e.OldStartingIndex);
                            }
                        } else {
                            foreach (TSource item1 in e.OldItems ?? throw new InvalidOperationException($"At least one of must be set: {nameof(e.OldStartingIndex)}, {nameof(e.OldItems)}")) {
                                var key = keySelector(item1);
                                disposes[key].Dispose();
                                disposes.Remove(key);
                                observable.RemoveGroup(key);
                            }
                        }
                    } else if (e.Action == NotifyCollectionChangedAction.Replace) {
                        if (e.NewItems is null) {
                            throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                        }
                        if (e.OldStartingIndex != -1) {
                            for (int i = 0; i < e.NewItems.Count; i++) {
                                var newItem = (TSource)e.NewItems[i]!;
                                var newKey = keySelector(newItem);
                                ObservableGroup<TKey, TValue> newGroup = new(newKey);
                                valueSelector(newItem).ContinueWith(task => {
                                    var subCollection = task.Result;
                                    foreach (var subItem in subCollection) {
                                        newGroup.Add(subItem);
                                    }
                                    disposes.Add(newKey, RegisterSubCollection(newGroup, subCollection));
                                    var oldKey = observable[i + e.OldStartingIndex].Key;
                                    disposes[oldKey].Dispose();
                                    disposes.Remove(oldKey);
                                    observable[i + e.OldStartingIndex] = newGroup;
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        } else {
                            if (e.OldItems is null) {
                                throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                            }
                            if (e.OldItems.Count != e.NewItems.Count) {
                                throw new InvalidOperationException($"{nameof(e.NewItems)} and {nameof(e.OldItems)} must have the same amount of Elements.");
                            }
                            for (int i = 0; i < e.OldItems.Count; i++) {
                                var oldKey = keySelector((TSource)e.OldItems[0]!);
                                var group = observable.FirstOrDefault(oldKey) ?? throw new InvalidOperationException($"Did not find group to key {oldKey}");
                                var index = observable.IndexOf(group);
                                var newItem = (TSource)e.NewItems[i]!;
                                var newKey = keySelector(newItem);
                                ObservableGroup<TKey, TValue> newGroup = new(newKey);
                                valueSelector(newItem).ContinueWith(task => {
                                    var subCollection = task.Result;
                                    foreach (var subItem in subCollection) {
                                        newGroup.Add(subItem);
                                    }
                                    disposes.Add(newKey, RegisterSubCollection(newGroup, subCollection));
                                    var oldKey = observable[index].Key;
                                    disposes[oldKey].Dispose();
                                    disposes.Remove(oldKey);
                                    observable[index] = newGroup;
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            }
                        }
                    } else {
                        throw new NotImplementedException($"I forgot action {e.Action}");
                    }
                };
                collection.CollectionChanged += changedMethod;


            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        return new(observable);
    }




    private static ReadOnlyObservableGroupe<TKey, TValue> ToGroupingInternal<TSource, TValue, TKey, TCollectionKey, TCollectionValue>(TCollectionKey collection, Func<TSource, TCollectionValue> valueSelector, Func<TSource, TKey> keySelector)
        where TCollectionKey : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged
        where TCollectionValue : IList<TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        where TSource : notnull
        where TKey : notnull {
        ObservableGroupedCollection<TKey, TValue> observable = new();
        Dictionary<TKey, Disposable> disposes = new();

        foreach (var item in collection) {
            var key = keySelector(item);
            ObservableGroup<TKey, TValue> group = new(key);
            var subCollection = valueSelector(item);
            foreach (var subItem in subCollection) {
                group.Add(subItem);
            }
            disposes.Add(key, RegisterSubCollection(group, subCollection));
            observable.Add(group);
        }

        collection.CollectionChanged += (sender, e) => {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null) {
                foreach (TSource item in e.NewItems) {
                    var key = keySelector(item);
                    ObservableGroup<TKey, TValue> group = new(key);
                    var subCollection = valueSelector(item);
                    foreach (var subItem in subCollection) {
                        group.Add(subItem);
                    }
                    disposes.Add(key, RegisterSubCollection(group, subCollection));
                    observable.Add(group);
                }
            } else if (e.Action == NotifyCollectionChangedAction.Reset) {
                foreach (var disposable in disposes.Values) {
                    disposable.Dispose();
                }
                foreach (var group in observable) {
                    group.Clear();
                }
                observable.Clear();
                foreach (var item in collection) {
                    var key = keySelector(item);
                    ObservableGroup<TKey, TValue> group = new(key);
                    var subCollection = valueSelector(item);
                    foreach (var subItem in subCollection) {
                        group.Add(subItem);
                    }
                    observable.Add(group);
                    disposes.Add(key, RegisterSubCollection(group, subCollection));
                }
            } else if (e.Action == NotifyCollectionChangedAction.Move) {
                if (e.OldItems is null || e.OldItems.Count == 1) {
                    observable.Move(e.OldStartingIndex, e.NewStartingIndex);
                } else {
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        observable.RemoveAt(e.OldStartingIndex);
                    }
                    for (int i = e.OldItems.Count - (1); i >= 0; i--) {
                        observable.Insert(e.NewStartingIndex, (ObservableGroup<TKey, TValue>)e.OldItems[i]!);// TValue is notnull
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < (e.OldItems?.Count ?? 1); i++) {
                        var removing = observable[e.OldStartingIndex];
                        disposes[removing.Key].Dispose();
                        disposes.Remove(removing.Key);
                        observable.RemoveAt(e.OldStartingIndex);
                    }
                } else {
                    foreach (TSource oldItem in e.OldItems ?? throw new InvalidOperationException($"At least one of must be set: {nameof(e.OldStartingIndex)}, {nameof(e.OldItems)}")) {
                        var key = keySelector(oldItem);
                        disposes[key].Dispose();
                        disposes.Remove(key);
                        observable.RemoveGroup(key);
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (e.NewItems is null) {
                    throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                }
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        var newItem = (TSource)e.NewItems[i]!;
                        var key = keySelector(newItem);
                        ObservableGroup<TKey, TValue> newGroup = new(key);
                        var subCollection = valueSelector(newItem);
                        foreach (var subItem in subCollection) {
                            newGroup.Add(subItem);
                        }
                        disposes.Add(key, RegisterSubCollection(newGroup, subCollection));
                        var oldGroup = observable[i + e.OldStartingIndex];
                        disposes[oldGroup.Key].Dispose();
                        disposes.Remove(oldGroup.Key);
                        observable[i + e.OldStartingIndex] = newGroup;
                    }
                } else {
                    if (e.OldItems is null) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                    }
                    if (e.OldItems.Count != e.NewItems.Count) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} and {nameof(e.OldItems)} must have the same amount of Elements.");
                    }
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        var oldKey = keySelector((TSource)e.OldItems[0]!);
                        var group = observable.FirstOrDefault(oldKey) ?? throw new InvalidOperationException($"Did not find group to key {oldKey}");
                        var index = observable.IndexOf(group);
                        var newItem = (TSource)e.NewItems[i]!;
                        var newKey = keySelector(newItem);
                        ObservableGroup<TKey, TValue> newGroup = new(newKey);
                        var subCollection = valueSelector(newItem);
                        foreach (var subItem in subCollection) {
                            newGroup.Add(subItem);
                        }
                        disposes.Add(newKey, RegisterSubCollection(newGroup, subCollection));
                        var oldGroup = observable[index];
                        disposes[oldKey].Dispose();
                        disposes.Remove(oldKey);
                        observable[index] = newGroup;
                    }
                }
            } else {
                throw new NotImplementedException($"I forgot action {e.Action}");
            }
        };



        return new(observable);
    }

    private static Disposable RegisterSubCollection<TValue, TKey, TCollectionValue>(ObservableGroup<TKey, TValue> newGroup, TCollectionValue subCollection)
        where TKey : notnull
        where TCollectionValue : IList<TValue>, INotifyCollectionChanged, INotifyPropertyChanged {
        NotifyCollectionChangedEventHandler changedCallback = (sender, e) => {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null) {
                if (e.NewStartingIndex != -1) {
                    for (int i = e.NewItems.Count - (1); i >= 0; i--) {
                        TValue item = (TValue)e.NewItems[i]!; // TValue is notnull
                        newGroup.Insert(e.NewStartingIndex, item);
                    }
                } else {
                    foreach (TValue newItem in e.NewItems) {
                        newGroup.Add(newItem);
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Reset) {
                newGroup.Clear();
                foreach (var newItem in subCollection) {
                    newGroup.Add(newItem);
                }
            } else if (e.Action == NotifyCollectionChangedAction.Move) {
                if (e.OldItems is null || e.OldItems.Count == 1) {
                    newGroup.Move(e.OldStartingIndex, e.NewStartingIndex);
                } else {
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        newGroup.RemoveAt(e.OldStartingIndex);
                    }
                    for (int i = e.OldItems.Count - (1); i >= 0; i--) {
                        newGroup.Insert(e.NewStartingIndex, (TValue)e.OldItems[i]!);// TValue is notnull
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < (e.OldItems?.Count ?? 1); i++) {
                        newGroup.RemoveAt(e.OldStartingIndex);
                    }
                } else {
                    foreach (TValue item1 in e.OldItems ?? throw new InvalidOperationException($"At least one of must be set: {nameof(e.OldStartingIndex)}, {nameof(e.OldItems)}")) {
                        newGroup.Remove(item1);
                    }
                }
            } else if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (e.NewItems is null) {
                    throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                }
                if (e.OldStartingIndex != -1) {
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        newGroup[i + e.OldStartingIndex] = (TValue)e.NewItems[i]!;
                    }
                } else {
                    if (e.OldItems is null) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} must be set on Replace");
                    }
                    if (e.OldItems.Count != e.NewItems.Count) {
                        throw new InvalidOperationException($"{nameof(e.NewItems)} and {nameof(e.OldItems)} must have the same amount of Elements.");
                    }
                    for (int i = 0; i < e.OldItems.Count; i++) {
                        var index = newGroup.IndexOf((TValue)e.OldItems[0]!);
                        newGroup[index] = (TValue)e.NewItems[i]!;
                    }
                }
            } else {
                throw new NotImplementedException($"I forgot action {e.Action}");
            }
        };
        subCollection.CollectionChanged += changedCallback;
        return new Disposable(() => subCollection.CollectionChanged -= changedCallback);
    }

    public class GroupingConfiguration<TCollectionKey, TSource>
        where TCollectionKey : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged
        where TSource : notnull {
        private TCollectionKey collection;

        public GroupingConfiguration(TCollectionKey collection) {
            this.collection = collection;
        }

        public GroupingConfiguration<TCollectionKey, TSource, TKey> WithKey<TKey>(Func<TSource, TKey> keySelector)
            where TKey : notnull {
            return new(collection, keySelector);


        }

    }
    public class GroupingConfiguration<TCollectionKey, TSource, TKey>
        where TCollectionKey : IList<TSource>, INotifyCollectionChanged, INotifyPropertyChanged
        where TSource : notnull
        where TKey : notnull {
        private TCollectionKey collection;
        private Func<TSource, TKey> keySelector;

        public GroupingConfiguration(TCollectionKey collection, Func<TSource, TKey> keySelector) {
            this.collection = collection;
            this.keySelector = keySelector;
        }

        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TValue>(Func<TSource, Task<ReadOnlyObservableCollection<TValue>>> valueSelector) {
            return WithSubCollection<ReadOnlyObservableCollection<TValue>, TValue>(valueSelector);
        }
        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TValue>(Func<TSource, Task<ObservableCollection<TValue>>> valueSelector) {
            return WithSubCollection<ObservableCollection<TValue>, TValue>(valueSelector);
        }
        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TValue>(Func<TSource, ReadOnlyObservableCollection<TValue>> valueSelector) {
            return WithSubCollection<ReadOnlyObservableCollection<TValue>, TValue>(valueSelector);
        }
        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TValue>(Func<TSource, ObservableCollection<TValue>> valueSelector) {
            return WithSubCollection<ObservableCollection<TValue>, TValue>(valueSelector);
        }


        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TCollectionValue, TValue>(Func<TSource, TCollectionValue> valueSelector)
             where TCollectionValue : IList<TValue>, INotifyCollectionChanged, INotifyPropertyChanged {
            return ToGroupingInternal<TSource, TValue, TKey, TCollectionKey, TCollectionValue>(collection, valueSelector, keySelector);

        }
        public ReadOnlyObservableGroupe<TKey, TValue> WithSubCollection<TCollectionValue, TValue>(Func<TSource, Task<TCollectionValue>> valueSelector)
             where TCollectionValue : IList<TValue>, INotifyCollectionChanged, INotifyPropertyChanged {
            return ToGroupingTaskInternal<TSource, TValue, TKey, TCollectionKey, TCollectionValue>(collection, valueSelector, keySelector);
        }
    }
}

// This is the implementation form CommunityToolkit, extended with dispose
public sealed class ReadOnlyObservableGroupe<TKey, TValue> : ReadOnlyObservableCollection<ReadOnlyObservableGroup<TKey, TValue>>, IDisposable
   where TKey : notnull {
    private readonly ObservableGroupedCollection<TKey, TValue>? notifyingCollection;
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableGroupe{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="collection">The source collection to wrap.</param>
    internal ReadOnlyObservableGroupe(ObservableGroupedCollection<TKey, TValue> collection)
        : this(collection.Select(static g => new ReadOnlyObservableGroup<TKey, TValue>(g))) {
        this.notifyingCollection = collection;
        this.notifyingCollection.CollectionChanged += OnSourceCollectionChanged;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableGroupe{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="collection">The initial data to add in the grouped collection.</param>
    internal ReadOnlyObservableGroupe(IEnumerable<ReadOnlyObservableGroup<TKey, TValue>> collection)
        : base(new ObservableCollection<ReadOnlyObservableGroup<TKey, TValue>>(collection)) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableGroupe{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="collection">The initial data to add in the grouped collection.</param>
    internal ReadOnlyObservableGroupe(IEnumerable<IGrouping<TKey, TValue>> collection)
        : this(collection.Select(static g => new ReadOnlyObservableGroup<TKey, TValue>(g.Key, g))) {
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        // Even if NotifyCollectionChangedEventArgs allows multiple items, the actual implementation
        // is only reporting the changes one by one. We consider only this case for now.
        if (e.OldItems?.Count > 1 || e.NewItems?.Count > 1) {
            static void ThrowNotSupportedException() {
                throw new NotSupportedException(
                    "ReadOnlyObservableGroupedCollection<TKey, TValue> doesn't support operations on multiple items at once.\n" +
                    "If this exception was thrown, it likely means support for batched item updates has been added to the " +
                    "underlying ObservableCollection<T> type, and this implementation doesn't support that feature yet.\n" +
                    "Please consider opening an issue in https://aka.ms/windowstoolkit to report this.");
            }

            ThrowNotSupportedException();
        }

        switch (e.Action) {
            case NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace:

                // We only need to find the new item if the operation is either add or remove. In this
                // case we just directly find the first item that was modified, or throw if it's not present.
                // This normally never happens anyway - add and replace should always have a target element.
                ObservableGroup<TKey, TValue> newItem = e.NewItems!.Cast<ObservableGroup<TKey, TValue>>().First();

                if (e.Action == NotifyCollectionChangedAction.Add) {
                    Items.Insert(e.NewStartingIndex, new ReadOnlyObservableGroup<TKey, TValue>(newItem));
                } else {
                    Items[e.OldStartingIndex] = new ReadOnlyObservableGroup<TKey, TValue>(newItem);
                }

                break;
            case NotifyCollectionChangedAction.Move:

                // Our inner Items list is our own ObservableCollection<ReadOnlyObservableGroup<TKey, TValue>> so we can safely cast Items to its concrete type here.
                ((ObservableCollection<ReadOnlyObservableGroup<TKey, TValue>>)Items).Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                Items.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                Items.Clear();
                break;
            default:
                Debug.Fail("unsupported value");
                break;
        }
    }


    public void Dispose() {
        if (disposedValue) {
            return;
        }
        disposedValue = true;
        if (notifyingCollection is not null) {
            this.notifyingCollection.Clear();
            this.notifyingCollection.CollectionChanged -= OnSourceCollectionChanged;
        }

        GC.SuppressFinalize(this);
    }
}

