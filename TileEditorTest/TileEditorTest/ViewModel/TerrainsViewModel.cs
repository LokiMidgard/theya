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
internal partial class TerrainsViewModel : IViewModel<TerrainsFile, TerrainsViewModel> {
    private TerrainsFile model;
    private ProjectItem<TerrainsFile> item;

    public TerrainsViewModel(TerrainsFile content, ProjectItem<TerrainsFile> item, CoreViewModel coreViewModel) {
        this.model = content;
        this.item = item;
        this.Project = coreViewModel;

        StandardUICommand save = new(StandardUICommandKind.Save);
        save.ExecuteRequested += async (sender, e) => await SaveValuesToModel();
        save.CanExecuteRequested += (sender, e) => e.CanExecute = HasChanges;
        this.Save = save;

        XamlUICommand addNew = new();
        addNew.ExecuteRequested += (sender, e) => {
            this.Terrans.Add(new() { Color = Color.FromArgb(255, 255, 255, 255) });
        };
        this.AddCommand = addNew;


        this.Terrans.CollectionChanged += (sender, e) => {
            foreach (var item1 in e.NewItems?.OfType<TerranViewModel>() ?? Enumerable.Empty<TerranViewModel>()) {
                item1.PropertyChanged += TerrainItemChanged;
            }
            foreach (var item1 in e.OldItems?.OfType<TerranViewModel>() ?? Enumerable.Empty<TerranViewModel>()) {
                item1.PropertyChanged -= TerrainItemChanged;
            }

            CheckHasChanges();
        };

        foreach (var terrain in model.Terrains) {
            this.Terrans.Add(new() {
                Color = terrain.Color,
                FillTransparency = terrain.Opacity,
                Name = terrain.Name,
                Type = terrain.Type
            });
        }
        CheckHasChanges();



    }

    private void TerrainItemChanged(object? sender, PropertyChangedEventArgs e) {
        CheckHasChanges();
    }

    private void CheckHasChanges() {
        HasChanges = model.Terrains.Length != this.Terrans.Count
            || !this.Terrans.Select(x => new Terrain(x.Name, x.Type, x.Color, x.FillTransparency)).SequenceEqual(model.Terrains);
        ;
    }
    private async Task SaveValuesToModel() {
        this.model.Terrains = this.Terrans.Select(x => new Terrain(x.Name, x.Type, x.Color, x.FillTransparency)).ToArray();
        await this.model.Save(this.item.Path, this.Project);
        CheckHasChanges();
    }

    public ObservableCollection<TerranViewModel> Terrans { get; } = new();

    public CoreViewModel Project { get; }

    public ICommand Save { get; }
    public ICommand AddCommand { get; }

    [Notify]
    private bool hasChanges;
    private void OnHasChangesChanged() {
        ((StandardUICommand)Save).NotifyCanExecuteChanged();
    }


    public static async Task<TerrainsViewModel> Create(ProjectItem<TerrainsFile> file, CoreViewModel project) {
        var content = await file.Content;
        TerrainsViewModel result = new(content, file, project);
        return result;
    }
}
