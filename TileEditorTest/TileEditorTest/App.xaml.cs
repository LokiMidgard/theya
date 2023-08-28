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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.Model;
using TileEditorTest.View;
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

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
        m_window = new MainWindow();
        m_window.Activate();
        TrackWindow(m_window);
    }

    private Window? m_window;

    public static new App Current => (App)Application.Current;

    public Window MainWindow => m_window ?? throw new InvalidOperationException("The app is not jet initilized");


    private static readonly ObservableCollection<TabViewItem> tabs = new();
    private static readonly ObservableCollection<Window> windows = new();
    public static ReadOnlyObservableCollection<Window> ActiveWindows { get; } = new(windows);
    public static ReadOnlyObservableCollection<TabViewItem> ActiveTabs { get; } = new(tabs);

    static public Window CreateWindow() {
        Window newWindow = new Window {
            SystemBackdrop = new MicaBackdrop()
        };
        TrackWindow(newWindow);
        return newWindow;
    }

    static public void TrackWindow(Window window) {
        window.Closed += (sender, args) => {
            windows.Remove(window);
        };
        windows.Add(window);
    }

    static public Window? GetWindowForElement(UIElement element) {
        if (element.XamlRoot != null) {
            foreach (Window window in windows) {
                if (element.XamlRoot == window.Content.XamlRoot) {
                    return window;
                }
            }
        }
        return null;
    }

    public static ReadOnlyObservableGroupedCollection<ProjectPath, ViewLoader> TabPathes;
    private static ObservableGroupedCollection<ProjectPath, ViewLoader> tabPathes = new();

    static App() {
        TabPathes = new(tabPathes);
    }

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


}
