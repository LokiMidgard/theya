using InterfaceGenerator;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

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
using TileEditorTest.View;
using TileEditorTest.ViewModel.Controls;

using Windows.System.Threading;
using Windows.UI;

namespace TileEditorTest.ViewModel;

[GenerateAutoInterface]
public abstract partial class ViewModel : IViewModel {
    public CoreViewModel CoreViewModel { get; }

    public ICommand Save { get; }

    [Notify(global::PropertyChanged.SourceGenerator.Setter.Protected)]
    private bool hasChanges;

    private void OnHasChangesChanged() {
        ((StandardUICommand)Save).NotifyCanExecuteChanged();
    }

    public ViewModel(CoreViewModel project) {
        StandardUICommand save = new(StandardUICommandKind.Save);
        save.ExecuteRequested += async (sender, e) => await SaveValuesToModel();
        save.CanExecuteRequested += (sender, e) => e.CanExecute = HasChanges;
        this.Save = save;
        CoreViewModel = project;
    }

    protected abstract Task SaveValuesToModel();
    public abstract Task RestoreValuesFromModel();
}

public abstract partial class ViewModel<OfFile, VM> : ViewModel
    where OfFile : class, IProjectItemContent<OfFile>
    where VM : ViewModel<OfFile, VM>, IViewModel<OfFile, VM> {
    protected ViewModel(CoreViewModel project) : base(project) {
    }
}

public partial interface IViewModel : INotifyPropertyChanged {
    public bool HasChanges { get; }
}

public interface IViewModel<OfFile, ViewModel> : IViewModel
    where OfFile : class, IProjectItemContent<OfFile>
    where ViewModel : IViewModel<OfFile, ViewModel> {
    public static abstract Task<ViewModel> Create(ProjectItem<OfFile> file, CoreViewModel project);
}
public interface IView {
    public IViewModel ViewModel { get; }
}
public interface IView<OfFile, VM, View> : IView
where OfFile : class, IProjectItemContent<OfFile>
where VM : IViewModel<OfFile, VM>
where View : Control, IView<OfFile, VM, View> {

    public new VM ViewModel { get; }
    IViewModel IView.ViewModel => this.ViewModel;
    public static abstract View Create(VM viewModel);

}


public partial class TileViewModel {

    [Notify]
    private TerranViewModel? leftTop;
    [Notify]
    private TerranViewModel? top;
    [Notify]
    private TerranViewModel? rightTop;
    [Notify]
    private TerranViewModel? left;
    [Notify]
    private TerranViewModel? center;
    [Notify]
    private TerranViewModel? right;
    [Notify]
    private TerranViewModel? leftBottom;
    [Notify]
    private TerranViewModel? bottom;
    [Notify]
    private TerranViewModel? rightBottom;

    private GridSelection grid;
    public ref GridSelection Grid => ref grid;

    private void OnAnyPropertyChanged(string propertyName) {
        if (propertyName == nameof(LeftTop)) {
            Points[0] = leftTop;
        } else if (propertyName == nameof(Top)) {
            Points[1] = top;
        } else if (propertyName == nameof(RightTop)) {
            Points[2] = rightTop;
        } else if (propertyName == nameof(Left)) {
            Points[3] = left;
        } else if (propertyName == nameof(Center)) {
            Points[4] = center;
        } else if (propertyName == nameof(Right)) {
            Points[5] = right;
        } else if (propertyName == nameof(LeftBottom)) {
            Points[6] = leftBottom;
        } else if (propertyName == nameof(Bottom)) {
            Points[7] = bottom;
        } else if (propertyName == nameof(RightBottom)) {
            Points[8] = rightBottom;
        }
    }

    public readonly struct GridSelection {
        private readonly TileViewModel tile;
        public GridSelection(TileViewModel tile) {
            this.tile = tile ?? throw new ArgumentNullException(nameof(tile));
        }
        public readonly TerranViewModel? this[int x, int y] {
            get => tile.Points[x + y * 3];
            set => tile.Points[x + y * 3] = value;
        }
    }

    public ObservableCollection<TerranViewModel?> Points { get; }
    public int X { get; }
    public int Y { get; }

    public TileViewModel(int x, int y) {
        Points = new() {
        leftTop,
        top,
        rightTop,
        left,
        center,
        right,
        leftBottom,
        bottom,
        rightBottom
        };
        Points.CollectionChanged += Points_CollectionChanged;
        this.grid = new(this);
        X = x;
        Y = y;
    }

    private void Points_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        switch (e.NewStartingIndex) {
            case 0: LeftTop = Points[e.NewStartingIndex]; break;
            case 1: Top = Points[e.NewStartingIndex]; break;
            case 2: RightTop = Points[e.NewStartingIndex]; break;
            case 3: Left = Points[e.NewStartingIndex]; break;
            case 4: Center = Points[e.NewStartingIndex]; break;
            case 5: Right = Points[e.NewStartingIndex]; break;
            case 6: LeftBottom = Points[e.NewStartingIndex]; break;
            case 7: Bottom = Points[e.NewStartingIndex]; break;
            case 8: RightBottom = Points[e.NewStartingIndex]; break;
            default:
                break;
        }

    }
}
public enum TerranType {
    Floor,
    Wall,
    Cut
}
public sealed partial class TerranViewModel : IAsyncDisposable {
    [Notify]
    private Color color;
    [Notify]
    private double fillTransparency = 0.5;
    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private Color stroke;
    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private Color fill;
    [Notify]
    private string name = "";
    [Notify]
    private TerranType type;

