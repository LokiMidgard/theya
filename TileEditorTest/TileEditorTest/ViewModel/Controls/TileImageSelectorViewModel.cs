using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TileEditorTest.Model;

namespace TileEditorTest.ViewModel.Controls;
public partial class TileImageSelectorViewModel : IAsyncDisposable {
    [Notify]
    private int x;
    [Notify]
    private int y;
    [Notify]
    private ProjectPath? selectedTileSet;

    [Notify]
    private TileSetViewModel? selectedViewModel;
    private IAsyncDisposable? tileSetDisposable;



    public ReadOnlyObservableCollection<ProjectPath> TileSets { get; }

    private readonly CoreViewModel core;

    public TileImageSelectorViewModel(CoreViewModel core) {
        TileSets = core.GetProjectItemCollectionOfType<TileSetFile>();
        this.core = core;
    }

    private async void OnSelectedTileSetChanged() {
        if (this.tileSetDisposable is not null) {
            await this.tileSetDisposable.DisposeAsync();
        }
        this.SelectedViewModel = null;
        if (selectedTileSet is not null && core.GetProjectItem<TileSetFile>(selectedTileSet.Value) is ProjectItem<TileSetFile> item) {
            this.tileSetDisposable = App.GetViewModel(item, core, true).Of<TileSetViewModel>(out var vmTask);
            this.SelectedViewModel = await vmTask;
        }
    }

    public ValueTask DisposeAsync() {
        return tileSetDisposable is not null
            ? tileSetDisposable.DisposeAsync()
            : ValueTask.CompletedTask;
    }
}
