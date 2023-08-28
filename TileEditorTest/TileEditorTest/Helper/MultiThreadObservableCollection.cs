using Microsoft.UI.Dispatching;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileEditorTest.Helper;
public class MultiThreadObservableCollection<T> : ObservableCollection<T> {

    private Dictionary<NotifyCollectionChangedEventHandler, DispatcherQueue?> collectionHandlers = new();
    private Dictionary<PropertyChangedEventHandler, DispatcherQueue?> propertyHandlers = new();

    public override event NotifyCollectionChangedEventHandler? CollectionChanged {
        add {
            if (value is not null) {
                collectionHandlers.TryAdd(value, DispatcherQueue.GetForCurrentThread());
            }
        }
        remove {
            if (value is not null) {
                collectionHandlers.Remove(value);
            }
        }
    }

    protected override event PropertyChangedEventHandler? PropertyChanged {
        add {
            if (value is not null) {
                propertyHandlers.TryAdd(value, DispatcherQueue.GetForCurrentThread());
            }
        }
        remove {
            if (value is not null) {
                propertyHandlers.Remove(value);
            }
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
        foreach (var handler in collectionHandlers) {
            if (handler.Value is not null) {
                handler.Value.TryEnqueue(() => {
                    handler.Key(this, e);
                });
            } else {
                handler.Key(this, e);
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
        foreach (var handler in propertyHandlers) {
            if (handler.Value is not null) {
                handler.Value.TryEnqueue(() => {
                    handler.Key(this, e);
                });
            } else {
                handler.Key(this, e);
            }
        }
    }


}
