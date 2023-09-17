
using InterfaceGenerator;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI;
using Microsoft.UI.Composition;
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
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using TileEditorTest.Model;
using TileEditorTest.View;
using TileEditorTest.View.Controls;
using TileEditorTest.ViewModel.Controls;

using Windows.UI;

using static TileEditorTest.ViewModel.TerranFormViewModel;

using Window = Microsoft.UI.Xaml.Window;

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
    private TerranFormViewModel? leftTop;
    [Notify]
    private TerranFormViewModel? top;
    [Notify]
    private TerranFormViewModel? rightTop;
    [Notify]
    private TerranFormViewModel? left;
    [Notify]
    private TerranFormViewModel? center;
    [Notify]
    private TerranFormViewModel? right;
    [Notify]
    private TerranFormViewModel? leftBottom;
    [Notify]
    private TerranFormViewModel? bottom;
    [Notify]
    private TerranFormViewModel? rightBottom;

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
        public readonly TerranFormViewModel? this[int x, int y] {
            get => tile.Points[x + y * 3];
            set => tile.Points[x + y * 3] = value;
        }
    }

    public ObservableCollection<TerranFormViewModel?> Points { get; }
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
    private string name = "";
    [Notify]
    private Color color = Color.FromArgb(255, 255, 255, 255);

    private readonly TerranFormViewModel floor;
    private readonly TerranFormViewModel wall;
    private readonly TerranFormViewModel cut;
    public TerranFormViewModel? Floor => HasFloor ? floor : null;
    public TerranFormViewModel? Wall => HasWall ? wall : null;
    public TerranFormViewModel? Cut => HasCut ? cut : null;

    [Notify]
    private int index;

    [Notify]
    private bool hasFloor;
    [Notify]
    private bool hasWall;
    [Notify]
    private bool hasCut;

    public ProjectPath TerrainFile { get; }

    internal TileImageSelectorViewModel ImageSelectorViewModel { get; }
    public Guid Id { get; }


    public TerranViewModel(CoreViewModel core, Guid id, ProjectPath terrainFile) {
        ImageSelectorViewModel = new(core);
        Id = id;
        TerrainFile = terrainFile;
        this.floor = new(TerranType.Floor, this);
        this.wall = new(TerranType.Wall, this);
        this.cut = new(TerranType.Cut, this);

        this.floor.PropertyChanged += (sender, e) => OnPropertyChanged(new PropertyChangedEventArgs($"{nameof(Floor)}.{e.PropertyName}"));
        this.wall.PropertyChanged += (sender, e) => OnPropertyChanged(new PropertyChangedEventArgs($"{nameof(Wall)}.{e.PropertyName}"));
        this.cut.PropertyChanged += (sender, e) => OnPropertyChanged(new PropertyChangedEventArgs($"{nameof(Cut)}.{e.PropertyName}"));

    }

    public ValueTask DisposeAsync() {
        return ImageSelectorViewModel.DisposeAsync();
    }
}

public enum FillType {
    Solid,
    Doted,
    Lines
}


public sealed partial class TerranFormViewModel {
    [Notify]
    private FillType fillType = FillType.Solid;
    [Notify]
    private double fillTransparency = 0.5;
    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private Color stroke;
    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private Brush fill;

    [Notify(global::PropertyChanged.SourceGenerator.Setter.Private)]
    private bool isEnabled;

    public DotBrushViewModel DotBrushConfiguration { get; } = new();
    public LineBrushViewModel LineBrushConfiguration { get; } = new();

    public partial class DotBrushViewModel {
        [Notify]
        private double radius = 8;
        [Notify]
        private double distance = 4;
    }
    public partial class LineBrushViewModel {


        [Notify]
        private double thickness = 4;
        [Notify]
        private double distance = 4;
        [Notify]
        private double angle = 4;
    }

    public TerranType Type { get; }
    public TerranViewModel Parent { get; }

