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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.View.Dialogs;
using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View;
public sealed partial class MainView : UserControl, INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    [Notify]
    private bool isMenuCompact;

    private ProjectViewModel? projectViewModel;
    internal ProjectViewModel? ProjectViewModel {
        get => projectViewModel;
        set {
            if (projectViewModel != value) {
                if (projectViewModel != null) {
                    projectViewModel.OnShowNewFileDialog -= ProjectViewModel_ShowNewFileDialog;
                    projectViewModel.OnOpenFile -= ProjectViewModel_OnOpenFile;
                }
                projectViewModel = value;
                if (projectViewModel != null) {
                    projectViewModel.OnOpenFile += ProjectViewModel_OnOpenFile;
                    projectViewModel.OnShowNewFileDialog += ProjectViewModel_ShowNewFileDialog;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectViewModel)));
            }
        }
    }

    private System.Threading.Tasks.Task ProjectViewModel_OnOpenFile(Model.ProjectItem arg) {
        documents.AddTab(arg);
        return Task.CompletedTask;
    }

    private System.Threading.Tasks.Task<string?> ProjectViewModel_ShowNewFileDialog(ProjectItemType arg, Predicate<string>? isAllowed = null) {
        return NewFileDialogContent.ShowDialog(this.XamlRoot, isAllowed);
    }

    public MainView() {
        this.InitializeComponent();
    }

    private async void AppBarButton_Click(object sender, RoutedEventArgs e) {
        var dialog = new FileOpenPicker();

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.GetWindowForElement(this) ?? App.Current.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(dialog, hWnd);

        dialog.FileTypeFilter.Add(".json");
        var result = await dialog.PickSingleFileAsync();
        if (result is null) {
            return;
        }
        var folder = await result.GetParentAsync();
        var vm = await ProjectViewModel.Load(result);
        Environment.CurrentDirectory = folder.Path;
        this.ProjectViewModel = vm;

    }

    private void NavigationView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args) {
        this.IsMenuCompact = args.DisplayMode == NavigationViewDisplayMode.Minimal;
    }
}
