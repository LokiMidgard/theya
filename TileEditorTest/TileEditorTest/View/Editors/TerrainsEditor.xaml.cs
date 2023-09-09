using CommunityToolkit.WinUI.UI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using TileEditorTest.Model;
using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Editors;
internal sealed partial class TerrainsEditor : UserControl, IView<TerrainsFile, TerrainsViewModel, TerrainsEditor> {
    public TerrainsEditor(TerrainsViewModel viewModel) {
        this.InitializeComponent();
        ViewModel = viewModel;
    }

    internal TerrainsViewModel ViewModel { get; }

    TerrainsViewModel IView<TerrainsFile, TerrainsViewModel, TerrainsEditor>.ViewModel => this.ViewModel;

    static TerrainsEditor IView<TerrainsFile, TerrainsViewModel, TerrainsEditor>.Create(TerrainsViewModel viewModel) {
        return new(viewModel);
    }

    private async void Button_Tapped(object sender, TappedRoutedEventArgs e) {
        Button button = (Button)sender;
        StackPanel stackPanel = (StackPanel)button.Parent;
        await stackPanel.Children.OfType<ContentDialog>().Single().ShowAsync();
    }
}