    public TerranFormViewModel(TerranType type, TerranViewModel parent) {
        Type = type;
        Parent = parent;
        parent.PropertyChanged += Parent_PropertyChanged;
        OnColorChanged();
        this.DotBrushConfiguration.PropertyChanged += (sender, e) => {
            if (this.Fill is DotBrush dotBrush) {
                OnColorChanged();
            }
        };
        this.LineBrushConfiguration.PropertyChanged += (sender, e) => {
            if (this.Fill is LineBrush dotBrush) {
                OnColorChanged();
            }
        };

    }

    private void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Parent.Color)) {
            OnColorChanged();
        }
        if (this.Type == TerranType.Cut && e.PropertyName == nameof(Parent.HasCut)
|| this.Type == TerranType.Floor && e.PropertyName == nameof(Parent.HasFloor)
|| this.Type == TerranType.Wall && e.PropertyName == nameof(Parent.HasWall)
            ) {
            this.IsEnabled = this.Type switch {
                TerranType.Cut => Parent.HasCut,
                TerranType.Floor => Parent.HasFloor,
                TerranType.Wall => Parent.HasWall,
                _ => false
            };
        }
    }

    private void OnFillTransparencyChanged() {
        OnColorChanged();
    }
    private void OnFillTypeChanged() {
        OnColorChanged();
    }

    private void OnColorChanged() {
        var color = Parent.Color;
        this.Stroke = color;
        Color fillColor = Color.FromArgb((byte)(255 * fillTransparency), color.R, color.G, color.B);
        this.Fill = this.FillType switch {
            FillType.Solid => new SolidColorBrush() { Color = fillColor },
            FillType.Doted => new DotBrush() { Color = fillColor, Distance = this.DotBrushConfiguration.Distance, Radius = this.DotBrushConfiguration.Radius },
            FillType.Lines => new LineBrush() { Color = fillColor, Configuration = this.LineBrushConfiguration },
            _ => throw new NotImplementedException()
        };
    }

}


public partial class DotBrush : XamlCompositionBrushBase {
    public required Color Color { get; init; }
    public required double Radius { get; init; }
    public required double Distance { get; init; }
    protected Compositor _compositor => App.Current.MainWindow.Compositor;

    protected CompositionBrush? _imageBrush = null;

    protected IDisposable? _surfaceSource = null;

    protected override void OnConnected() {
        base.OnConnected();

        if (CompositionBrush == null) {
            CreateEffectBrush();
            Render();
        }
    }

    protected override void OnDisconnected() {
        base.OnDisconnected();
        this.CompositionBrush?.Dispose();
        this.CompositionBrush = null;
        ClearResources();
    }

    private void ClearResources() {
        _imageBrush?.Dispose();
        _imageBrush = null;

        _surfaceSource?.Dispose();
        _surfaceSource = null;
    }

    private void UpdateBrush() {
        if (CompositionBrush != null && _imageBrush != null) {
            ((CompositionEffectBrush)CompositionBrush).SetSourceParameter(nameof(BorderEffect.Source), _imageBrush);
        }
    }

    protected ICompositionSurface CreateSurface() {
        double width = Distance + Radius * 2;
        double height = width;

        CanvasDevice device = CanvasDevice.GetSharedDevice();
        var graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, device);
        var drawingSurface = graphicsDevice.CreateDrawingSurface(
            new(width, height),
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            DirectXAlphaMode.Premultiplied);

        /* Create Drawing Session is not thread safe - only one can ever be active at a time per app */
        using (var ds = CanvasComposition.CreateDrawingSession(drawingSurface)) {
            ds.Clear(Colors.Transparent);
            var centerPoint = (float)(Radius + Distance / 2);
            ds.FillCircle(centerPoint, centerPoint, (float)Radius, this.Color);
        }

