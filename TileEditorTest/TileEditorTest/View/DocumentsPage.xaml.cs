using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.Model;
using TileEditorTest.View;
using TileEditorTest.View.Dialogs;
using TileEditorTest.ViewModel;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;

using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DocumentsPage : Page {
    private const string DataIdentifier = "TileDocumentTab";

    public ProjectViewModel ProjectViewModel { get; set; }

    [Notify(Getter.Internal)]
    private ViewLoader? selectedViewLoader;

    [Notify(Getter.Internal)]
    private bool indentMenu;

    public DocumentsPage() {
        this.InitializeComponent();
    }


    internal void AddAndFocusTab(ViewLoader loader) {
        Tabs.TabItems.Add(loader);
        Tabs.SelectedIndex = Tabs.TabItems.Count - 1;
    }



    protected override void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);
    }

    private void AddTabToTabs(TabViewItem tab) {
        Tabs.TabItems.Add(tab);
    }

    // Create a new Window once the Tab is dragged outside.
    private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args) {
        var newPage = new DocumentsPage() { ProjectViewModel = this.ProjectViewModel };

        Tabs.TabItems.Remove(args.Tab);
        newPage.Tabs.TabItems.Add(args.Tab);

        var newWindow = App.CreateWindow(newPage);

        newWindow.Activate();
    }

    private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args) {
        // We can only drag one tab at a time, so grab the first one...
        var firstItem = args.Tab;

        // ... set the drag data to the tab...
        args.Data.Properties.Add(DataIdentifier, firstItem);

        // ... and indicate that we can move it
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void Tabs_TabStripDrop(object sender, DragEventArgs e) {
        // This event is called when we're dragging between different TabViews
        // It is responsible for handling the drop of the item into the second TabView

        if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj)) {
            // Ensure that the obj property is set before continuing.
            if (obj == null) {
                return;
            }

            var destinationTabView = (TabView)sender;
            var destinationItems = destinationTabView.TabItems;

            if (destinationItems != null) {
                // First we need to get the position in the List to drop to
                var index = -1;

                // Determine which items in the list our pointer is between.
                for (int i = 0; i < destinationTabView.TabItems.Count; i++) {
                    var item = (TabViewItem)destinationTabView.ContainerFromIndex(i);

                    if (e.GetPosition(item).X - item.ActualWidth < 0) {
                        index = i;
                        break;
                    }
                }



                var element = (ViewLoader)obj;

                // AppWindows should run on the same UI Thread
                System.Diagnostics.Debug.Assert(Object.ReferenceEquals(element.DispatcherQueue, this.DispatcherQueue));

                var destinationTabViewListView = (TabViewListView)element.Parent;
                destinationTabViewListView.Items.Remove(obj);



                if (index < 0) {
                    // We didn't find a transition point, so we're at the end of the list
                    destinationItems.Add(element);
                } else if (index < destinationTabView.TabItems.Count) {
                    // Otherwise, insert at the provided index.
                    destinationItems.Insert(index, element);
                }

                // Select the newly dragged tab
                destinationTabView.SelectedItem = element;
            }
        }
    }


    // This method prevents the TabView from handling things that aren't text (ie. files, images, etc.)
    private void Tabs_TabStripDragOver(object sender, DragEventArgs e) {
        if (e.DataView.Properties.ContainsKey(DataIdentifier)) {
            e.AcceptedOperation = DataPackageOperation.Move;
        }
    }


    internal void AddTab(ProjectItem item) {
        App.CreateTab(this, item);
    }


    private async void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) {
        await App.DestroyTab((ViewLoader)args.Tab);
        if (Tabs.TabItems.Count == 0) {
            Window? window = App.GetWindowForElement(this);
            if (window != App.Current.MainWindow) {
                window?.Close();
            }
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        SelectedViewLoader = (ViewLoader?)Tabs.SelectedItem;
    }

    private void TabbedCommandBar_SizeChanged(object sender, SizeChangedEventArgs e) {
        IEnumerable<double> list = commandBar.MenuItems.OfType<TabbedCommandBarItem>()
                    .Select(GetRight)
                    .Append(GetRight((FrameworkElement)commandBar.PaneHeader))
                    .Append(1)
                    .ToArray();
        var mostRightPoint = list
            .Max();

        //var newWidth = this.ActualWidth - mostRightPoint;

        //this.AppTitleBar.Width = newWidth;

        double GetRight(FrameworkElement x) {


            if (x.Parent is not UIElement parent) {
                return 0;
            }

            GeneralTransform generalTransform = parent.TransformToVisual(this);
            //var visual = x.GetVisual();
            var visual = x;
            Point input = new Point(visual.ActualSize.X + visual.ActualOffset.X, 0);
            Point reslut = generalTransform.TransformPoint(input);
            
            return reslut.X;
        }
    }
}