    internal TileImageSelectorViewModel ImageSelectorViewModel { get; }

    public TerranViewModel(CoreViewModel core) {
        ImageSelectorViewModel = new(core);
    }

    private void OnFillTransparencyChanged() {
        OnColorChanged();
    }
    private void OnColorChanged() {
        this.Stroke = color;
        this.Fill = Color.FromArgb((byte)(255 * fillTransparency), color.R, color.G, color.B);
    }

    public ValueTask DisposeAsync() {
        return ImageSelectorViewModel.DisposeAsync();
    }
}

public partial class TileSetViewModel : ViewModel<TileSetFile, TileSetViewModel>, IViewModel<TileSetFile, TileSetViewModel> {




    [Notify]
    private int tileHeight;

    [Notify]
    private int tileWidth;

    [Notify]
    private int columns;

    [Notify]
    private int rows;

    [Notify]
    private TileViewModel[] tileModel;


    [Notify]
    private ProjectItem<ImageFile>? selectedImage;

    private async void OnSelectedImageChanged() {
        await LoadTileModels();
    }
    private async void OnTileWidthChanged() {
        await LoadTileModels();
    }
    private async void OnTileHeightChanged() {
        await LoadTileModels();
    }

    private async Task LoadTileModels() {
        ClearTileModels();
        if (SelectedImage is null) {
            return;
        }
        BitmapImage image = new() {
            UriSource = new Uri(SelectedImage.Path.SystemPath(CoreViewModel)),
        };
        this.ImageSource = image;
        var content = await SelectedImage.Content;
        //TODO: Refresh on tilesize change
        Columns = (int)content.Width / (TileWidth == 0 ? (int)content.Width : tileWidth);
        Rows = (int)content.Height / (TileHeight == 0 ? (int)content.Height : TileHeight);
        var tileModel = new TileViewModel[Rows * Columns];
        for (global::System.Int32 i = 0; i < tileModel.Length; i++) {
            tileModel[i] = new(i % columns, i / columns);
            tileModel[i].PropertyChanged += TileModelChanged;
        }

        TileModel = tileModel;
    }

    private void ClearTileModels() {
        foreach (var item in tileModel) {
            item.PropertyChanged -= TileModelChanged;
        }
        this.ImageSource = null;
        TileModel = Array.Empty<TileViewModel>();
    }

    private void TileModelChanged(object? sender, PropertyChangedEventArgs e) {
        this.UpdateHasChanges();

    }

    [Notify]
    private ImageSource? imageSource;


    private void OnAnyPropertyChanged(string propertyName) {
        if (propertyName is not nameof(HasChanges) or nameof(ImageSource)) {
            this.UpdateHasChanges();
        }
    }



    private readonly TileSetFile tileSet;
    private readonly ProjectItem<TileSetFile> projectItem;

    public TileSetViewModel(TileSetFile tileSet, ProjectItem<TileSetFile> projectItem, CoreViewModel project, ObservableCollection<ProjectItem<ImageFile>> allImages) : base(project) {
        this.tileSet = tileSet;
        this.projectItem = projectItem;
        this.AllImages = new ReadOnlyObservableCollection<ProjectItem<ImageFile>>(allImages);
        this.tileModel = Array.Empty<TileViewModel>();
        RestoreValuesFromModel();
    }

    public override Task RestoreValuesFromModel() {
        this.SelectedImage = tileSet.Image;
        this.TileWidth = tileSet.TileSize?.Width ?? 32;
        this.TileHeight = tileSet.TileSize?.Height ?? 32;
        UpdateHasChanges();
        return Task.CompletedTask;
    }


    protected override Task SaveValuesToModel() {
        tileSet.Image = SelectedImage;
        tileSet.TileSize = new() { Width = TileWidth, Height = TileHeight };
        UpdateHasChanges();
        return tileSet.Save(projectItem.Path, CoreViewModel);
    }

    private void UpdateHasChanges() {
        this.HasChanges = this.SelectedImage != tileSet.Image ||
        this.TileWidth != (tileSet.TileSize?.Width ?? 32) ||
        this.TileHeight != (tileSet.TileSize?.Height ?? 32);

        //this.TileModel.Length != tileSet.TileData.Length || TileModel.Select(x=> new TileData(new TerrainIdData(x.LeftTop, x.Top, x.RightTop, x.LeftTop, x.Center, x.Right, x.LeftBottom, x.Bottom, x.RightBottom.))

    }


    public ReadOnlyObservableCollection<ProjectItem<ImageFile>> AllImages { get; }



    public static async Task<TileSetViewModel> Create(ProjectItem<TileSetFile> projectItem, CoreViewModel project) {
        var tileSet = await projectItem.Content;
        var files = project.GetProjectPathCollectionOfType<ImageFile>();
        ObservableCollection<ProjectItem<ImageFile>> allImages = new();
        foreach (var file in files) {
            var imageItems = project.GetProjectItem<ImageFile>(file);
            if (imageItems is not null) {
                allImages.Add(imageItems);
            }
        }

        return new TileSetViewModel(tileSet, projectItem, project, allImages);
    }


}