        return drawingSurface;
    }

    private void Render() {
        ClearResources();

        try {
            var src = CreateSurface();
            _surfaceSource = src as IDisposable;
            var surfaceBrush = _compositor.CreateSurfaceBrush(src);
            surfaceBrush.VerticalAlignmentRatio = 0.0f;
            surfaceBrush.HorizontalAlignmentRatio = 0.0f;
            surfaceBrush.Stretch = CompositionStretch.None;
            _imageBrush = surfaceBrush;

            UpdateBrush();
        } catch {
            // no image for you, soz.
        }
    }

    private void CreateEffectBrush() {
        using (var effect = new BorderEffect {
            Name = nameof(BorderEffect),
            ExtendY = CanvasEdgeBehavior.Wrap,
            ExtendX = CanvasEdgeBehavior.Wrap,
            Source = new CompositionEffectSourceParameter(nameof(BorderEffect.Source))
        })
        using (var _effectFactory = _compositor.CreateEffectFactory(effect)) {
            this.CompositionBrush = _effectFactory.CreateBrush();
        }
    }
}

public partial class LineBrush : XamlCompositionBrushBase {
    public required Color Color { get; init; }
    public required LineBrushViewModel Configuration { get; init; }
    protected Compositor _compositor => App.Current.MainWindow.Compositor;

    protected CompositionBrush? _imageBrush = null;

    protected IDisposable? _surfaceSource = null;

    protected override void OnConnected() {
        base.OnConnected();

        if (CompositionBrush == null) {
            CreateEffectBrush();
            Render();
        }
    }

    protected override void OnDisconnected() {
        base.OnDisconnected();
        this.CompositionBrush?.Dispose();
        this.CompositionBrush = null;
        ClearResources();
    }

    private void ClearResources() {
        _imageBrush?.Dispose();
        _imageBrush = null;

        _surfaceSource?.Dispose();
        _surfaceSource = null;
    }

    private void UpdateBrush() {
        if (CompositionBrush != null && _imageBrush != null) {
            ((CompositionEffectBrush)CompositionBrush).SetSourceParameter(nameof(BorderEffect.Source), _imageBrush);
        }
    }

    protected ICompositionSurface CreateSurface() {

        float angle = (float)Configuration.Angle;
        bool mirrow = false;
        if (angle > 90) {
            angle = 90 - (angle - 90);
            mirrow = true;
        }

        var radians = angle * MathF.PI / 180;
        float baseSize = (float)((Configuration.Thickness) + Configuration.Distance) * 2;
        float width;
        float height;
        if (Configuration.Angle is 0 or 90 or 180) {
            width = baseSize;
            height = baseSize;
        } else {
            //if (factor < 1) {
            //factor = 1 / factor;
            //width = baseSize ;
            //height = baseSize * factor;
            //} else {
            //var factor = MathF.Cos(radians) / MathF.Sin(radians);
            var widthModifire = MathF.Cos(radians);
            while (widthModifire < 1) {
                if (widthModifire == 0) {
                    widthModifire = 1;
                }
                widthModifire *= 2;
            }
            var heightModifire = MathF.Sin(radians);
            while (heightModifire < 1) {
                if (heightModifire == 0) {
                    heightModifire = 1;
                }
                heightModifire *= 2;
            }
            width = baseSize * widthModifire;
            height = baseSize * heightModifire;
            //}

        }

        CanvasDevice device = CanvasDevice.GetSharedDevice();
        var graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, device);
        var drawingSurface = graphicsDevice.CreateDrawingSurface(
            new(width, height),
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            DirectXAlphaMode.Premultiplied);


        //Vector2 direction = new(MathF.Cos(radians), MathF.Sin(radians));

