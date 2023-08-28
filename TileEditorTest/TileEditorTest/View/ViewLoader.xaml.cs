using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.Model;

using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View;
public sealed partial class ViewLoader : TabViewItem, IDisposable {

    /// <summary>
    /// disables the internal update when Item or ProjectViewModel is set. Necessary for cloning to another Thread…
    /// </summary>
    private bool disableUpdate;

    private readonly ImmutableDictionary<Type, Func<ProjectItem, ProjectViewModel, Task<IViewModel>>> loadersViewModel;
    private readonly ImmutableDictionary<Type, Func<IViewModel, Control>> loadersView;




    internal IViewModel? ContentViewModel {
        get { return (IViewModel?)GetValue(ContentViewModelProperty); }
        set { SetValue(ContentViewModelProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ContentViewModel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ContentViewModelProperty =
        DependencyProperty.Register("ContentViewModel", typeof(IViewModel), typeof(ViewLoader), new PropertyMetadata(null));



    internal ProjectItem? Item {
        get { return (ProjectItem?)GetValue(ItemProperty); }
        set { SetValue(ItemProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Item.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register("Item", typeof(ProjectItem), typeof(ViewLoader), new PropertyMetadata(null, ItemChanged));



    public bool IsLoading {
        get { return (bool)GetValue(IsLoadingProperty); }
        set { SetValue(IsLoadingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register("IsLoading", typeof(bool), typeof(ViewLoader), new PropertyMetadata(false));



    internal ProjectViewModel? ProjectViewModel {
        get { return (ProjectViewModel?)GetValue(ProjectViewModelProperty); }
        set { SetValue(ProjectViewModelProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ProjectViewModelProperty =
        DependencyProperty.Register("ProjectViewModel", typeof(ProjectViewModel), typeof(ViewLoader), new PropertyMetadata(null, ProjectChanged));

    private static void ProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (ViewLoader)d;
        if (e.NewValue is not null) {
            me.ItemChanged(me.Item, me.Item);
        } else {
            me.ItemChanged(null, me.Item);
        }

    }

    public Control? Control {
        get { return (Control?)GetValue(ControlProperty); }
        set { SetValue(ControlProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Control.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ControlProperty =
        DependencyProperty.Register("Control", typeof(Control), typeof(ViewLoader), new PropertyMetadata(null));

    private static Dictionary<ProjectPath, (IViewModel model, int count)> existingViewmodels = new();
    private bool disposedValue;

    private static void ItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var me = (ViewLoader)d;
        if (e.NewValue != e.OldValue) {
            me.ItemChanged(e.NewValue as ProjectItem, e.OldValue as ProjectItem);
        }
    }
    private async void ItemChanged(ProjectItem? newItem, ProjectItem? oldItem) {
        if (disableUpdate || disposedValue) {
            return;
        }
        this.IsLoading = true;
        ClearItem(oldItem);
        this.ContentViewModel = null;

        if (newItem is not null && ProjectViewModel is not null) {
            if (!existingViewmodels.TryGetValue(newItem.Path, out var fonud)) {
                var content = await newItem.Content;
                var createViewModel = this.loadersViewModel[content.GetType()];
                fonud = (await createViewModel(newItem, ProjectViewModel), 1);
            } else {
                fonud = (fonud.model, fonud.count + 1);
            }
            if (!disposedValue) { // Explicitly check again after the awaits
                existingViewmodels[newItem.Path] = fonud;
                var (vm, counter) = fonud;
                this.ContentViewModel = vm;
                var createControl = this.loadersView[vm.GetType()];
                var control = createControl(vm);
                this.Control = control;
            }
        }
        this.IsLoading = false;
    }

    private void ClearItem(ProjectItem? item) {
        if (this.Control is not null && item is not null) {
            if (existingViewmodels.TryGetValue(item.Path, out var toDecriment)) {
                toDecriment = (toDecriment.model, toDecriment.count - 1);
                if (toDecriment.count == 0) {
                    existingViewmodels.Remove(item.Path);
                } else {
                    existingViewmodels[item.Path] = toDecriment;
                }
            }
            this.Control = null;
        }
    }

    public ViewLoader() {
        this.loadersViewModel = this.GetView().ToImmutableDictionary(x => x.fileType, x => new Func<ProjectItem, ProjectViewModel, Task<IViewModel>>((item, vm) => x.createViewModel(item, vm)));
        this.loadersView = this.GetView().ToImmutableDictionary(x => x.viewModelType, x => new Func<IViewModel, Control>((vm) => x.createView(vm)));
        this.InitializeComponent();
    }

    [AutoInvoke.FindAndInvoke]
    private (Type fileType, Type viewModelType, Func<ProjectItem, ProjectViewModel, Task<IViewModel>> createViewModel, Func<IViewModel, Control> createView) GetView<View, VM, OfFile>()
    where OfFile : class, IProjectItemContent<OfFile>
    where VM : IViewModel<OfFile, VM>
    where View : Control, IView<OfFile, VM, View> {

        return (fileType: typeof(OfFile), viewModelType: typeof(VM),
            async (f, vm) => {
                return await VM.Create((ProjectItem<OfFile>)f, vm);
            }, (vm) => {
                return View.Create((VM)vm);
            }
        );
    }



    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                ClearItem(this.Item);
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ViewLoader()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
