using Microsoft.UI.Xaml.Input;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using TileEditorTest.Model;

using Windows.UI;

namespace TileEditorTest.ViewModel;
internal sealed partial class TerrainsViewModel : ViewModel<TerrainsFile, TerrainsViewModel>, IViewModel<TerrainsFile, TerrainsViewModel>, IAsyncDisposable {
    private TerrainsFile model;
    private ProjectItem<TerrainsFile> item;

    public ReadOnlyObservableCollection<TerranViewModel> Terrains { get; }
    private ObservableCollection<TerranViewModel> terrains = new();


    public ICommand AddCommand { get; }

    public TerrainsViewModel(TerrainsFile content, ProjectItem<TerrainsFile> item, CoreViewModel coreViewModel) : base(coreViewModel) {
        this.Terrains = new(terrains);
        this.model = content;
        this.item = item;


        XamlUICommand addNew = new();
        addNew.ExecuteRequested += (sender, e) => {
            this.terrains.Add(new(coreViewModel) { Color = Color.FromArgb(255, 255, 255, 255) });
        };
        this.AddCommand = addNew;


        this.terrains.CollectionChanged += async (sender, e) => {
            foreach (var item1 in e.NewItems?.OfType<TerranViewModel>() ?? Enumerable.Empty<TerranViewModel>()) {
                item1.PropertyChanged += TerrainItemChanged;
                item1.ImageSelectorViewModel.PropertyChanged += TerrainItemChanged;
            }
            foreach (var item1 in e.OldItems?.OfType<TerranViewModel>() ?? Enumerable.Empty<TerranViewModel>()) {
                item1.PropertyChanged -= TerrainItemChanged;
                item1.ImageSelectorViewModel.PropertyChanged -= TerrainItemChanged;
                await item1.DisposeAsync();
            }

            CheckHasChanges();
        };
        RestoreValuesFromModel();
    }

    public override Task RestoreValuesFromModel() {
        // TODO: only handel changes....
        terrains.Clear();
        foreach (var terrain in model.Terrains) {
            TerranViewModel terrainViewModel = new(this.CoreViewModel) {
                Color = terrain.Color,
                FillTransparency = terrain.Opacity / 100.0,
                Name = terrain.Name,
                Type = terrain.Type
            };
            if (terrain.Image is not null) {
                terrainViewModel.ImageSelectorViewModel.SelectedTileSet = terrain.Image.TileSetPath;
                terrainViewModel.ImageSelectorViewModel.X = terrain.Image.x;
                terrainViewModel.ImageSelectorViewModel.Y = terrain.Image.y;
            }
            this.terrains.Add(terrainViewModel);
        }
        CheckHasChanges();
        return Task.CompletedTask;
    }

    private void TerrainItemChanged(object? sender, PropertyChangedEventArgs e) {
        CheckHasChanges();
    }

    private void CheckHasChanges() {
        HasChanges = model.Terrains.Length != this.Terrains.Count
            || !this.Terrains.Select(x => new Terrain(x.Name, x.Type, x.Color, (int)(x.FillTransparency * 100), x.ImageSelectorViewModel.SelectedTileSet is not null ? new TileImage(x.ImageSelectorViewModel.SelectedTileSet, x.ImageSelectorViewModel.X, x.ImageSelectorViewModel.Y) : null)).SequenceEqual(model.Terrains);
        ;
    }
    protected override async Task SaveValuesToModel() {
        this.model.Terrains = this.Terrains.Select(x => new Terrain(x.Name, x.Type, x.Color, (int)(x.FillTransparency * 100), x.ImageSelectorViewModel.SelectedTileSet is not null ? new TileImage(x.ImageSelectorViewModel.SelectedTileSet, x.ImageSelectorViewModel.X, x.ImageSelectorViewModel.Y) : null)).ToArray();
        await this.model.Save(this.item.Path, this.CoreViewModel);
        CheckHasChanges();
    }



    public static async Task<TerrainsViewModel> Create(ProjectItem<TerrainsFile> file, CoreViewModel project) {
        var content = await file.Content;
        TerrainsViewModel result = new(content, file, project);
        return result;
    }

    public async ValueTask DisposeAsync() {
        foreach (var item1 in Terrains) {
            await item1.DisposeAsync();
        }
        terrains.Clear();// technically this calls dispose again which can't be awaited, but this is handled on implementation side, you can call dispose multiple times.
    }
}