        /* Create Drawing Session is not thread safe - only one can ever be active at a time per app */
        using (var ds = CanvasComposition.CreateDrawingSession(drawingSurface)) {
            ds.Clear(Colors.Transparent);
            ds.Transform = Matrix3x2.CreateRotation(radians, new(width / 2, height / 2)) * (mirrow ? Matrix3x2.CreateScale(-1, 1) : Matrix3x2.Identity);
            CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle() { };

            bool skipFirst = true;
            for (float y = height / 2; y < height * 2; y += (float)(Configuration.Thickness + Configuration.Distance)) {
                //if (skipFirst) {
                //    ds.DrawLine(-width, y, width * 2, y, Color.FromArgb(255, 255, 0, 0), (float)Configuration.Thickness, strokeStyle);
                //    skipFirst = false;
                //    continue;
                //}

                ds.DrawLine(-width, y, width * 2, y, Color, (float)Configuration.Thickness, strokeStyle);
            }
            skipFirst = true;
            for (float y = height / 2; y > -height * 2; y -= (float)(Configuration.Thickness + Configuration.Distance)) {
                if (skipFirst) {
                    skipFirst = false;
                    continue;
                }
                ds.DrawLine(-width, y, width * 2, y, Color, (float)Configuration.Thickness, strokeStyle);
            }

        }

        return drawingSurface;



    }

    private void Render() {
        ClearResources();

        try {
            var src = CreateSurface();
            _surfaceSource = src as IDisposable;
            var surfaceBrush = _compositor.CreateSurfaceBrush(src);
            surfaceBrush.VerticalAlignmentRatio = 0.0f;
            surfaceBrush.HorizontalAlignmentRatio = 0.0f;
            surfaceBrush.Stretch = CompositionStretch.None;
            _imageBrush = surfaceBrush;

            UpdateBrush();
        } catch {
            // no image for you, soz.
        }
    }

    private void CreateEffectBrush() {
        using (var effect = new BorderEffect {
            Name = nameof(BorderEffect),
            ExtendY = CanvasEdgeBehavior.Wrap,
            ExtendX = CanvasEdgeBehavior.Wrap,
            Source = new CompositionEffectSourceParameter(nameof(BorderEffect.Source))
        })
        using (var _effectFactory = _compositor.CreateEffectFactory(effect)) {
            this.CompositionBrush = _effectFactory.CreateBrush();
        }
    }
}

public partial class TileSetViewModel : ViewModel<TileSetFile, TileSetViewModel>, IViewModel<TileSetFile, TileSetViewModel>, IAsyncDisposable {




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

    private readonly List<IAsyncDisposable> disposeTerransViewModel = new();

