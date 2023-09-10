using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI.UI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;

using TileEditorTest.Model;
using TileEditorTest.View;
using TileEditorTest.View.Controls;
using TileEditorTest.View.Dialogs;
using TileEditorTest.ViewModel;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application {
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App() {
        this.InitializeComponent();
    }

    static App() {
        TabPathes = new(tabPathes);
        prjectItemTypeToloadersViewModel = GetViewModels().ToImmutableDictionary(x => x.projectItem, x => new Func<ProjectItem, CoreViewModel, Task<IViewModel>>((item, vm) => x.createViewModel(item, vm)));
    }

    [AutoInvoke.FindAndInvoke]
    private static (Type projectItem, Func<ProjectItem, CoreViewModel, Task<IViewModel>> createViewModel) GetViewModels<VM, OfFile>()
where OfFile : class, IProjectItemContent<OfFile>
where VM : IViewModel<OfFile, VM> {
        return (typeof(ProjectItem<OfFile>),
    async (f, vm) => {
        return await VM.Create((ProjectItem<OfFile>)f, vm);
    }
        );
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
        m_window = new MainWindow();
        TrackWindow(m_window);
        m_window.Activate();
    }

    private Window? m_window;

    public static new App Current => (App)Application.Current;

    public Window MainWindow => m_window ?? throw new InvalidOperationException("The app is not jet initilized");


    private static readonly ObservableCollection<TabViewItem> tabs = new();
    private static readonly ObservableCollection<Window> windows = new();

    private static readonly Dictionary<Window, NotificationArea> notificationAreas = new();

    public static ReadOnlyObservableCollection<Window> ActiveWindows { get; } = new(windows);
    public static ReadOnlyObservableCollection<TabViewItem> ActiveTabs { get; } = new(tabs);

    static public Window CreateWindow(DocumentsPage newPage) {
        Window newWindow = new() {
            SystemBackdrop = new MicaBackdrop(),
            ExtendsContentIntoTitleBar = true,
            Content = newPage
        };

        newPage.Loaded += (object sender, RoutedEventArgs e) => {
            FrameworkElement? titleBar = newWindow.Content.FindDescendant("AppTitleBar");
            newWindow.SetTitleBar(titleBar);
        };


        TrackWindow(newWindow);
        return newWindow;
    }

    static public void TrackWindow(Window window) {
        window.Closed += (sender, args) => {
            windows.Remove(window);
            notificationAreas.Remove(window);
        };
        RoutedEventHandler onLoad = null!;
        onLoad = (sender, e) => {
            var notificationArea = window.Content.FindDescendantOrSelf<NotificationArea>() ?? throw new ArgumentException("Window dose not contain a notification area.", nameof(window));
            notificationAreas.Add(window, notificationArea);
            ((FrameworkElement)window.Content).Loaded -= onLoad;
        };
        ((FrameworkElement)window.Content).Loaded += onLoad;

        windows.Add(window);
    }

    static public Window GetWindowForElement(UIElement element) {
        if (element.XamlRoot != null) {
            foreach (Window window in windows) {
                if (element.XamlRoot == window.Content.XamlRoot) {
                    return window;
                }
            }
        }
        throw new ArgumentException("Could not find Window for element.", nameof(element));
    }

    internal static NotificationArea GetNotificationAreaForElement(Window element) {
        return !notificationAreas.TryGetValue(element, out var notificationArea)
            ? throw new ArgumentException("Could not find NotificationArea for Window.", nameof(element))
            : notificationArea;
    }
    internal static NotificationArea GetNotificationAreaForElement(UIElement element) {
        return GetNotificationAreaForElement(GetWindowForElement(element));
    }

    public static ReadOnlyObservableGroupedCollection<ProjectPath, ViewLoader> TabPathes;
    private static ObservableGroupedCollection<ProjectPath, ViewLoader> tabPathes = new();


    internal static void CreateTab(DocumentsPage docs, ProjectItem item) {
        ViewLoader view = new() { ProjectViewModel = docs.ProjectViewModel, Item = item };

        tabPathes.AddItem(item.Path, view);
        tabs.Add(view);
        docs.AddAndFocusTab(view);
    }

    internal static async Task DestroyTab(ViewLoader tab) {
        var provideUndo = false;
        if (tab.Item is not null) {
            tabPathes.RemoveItem(tab.Item.Path, tab);
            if (!(tabPathes.FirstOrDefault(tab.Item.Path)?.Count > 0)) {
                provideUndo = true;
            }
        }
        tabs.Remove(tab);
        TabView parent = tab.FindAscendant<TabView>() ?? throw new ArgumentException("Tab is not in an TabView", nameof(tab));



        parent.TabItems.Remove(tab);
        if ((tab.ContentViewModel?.HasChanges ?? false) && provideUndo) {
            var undo = await ClosedUnsavedChangesInfo.ShowAsync(tab.Item?.Path ?? "UNKNOWN", parent);
            if (undo) {
                parent.TabItems.Add(tab);
                parent.SelectedItem = tab;
                return;
            }
        }
        tab.Dispose();
    }


    private static Dictionary<ProjectPath, (Task<IViewModel> task, int counterWrite, int counterRead)> viewModels = new();
    private static Dictionary<IViewModel, ProjectPath> reverseViewModels = new();
    private static ImmutableDictionary<Type, Func<ProjectItem, CoreViewModel, Task<IViewModel>>> prjectItemTypeToloadersViewModel;

    private static async Task ReturnViewModel(IViewModel viewModel, bool read) {
        if (!reverseViewModels.TryGetValue(viewModel, out var path) || !viewModels.TryGetValue(path, out var cached)) {
            return;
        }
        if (read) {
            cached.counterRead--;
        } else {
            cached.counterWrite--;
        }
        if (cached.counterWrite == 0 && cached.counterRead == 0) {
            viewModels.Remove(path);
            reverseViewModels.Remove(viewModel);
            if (viewModel is IAsyncDisposable asyncDisposable) {
                await asyncDisposable.DisposeAsync();
            } else if (viewModel is IDisposable disposable) {
                disposable.Dispose();
            }
        } else {
            if (cached.counterWrite == 0 && !read) {
                // if it was returned as writeMode, and we reached 0 reset
                await viewModel.RestoreValuesFromModel();
            }
            viewModels[path] = cached;
        }
    }

    internal static IViewModelLookup<OfFile> GetViewModel<OfFile>(ProjectItem<OfFile> file, CoreViewModel core, bool read = false)
        where OfFile : class, IProjectItemContent<OfFile> {
        return new ViewModelLookup<OfFile>(file, core, read);
    }
    internal static IAsyncDisposable GetViewModel(ProjectItem file, CoreViewModel core, out Task<IViewModel> viewModel, bool read = false) {
        return new ViewModelLookup(file, core, read).Of(out viewModel);
    }



    [InterfaceGenerator.GenerateAutoInterface]
    private partial class ViewModelLookup<OfFile> : IViewModelLookup<OfFile> where OfFile : class, IProjectItemContent<OfFile> {
        private ProjectItem<OfFile> file;
        private readonly CoreViewModel core;
        private readonly bool read;

        public ViewModelLookup(ProjectItem<OfFile> file, CoreViewModel core, bool read) {
            this.file = file;
            this.core = core;
            this.read = read;
        }

        public IAsyncDisposable Of(out Task<IViewModel> viewModel) {
            Task<IViewModel> result;

            if (viewModels.TryGetValue(file.Path, out var cached)) {
                result = cached.task;
            } else {
                result = prjectItemTypeToloadersViewModel[file.GetType()](file, core);
                result.ContinueWith(t => reverseViewModels.Add(t.Result, file.Path));
                cached = (result, 0, 0);
            }
            if (read) {
                cached.counterRead++;
            } else {
                cached.counterWrite++;
            }
            viewModels[file.Path] = cached;

            viewModel = result;
            return new AsyncDisposable(async () => {
                var data = await result;
#pragma warning disable CS0618 // Type or member is obsolete
                ReturnViewModel(data, read);
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }
        public IAsyncDisposable Of<T>(out Task<T> viewModel) where T : IViewModel<OfFile, T> {
            Task<T> result;

            if (viewModels.TryGetValue(file.Path, out var cached)) {
                result = cached.task.ContinueWith(t => (T)t.Result);
            } else {
                result = T.Create(file, core);
                result.ContinueWith(t => reverseViewModels.Add(t.Result, file.Path));
                cached = (result.ContinueWith(t => (IViewModel)t.Result), 0, 0);
            }
            if (read) {
                cached.counterRead++;
            } else {
                cached.counterWrite++;
            }
            viewModels[file.Path] = cached;
            viewModel = result;

            return new AsyncDisposable(async () => {
                var data = await result;
#pragma warning disable CS0618 // Type or member is obsolete
                ReturnViewModel(data, read);
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }
    }

    [InterfaceGenerator.GenerateAutoInterface]
    private partial class ViewModelLookup : IViewModelLookup {
        private ProjectItem file;
        private readonly CoreViewModel core;
        private readonly bool read;

        public ViewModelLookup(ProjectItem file, CoreViewModel core, bool read) {
            this.file = file;
            this.core = core;
            this.read = read;
        }

        public IAsyncDisposable Of(out Task<IViewModel> viewModel) {
            Task<IViewModel> result;

            if (viewModels.TryGetValue(file.Path, out var cached)) {
                result = cached.task;
            } else {
                result = prjectItemTypeToloadersViewModel[file.GetType()](file, core);
                result.ContinueWith(t => reverseViewModels.Add(t.Result, file.Path));
                cached = (result, 0, 0);
            }
            if (read) {
                cached.counterRead++;
            } else {
                cached.counterWrite++;
            }

            viewModels[file.Path] = cached;
            viewModel = result;

            return new AsyncDisposable(async () => {
                var data = await result;
#pragma warning disable CS0618 // Type or member is obsolete
                await ReturnViewModel(data, read);
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }

    }


}
