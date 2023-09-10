using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI.UI.Controls;

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
using System.Threading.Tasks;

using TileEditorTest.Model;
using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;

using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Editors;

public sealed partial class TileSetEditor : UserControl, IView<TileSetFile, TileSetViewModel, TileSetEditor>, IDisposable {
    private TileSetEditor(TileSetViewModel viewModel) {
        this.ViewModel = viewModel;
        var terrainsFiles = this.ViewModel.CoreViewModel.GetProjectItemCollectionOfType<TerrainsFile>();
        Terrains = terrainsFiles.ToGrouping().WithKey(x => x.Path).WithSubCollection(async x => {
            var disposable = App.GetViewModel<TerrainsFile>(x, viewModel.CoreViewModel, true).Of<TerrainsViewModel>(out var vmTask);
            var vm = await vmTask;
            return vm.Terrains;
        });
        this.InitializeComponent();
    }

    public ReadOnlyObservableGroupe<ProjectPath, TerranViewModel> Terrains { get; }
    internal TileSetViewModel ViewModel { get; }

    TileSetViewModel IView<TileSetFile, TileSetViewModel, TileSetEditor>.ViewModel => this.ViewModel;

    static TileSetEditor IView<TileSetFile, TileSetViewModel, TileSetEditor>.Create(TileSetViewModel viewModel) {
        return new TileSetEditor(viewModel);
    }

    public void Dispose() {
        Terrains.Dispose();
    }

    private TerranViewModel? ConvertTerrainViewModel(object obj) {
        TerranViewModel? terrainViewModel = obj as TerranViewModel ?? grid.SelectedColor;
        if (terrainViewModel != terrainsSelection.SelectedItem) {
            terrainsSelection.SelectedItem = terrainViewModel;
        }
        return terrainViewModel;
    }
}