    private async Task LoadTileModels() {
        ClearTileModels();
        this.ImageSource = null;
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

    public TileSetViewModel(TileSetFile tileSet, ProjectItem<TileSetFile> projectItem, CoreViewModel project, ObservableCollection<ProjectItem<ImageFile>> allImages) : base(project) {
        this.TileSet = tileSet;
        this.ProjectItem = projectItem;
        this.AllImages = new ReadOnlyObservableCollection<ProjectItem<ImageFile>>(allImages);
        this.tileModel = Array.Empty<TileViewModel>();
        RestoreValuesFromModel();
    }

    public override async Task RestoreValuesFromModel() {
        this.SelectedImage = TileSet.Image;
        this.TileWidth = TileSet.TileSize?.Width ?? 32;
        this.TileHeight = TileSet.TileSize?.Height ?? 32;
        this.Columns = TileSet.Columns;

        if (this.Columns == 0) {
            return;
        }
        // release tho old models...
        await DisposeTerrainsViewModels();

        Dictionary<ProjectPath, TerrainsViewModel> vmLookup = new();

        ClearTileModels();

        this.TileModel = await Task.WhenAll((IEnumerable<Task<TileViewModel>>)TileSet.TileData.Select(async (model, index) => {
            var result = new TileViewModel(index % columns, index / columns) {
                LeftTop = await GetPointViewModel(vmLookup, model.Terrain.TopLeft),
                Top = await GetPointViewModel(vmLookup, model.Terrain.Top),
                RightTop = await GetPointViewModel(vmLookup, model.Terrain.TopRight),
                Left = await GetPointViewModel(vmLookup, model.Terrain.Left),
                Center = await GetPointViewModel(vmLookup, model.Terrain.Center),
                Right = await GetPointViewModel(vmLookup, model.Terrain.Right),
                LeftBottom = await GetPointViewModel(vmLookup, model.Terrain.BottomLeft),
                Bottom = await GetPointViewModel(vmLookup, model.Terrain.Bottom),
                RightBottom = await GetPointViewModel(vmLookup, model.Terrain.BottomRight),
            };
            result.PropertyChanged += TileModelChanged;
            return result;

            async Task<TerranFormViewModel?> GetPointViewModel(Dictionary<ProjectPath, TerrainsViewModel> vmLookup, TerrainId? dataHolder) {
                if (dataHolder is null) {
                    return null;
                }
                if (!vmLookup.TryGetValue(dataHolder.Terrain, out var vm)) {
                    var disposable = App.GetViewModel(file: CoreViewModel.GetProjectItem<TerrainsFile>(dataHolder.Terrain)
                                                            ?? throw new InvalidOperationException($"Did not find ProjectItem to file {dataHolder.Terrain}"),
                                                      core: CoreViewModel,
                                                      read: true)
                                        .Of<TerrainsViewModel>(out var vmTask);
                    vm = await vmTask;
                    disposeTerransViewModel.Add(disposable);
                }
                var result = dataHolder.Type switch {
                    TerranType.Cut => vm.Terrains[dataHolder.Index].Cut,
                    TerranType.Wall => vm.Terrains[dataHolder.Index].Wall,
                    TerranType.Floor => vm.Terrains[dataHolder.Index].Floor,
                    _ => throw new NotImplementedException($"Type {dataHolder.Type} not implemented.")
                };
                return result;
            }
        }));

        UpdateHasChanges();

    }

    private async Task DisposeTerrainsViewModels() {
        foreach (var item in disposeTerransViewModel.ToArray()) {
            disposeTerransViewModel.Remove(item);
            await item.DisposeAsync();
        }
    }

    protected override Task SaveValuesToModel() {
        TileSet.Image = SelectedImage;
        TileSet.TileSize = new() { Width = TileWidth, Height = TileHeight };
        TileSet.Columns = this.Columns;
        TileSet.TileData = this.TileModel.Select(CreateTileModelData).ToArray();
        UpdateHasChanges();
        return TileSet.Save(ProjectItem.Path, CoreViewModel);
    }

    private static TileData CreateTileModelData(TileViewModel x) {
        TileData tileData = new TileData(new TerrainIdData(GetIdData(x.LeftTop), GetIdData(x.Top), GetIdData(x.RightTop), GetIdData(x.Left), GetIdData(x.Center), GetIdData(x.Right), GetIdData(x.LeftBottom), GetIdData(x.Bottom), GetIdData(x.RightBottom)));
        return tileData;
        static TerrainId? GetIdData(TerranFormViewModel? x) {
            return x is not null ? new(x.Parent.TerrainFile, x.Parent.Index, x.Type) : null;
        }
    }

    private void UpdateHasChanges() {
        this.HasChanges = this.SelectedImage != TileSet.Image ||
        this.TileWidth != (TileSet.TileSize?.Width ?? 32) ||
        this.TileHeight != (TileSet.TileSize?.Height ?? 32) ||
        this.TileModel.Length != TileSet.TileData.Length ||
        !this.TileModel.Select(CreateTileModelData).SequenceEqual(TileSet.TileData)
        ;

        //this.TileModel.Length != tileSet.TileData.Length || TileModel.Select(x=> new TileData(new TerrainIdData(x.LeftTop, x.Top, x.RightTop, x.LeftTop, x.Center, x.Right, x.LeftBottom, x.Bottom, x.RightBottom.))

    }


    public ReadOnlyObservableCollection<ProjectItem<ImageFile>> AllImages { get; }

    public ProjectItem<TileSetFile> ProjectItem { get; }

    public TileSetFile TileSet { get; }

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

    public async ValueTask DisposeAsync() {
        GC.SuppressFinalize(this);
        await DisposeTerrainsViewModels();
    }
}
