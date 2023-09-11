using Microsoft.UI.Xaml;
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

    public ReadOnlyObservableCollection<TerranViewModel> Terrains { get; }
    private ObservableCollection<TerranViewModel> terrains = new();


    public ICommand AddCommand { get; }

    internal ProjectItem<TerrainsFile> Item { get; }

    public TerrainsViewModel(TerrainsFile content, ProjectItem<TerrainsFile> item, CoreViewModel coreViewModel) : base(coreViewModel) {
        this.Terrains = new(terrains);
        this.model = content;
        this.Item = item;


        XamlUICommand addNew = new();
        addNew.ExecuteRequested += (sender, e) => {
            this.terrains.Add(new(coreViewModel, Guid.NewGuid(), item.Path));
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
    private Dictionary<Guid, TerranViewModel> loadedModels = new();
    public override Task RestoreValuesFromModel() {

        var vmTarreans = terrains.Select(ToModelTerrain);

        var newTerrains = model.Terrains.Except(vmTarreans, Terrain.IdEqualityComparer);
        var removeTerrains = vmTarreans.Except(model.Terrains, Terrain.IdEqualityComparer);
        var revertChanges = model.Terrains.Intersect(vmTarreans, Terrain.IdEqualityComparer);

        foreach (var terrain in removeTerrains) {
            var toRemove = terrains.FirstOrDefault(x => ToModelTerrain(x) == terrain);
            if (toRemove != null) {
                terrains.Remove(toRemove);
            }
        }
        foreach (var terrain in newTerrains) {
            if (!loadedModels.TryGetValue(terrain.FileLoadGuid, out var oldModel)) {
                oldModel = new(this.CoreViewModel, terrain.FileLoadGuid, Item.Path);
                loadedModels.Add(terrain.FileLoadGuid, oldModel);
            }

            var index = Array.IndexOf(model.Terrains, terrain);
            this.terrains.Insert(index, oldModel);
        }
        foreach (var toRevert in revertChanges.Concat(newTerrains)) {
            var terrainViewModel = terrains.First(x => x.Id == toRevert.FileLoadGuid);
            
            if (toRevert.Wall is not null) {
                terrainViewModel.HasWall = true;
                terrainViewModel.Wall!.FillTransparency = toRevert.Wall.Opacity / 100.0;
            } else {
                terrainViewModel.HasWall = false;
            }
            if (toRevert.Floor is not null) {
                terrainViewModel.HasFloor = true;
                terrainViewModel.Floor!.FillTransparency = toRevert.Floor.Opacity / 100.0;
            } else {
                terrainViewModel.HasFloor = false;
            }
            if (toRevert.Cut is not null) {
                terrainViewModel.HasCut = true;
                terrainViewModel.Cut!.FillTransparency = toRevert.Cut.Opacity / 100.0;
            } else {
                terrainViewModel.HasCut = false;
            }

            terrainViewModel.Name = toRevert.Name;
            terrainViewModel.Color = toRevert.Color;
            if (toRevert.Image is not null) {
                terrainViewModel.ImageSelectorViewModel.SelectedTileSet = toRevert.Image.TileSetPath;
                terrainViewModel.ImageSelectorViewModel.X = toRevert.Image.x;
                terrainViewModel.ImageSelectorViewModel.Y = toRevert.Image.y;
            } else {
                terrainViewModel.ImageSelectorViewModel.SelectedTileSet = null;
                terrainViewModel.ImageSelectorViewModel.X = 0;
                terrainViewModel.ImageSelectorViewModel.Y = 0;
            }
        }

        UpdateIndexForTerrains();

        //// TODO: only handel changes....
        //terrains.Clear();
        //foreach (var terrain in model.Terrains) {
        //}
        CheckHasChanges();
        return Task.CompletedTask;
    }

    private void UpdateIndexForTerrains() {
        for (int i = 0; i < this.terrains.Count; i++) {
            terrains[i].Index = i;
        }
    }

    private void TerrainItemChanged(object? sender, PropertyChangedEventArgs e) {
        CheckHasChanges();
    }

    private void CheckHasChanges() {
        HasChanges = model.Terrains.Length != this.Terrains.Count
            || !this.Terrains.Select(ToModelTerrain).SequenceEqual(model.Terrains)
            || this.terrains.Select((x, i) => (x, i)).Any((x) => x.x.Index != x.i);
        ;
    }

    private static Terrain ToModelTerrain(TerranViewModel x) {
        Terrain terrain = new Terrain(x.Name, x.ImageSelectorViewModel.SelectedTileSet is not null ? new TileImage(x.ImageSelectorViewModel.SelectedTileSet, x.ImageSelectorViewModel.X, x.ImageSelectorViewModel.Y) : null,
            x.Color,
            x.Floor is null ? null : new((int)(x.Floor.FillTransparency * 100)),
            x.Wall is null ? null : new((int)(x.Wall.FillTransparency * 100)),
            x.Cut is null ? null : new((int)(x.Cut.FillTransparency * 100))
            ) { FileLoadGuid = x.Id };
        return terrain;
    }

    protected override async Task SaveValuesToModel() {
        this.model.Terrains = this.Terrains.Select(ToModelTerrain).ToArray();
        await this.model.Save(this.Item.Path, this.CoreViewModel);
        UpdateIndexForTerrains();
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
