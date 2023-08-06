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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.Model;

using TileEditorTest.Viewmodel;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;

using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DocumentsPage : Page
{
    private const string DataIdentifier = "TileDocumentTab";

    public DocumentsPage()
    {
        this.InitializeComponent();
        Tabs.TabItemsChanged += Tabs_TabItemsChanged;
    }




    private void Tabs_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        // If there are no more tabs, close the window.
        if (sender.TabItems.Count == 0)
        {
            WindowHelper.GetWindowForElement(this)?.Close();
        }
        // If there is only one tab left, disable dragging and reordering of Tabs.
        else if (sender.TabItems.Count == 1)
        {
            sender.CanReorderTabs = false;
            sender.CanDragTabs = false;
        }
        else
        {
            sender.CanReorderTabs = true;
            sender.CanDragTabs = true;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        SetupWindow();
    }

    void SetupWindow()
    {

        // Main Window -- add some default items
        //for (int i = 0; i < 3; i++)
        //{
        //    Tabs.TabItems.Add(new TabViewItem()
        //    {
        //        IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource()
        //        {
        //            Symbol = Symbol.Placeholder
        //        },
        //        Header = $"Item {i}",
        //        Content = new MyTabContentControl() { DataContext = $"Page {i}" }
        //    });
        //}

        //Tabs.SelectedIndex = 0;
    }

    public void AddTabToTabs(TabViewItem tab)
    {
        Tabs.TabItems.Add(tab);
    }

    // Create a new Window once the Tab is dragged outside.
    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        var newPage = new DocumentsPage();

        Tabs.TabItems.Remove(args.Tab);
        newPage.AddTabToTabs(args.Tab);

        var newWindow = WindowHelper.CreateWindow();
        newWindow.ExtendsContentIntoTitleBar = true;
        newWindow.Content = newPage;

        newWindow.Activate();
    }

    private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        // We can only drag one tab at a time, so grab the first one...
        var firstItem = args.Tab;

        // ... set the drag data to the tab...
        args.Data.Properties.Add(DataIdentifier, firstItem);

        // ... and indicate that we can move it
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private async void Tabs_TabStripDrop(object sender, DragEventArgs e)
    {
        // This event is called when we're dragging between different TabViews
        // It is responsible for handling the drop of the item into the second TabView

        if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
        {
            // Ensure that the obj property is set before continuing.
            if (obj == null)
            {
                return;
            }

            var destinationTabView = sender as TabView;
            var destinationItems = destinationTabView.TabItems;

            if (destinationItems != null)
            {
                // First we need to get the position in the List to drop to
                var index = -1;

                // Determine which items in the list our pointer is between.
                for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                {
                    var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                    if (e.GetPosition(item).X - item.ActualWidth < 0)
                    {
                        index = i;
                        break;
                    }
                }

                // The TabViewItem can only be in one tree at a time. Before moving it to the new TabView, remove it from the old.
                // Note that this call can happen on a different thread if moving across windows. So make sure you call methods on
                // the same thread as where the UI Elements were created.

                var element = (UIElement)obj;

                var taskCompletionSource = new TaskCompletionSource<(string header, TileMapEditorViewmodel datacontext)>();

                element.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                    new DispatcherQueueHandler(() =>
                    {
                        var tabItem = (TabViewItem)obj;
                        var destinationTabViewListView = (TabViewListView)tabItem.Parent;
                        destinationTabViewListView.Items.Remove(obj);
                        var header = tabItem.Header;
                        var dataContext = (tabItem.Content as TileMapEditorControl)?.DataContext as TileMapEditorViewmodel;

                        if (dataContext != null)
                            taskCompletionSource.SetResult((header?.ToString() ?? string.Empty, dataContext));
                        else
                            taskCompletionSource.SetException(new InvalidOperationException("Could not get Datacontext"));
                    }));

                var (header, dataContext) = await taskCompletionSource.Task;

                var insertedItem = CreateNewTVI(header.ToString(), dataContext);
                if (index < 0)
                {
                    // We didn't find a transition point, so we're at the end of the list
                    destinationItems.Add(insertedItem);
                }
                else if (index < destinationTabView.TabItems.Count)
                {
                    // Otherwise, insert at the provided index.
                    destinationItems.Insert(index, insertedItem);
                }

                // Select the newly dragged tab
                destinationTabView.SelectedItem = insertedItem;
            }
        }
    }

    private TabViewItem CreateNewTVI(string header, TileMapEditorViewmodel dataContext)
    {
        var newTab = new TabViewItem()
        {
            IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource()
            {
                Symbol = Symbol.Placeholder
            },
            Header = header,
            Content = new TileMapEditorControl()
            {
                DataContext = dataContext
            }
        };

        return newTab;
    }

    // This method prevents the TabView from handling things that aren't text (ie. files, images, etc.)
    private void Tabs_TabStripDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey(DataIdentifier))
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }
    }

    private async void Tabs_AddTabButtonClick(TabView sender, object args)
    {
        var dialog = new FileOpenPicker();

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowHelper.GetWindowForElement(this) ?? App.Current.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(dialog, hWnd);

        dialog.FileTypeFilter.Add(".png");
        var result = await dialog.PickSingleFileAsync();
        if (result is null)
        {
            return;
        }
        var vm = new TileMapEditorViewmodel(new Windows.Graphics.SizeInt32(32, 32))
        {
            Width = 32,
            Heigeht = 32,
            TileSets = ImmutableList.Create(new TileSetModel() { Path = result.Path, TileSize = new Windows.Graphics.SizeInt32(32, 32) })
        };
        var tab = CreateNewTVI("New Item", vm);
        sender.TabItems.Add(tab);
    }

    private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
    }

}
