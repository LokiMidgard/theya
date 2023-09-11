using static System.FormattableString;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using Microsoft.UI.Xaml.Navigation;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using System.Collections;
using System.ComponentModel;
using Microsoft.UI.Xaml.Markup;
using TileEditorTest.ViewModel;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Controls;
public sealed partial class NineGridPoints : UserControl {

    [Notify]
    private TerranFormViewModel? selectedColor;

    [Notify]
    private TileSetViewModel? viewModel;

    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData[] paths = Array.Empty<TerrainData>();

    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private IList<TerrainData> mousePaths = Array.Empty<TerrainData>();

    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private int hoverTileX;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private int hoverTileY;
    private TileViewModel[]? lastTielsModels;

    private void OnViewModelChanged(TileSetViewModel? oldValue, TileSetViewModel? newValue) {

        if (oldValue is not null) {
            oldValue.PropertyChanged -= ViewModelPropertyChanged;
        }
        if (newValue is not null) {
            newValue.PropertyChanged += ViewModelPropertyChanged;
            ViewModelPropertyChanged(this, new PropertyChangedEventArgs(nameof(ViewModel.TileModel)));
        }
        RecalculatePath();
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        System.Diagnostics.Debug.Assert(ViewModel is not null);
        if (e.PropertyName == nameof(ViewModel.TileModel)) {


            foreach (var t in lastTielsModels ?? Enumerable.Empty<TileViewModel>()) {
                t.PropertyChanged -= TileModelChanged;
            }
            this.lastTielsModels = ViewModel.TileModel;
            foreach (var t in ViewModel.TileModel) {
                t.PropertyChanged += TileModelChanged;
            }
            RecalculatePath();
        }
    }

    private void TileModelChanged(object? sender, PropertyChangedEventArgs e) {
        RecalculatePath();
    }

    public NineGridPoints() {
        this.InitializeComponent();
        pathHolder.SizeChanged += (sender, e) => this.RecalculatePath();
    }

    private void OnAnyPropertyChanged(string propertyName) {
        if (propertyName.StartsWith("Point")) {
            RecalculatePath();
        }
    }

    private void RecalculatePath() {
        var transform = new TranslateTransform() { };
        List<TerrainData> paths = new();
        if (viewModel is not null) {
            TerranFormViewModel?[,] colorsToHandle = new TerranFormViewModel[3 * viewModel.Columns, 3 * viewModel.Rows];

            for (int tileX = 0; tileX < viewModel.Columns; tileX++) {
                for (int tileY = 0; tileY < viewModel.Rows; tileY++) {
                    for (int x = 0; x < 3; x++) {
                        for (global::System.Int32 y = 0; y < 3; y++) {
                            var tileModelIndex = tileX + tileY * viewModel.Columns;
                            if (tileModelIndex < viewModel.TileModel.Length) {
                                // When changing Propertys the Events gtt fired in not quite valid states
                                colorsToHandle[tileX * 3 + x, tileY * 3 + y] = viewModel.TileModel[tileModelIndex].Grid[x, y];
                            }
                        }
                    }
                }
            }

            (int x, int y) start = (0, 0);
            while (true) {
                for (int y = start.y; y < colorsToHandle.GetLength(1); y++) {
                    for (int x = 0 /* dont't start at x since we will skip it every later y start.x*/; x < colorsToHandle.GetLength(0); x++) {
                        if (colorsToHandle[x, y] is not null) {
                            start = (x, y);
                            goto afterLoop;
                        }
                    }
                }
                break; // Did not find anything…
            afterLoop:

                var currentColor = colorsToHandle[start.x, start.y];
                if (currentColor is not null) {
                    string path = CalculatePath(transform, colorsToHandle, start, currentColor, Edge4.Left);// We have always the most top left point so we start left

                    paths.Add(new(currentColor, path));
                }
            }
        }

        this.Paths = paths.ToArray();
    }

    private string CalculatePath(Transform transform, TerranFormViewModel?[,] colorsToHandle, (int x, int y) start, TerranFormViewModel currentColor, Edge4 startEdge, bool negate = false) {

        if (viewModel is null) {
            return "";
        }

        // as coordinate system we use values from 0-6 on x and y
        // that way we always will have integer numbers below

        Transform absoluteTransform = new TransformGroup() {
            Children = {
                    new ScaleTransform(){
                        ScaleX = viewModel.TileWidth / 6.0,
                        ScaleY = viewModel.TileHeight/6.0
                    },
                    transform,
                }
        };
        Transform relativTransform = new TransformGroup() {
            Children = {
                    new ScaleTransform(){
                        ScaleX = viewModel.TileWidth / 6.0,
                        ScaleY = viewModel.TileHeight/6.0
                    },
                }
        };

        PathBuilder builder = new(absoluteTransform, relativTransform);

        // we have 24 Points to check, in a 7×7 grid (not all points are used)
        Edge4?[,] handledEdges = new Edge4?[7 * viewModel.Columns, 7 * viewModel.Rows];
        bool[,] handleSubTiles = new bool[3 * viewModel.Columns, 3 * viewModel.Rows];
        // Start here    
        int handledX = start.x * 2 + (startEdge switch {
            Edge4.Top or Edge4.Bottom => 1,
            Edge4.Right => 2,
            _ => 0
        });
        int handledY = start.y * 2 + (startEdge switch {
            Edge4.Left or Edge4.Right => 1,
            Edge4.Bottom => 2,
            _ => 0
        });

        builder.Move(new(handledX, handledY), CommandType.Absolute);
        switch (startEdge) {
            case Edge4.Left:
                HandelLeft(start.x, start.y);
                break;
            case Edge4.Right:
                HandelRight(start.x, start.y);
                break;
            case Edge4.Top:
                HandelTop(start.x, start.y);
                break;
            case Edge4.Bottom:
                HandelBottom(start.x, start.y);
                break;
            default:
                break;
        }

        (bool topLeft, bool top, bool topRight, bool left, bool right, bool bottomLeft, bool bottem, bool bottemRight) GetNeighbors(int x, int y) {
            return (
                x > 0 && y > 0 && colorsToHandle[x - 1, y - 1] == currentColor ^ negate,
                y > 0 && colorsToHandle[x, y - 1] == currentColor ^ negate,
                y > 0 && x < colorsToHandle.GetLength(0) - 1 && colorsToHandle[x + 1, y - 1] == currentColor ^ negate,
                x > 0 && colorsToHandle[x - 1, y] == currentColor ^ negate,
                x < colorsToHandle.GetLength(0) - 1 && colorsToHandle[x + 1, y] == currentColor ^ negate,
                x > 0 && y < colorsToHandle.GetLength(1) - 1 && colorsToHandle[x - 1, y + 1] == currentColor ^ negate,
                y < colorsToHandle.GetLength(1) - 1 && colorsToHandle[x, y + 1] == currentColor ^ negate,
                x < colorsToHandle.GetLength(0) - 1 && y < colorsToHandle.GetLength(1) - 1 && colorsToHandle[x + 1, y + 1] == currentColor ^ negate
                );
        }

        bool IsHandled(int x, int y, Edge4 edge) {
            int handledX = x * 2 + (edge switch {
                Edge4.Top or Edge4.Bottom => 1,
                Edge4.Right => 2,
                _ => 0
            });
            int handledY = y * 2 + (edge switch {
                Edge4.Left or Edge4.Right => 1,
                Edge4.Bottom => 2,
                _ => 0
            });
            handleSubTiles[x, y] = true;
            if (handledEdges[handledX, handledY] is not null) {
                return true;
            }
            handledEdges[handledX, handledY] = edge;
            return false;
        }

        Edge4[] GetEdges(int x, int y) {
            return Enum.GetValues<Edge4>().Where(edge => {

                int handledX = x * 2 + (edge switch {
                    Edge4.Top or Edge4.Bottom => 1,
                    Edge4.Right => 2,
                    _ => 0
                });
                int handledY = y * 2 + (edge switch {
                    Edge4.Left or Edge4.Right => 1,
                    Edge4.Bottom => 2,
                    _ => 0
                });
                return handledEdges[handledX, handledY] == edge;
            }).ToArray();
        }

        void HandelLeft(int x, int y) {
            if (IsHandled(x, y, Edge4.Left)) {
                Finish();
                return;
            }

            var neighbors = GetNeighbors(x, y);

            // 4 cases, straight up, right, or left, or a corner (to the right)

            if (neighbors.topLeft && (!negate || neighbors.top)) {
                // it curve to the left, if the over and diagonal is set
                // x ? 
                //   ↑ 
                // 
                if ((y % 3) == 0 || (((x - 1) % 3) == 2)) {
                    builder.VerticalLine(-1)
                        .HorizontalLine(-1);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(-1, -1));
                }
                HandelBottom(x - 1, y - 1);
            } else if (neighbors.top) {
                // it is straight up if the value over this matches (we already know that the value left from it did not) 
                builder.VerticalLine(-2);
                HandelLeft(x, y - 1);
            } else if ((y % 3) == 0 || ((x % 3) == 0)) {
                // it is hard corner if we are at y = 0 or x = 0 and we go to right
                builder.VerticalLine(-1)
                    .HorizontalLine(1);
                HandelTop(x, y);
            } else {
                // everything should be an arc to the right.
                builder.EllipticalArc(new(1, 1), 90, false, true, new(1, -1));
                HandelTop(x, y);
            }
        }
        void HandelRight(int x, int y) {
            if (IsHandled(x, y, Edge4.Right)) {
                Finish();
                return;
            }
            var neighbors = GetNeighbors(x, y);

            // 4 cases, straight down, right, or left, or a corner (to the left)

            if (neighbors.bottemRight && (!negate || neighbors.bottem)) {
                // it curve to the right, if the over and diagonal is set
                // ↓   
                // ? x 
                if (((x + 1) % 3) == 0 || (y % 3) == 2) {
                    builder.VerticalLine(1)
                        .HorizontalLine(1);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(1, 1));
                }
                HandelTop(x + 1, y + 1);
            } else if (neighbors.bottem) {
                // it is straight down if the value over this matches (we already know that the value right from it did not) 
                builder.VerticalLine(+2);
                HandelRight(x, y + 1);
            } else if ((y % 3) == 2 || ((x % 3) == 2)) {
                // it is hard corner if we are at y = 0 or x = 0 and we go to right
                builder.VerticalLine(1)
                    .HorizontalLine(-1);
                HandelBottom(x, y);
            } else {
                // everything should be an arc to the left.
                builder.EllipticalArc(new(1, 1), 90, false, true, new(-1, 1));
                HandelBottom(x, y);
            }

        }
        void HandelTop(int x, int y) {
            if (IsHandled(x, y, Edge4.Top)) {
                Finish();
                return;
            }
            var neighbors = GetNeighbors(x, y);

            // 4 cases, straight right, up arc, or down arc, or down corner
            if (neighbors.topRight && (!negate || neighbors.right)) {
                //   x
                // → ?
                if (((y - 1) % 3) == 2 || (x % 3) == 2) {
                    builder
                        .HorizontalLine(1)
                        .VerticalLine(-1);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(1, -1));
                }
                HandelLeft(x + 1, y - 1);
            } else if (neighbors.right) {
                // strait rigt
                builder.HorizontalLine(2);
                HandelTop(x + 1, y);
            } else if ((x % 3) == 2 || ((y % 3) == 0)) {
                builder.HorizontalLine(1)
                    .VerticalLine(1);
                HandelRight(x, y);
            } else {
                builder.EllipticalArc(new(1, 1), 90, false, true, new(1, 1));
                HandelRight(x, y);
            }
        }
        void HandelBottom(int x, int y) {
            if (IsHandled(x, y, Edge4.Bottom)) {
                Finish();
                return;
            }
            var neighbors = GetNeighbors(x, y);

            // 4 cases straight left, up arc, down arc, up corner
            if (neighbors.bottomLeft && (!negate || neighbors.left)) {
                // ? ←
                // x 
                if (((y + 1) % 3) == 0 || ((x % 3) == 0)) {
                    builder
                        .HorizontalLine(-1)
                        .VerticalLine(1);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(-1, 1));
                }
                HandelRight(x - 1, y + 1);
            } else if (neighbors.left) {
                builder.HorizontalLine(-2);
                HandelBottom(x - 1, y);
            } else if ((x % 3) == 0 || ((y % 3) == 2)) {
                builder.HorizontalLine(-1)
                    .VerticalLine(-1);
                HandelLeft(x, y);
            } else {
                builder.EllipticalArc(new(1, 1), 90, false, true, new(-1, -1));
                HandelLeft(x, y);
            }
        }
        void Finish() {
            // Get Edges of current tile and if at least one is set and another not, fill
            var visited = new HashSet<(int x, int y)>();
            var startPointsForCut = new Dictionary<(int x, int y), Edge4>();
            Visit(start.x, start.y);
            bool Visit(int x, int y) {
                if (visited.Contains((x, y)) || x < 0 || x >= colorsToHandle.GetLength(0) || y < 0 || y >= colorsToHandle.GetLength(1)) {
                    return false;
                }
                visited.Add((x, y));
                if (colorsToHandle[x, y] == currentColor) {
                    handleSubTiles[x, y] = true;
                    var currentEdges = GetEdges(x, y);
                    //if (currentEdges.Length > 0) {
                    if (!currentEdges.Contains(Edge4.Left)) {
                        if (Visit(x - 1, y)) {
                            startPointsForCut.TryAdd((x - 1, y), Edge4.Right);
                        }
                    }
                    if (!currentEdges.Contains(Edge4.Right)) {
                        if (Visit(x + 1, y)) {
                            startPointsForCut.TryAdd((x + 1, y), Edge4.Left);
                        }
                    }
                    if (!currentEdges.Contains(Edge4.Top)) {
                        if (Visit(x, y - 1)) {
                            startPointsForCut.TryAdd((x, y - 1), Edge4.Bottom);
                        }
                    }
                    if (!currentEdges.Contains(Edge4.Bottom)) {
                        if (Visit(x, y + 1)) {
                            startPointsForCut.TryAdd((x, y + 1), Edge4.Top);
                        }
                    }

                    var neighbors = GetNeighbors(x, y);
                    if (neighbors.topLeft) {
                        Visit(x - 1, y - 1);
                    }
                    if (neighbors.top) {
                        Visit(x, y - 1);
                    }
                    if (neighbors.topRight) {
                        Visit(x + 1, y - 1);
                    }
                    if (neighbors.left) {
                        Visit(x - 1, y);
                    }
                    if (neighbors.right) {
                        Visit(x + 1, y);
                    }
                    if (neighbors.bottomLeft) {
                        Visit(x - 1, y + 1);
                    }
                    if (neighbors.bottem) {
                        Visit(x, y + 1);
                    }
                    if (neighbors.bottemRight) {
                        Visit(x + 1, y + 1);
                    }

                    return false;
                } else {
                    return true;
                }
            }

            if (startPointsForCut.Count > 0) {
                var copyColorToHandle = new TerranFormViewModel?[colorsToHandle.GetLength(0), colorsToHandle.GetLength(1)];
                Array.Copy(colorsToHandle, copyColorToHandle, colorsToHandle.Length);
                while (!negate && startPointsForCut.Count > 0) {

                    var startPoint = startPointsForCut.Keys.First();
                    var edge = startPointsForCut[startPoint];
                    startPointsForCut.Remove(startPoint);

                    if (copyColorToHandle[startPoint.x, startPoint.y] != currentColor) {
                        var cutoutPath = CalculatePath(transform, copyColorToHandle, startPoint, currentColor, edge, true);
                        builder.AppendPath(cutoutPath);
                    }
                }
            }

            builder.Close();
        }
        string path = builder.ToString();

        // removing handled color
        for (int x = 0; x < colorsToHandle.GetLength(0); x++) {
            for (int y = 0; y < colorsToHandle.GetLength(1); y++) {
                if (handleSubTiles[x, y]) {
                    colorsToHandle[x, y] = negate ? currentColor : null;
                }
            }
        }

        return path;
    }

    private enum Edge4 {
        Top = 2,
        Left = 4,
        Right = 6,
        Bottom = 8,
    }
    private enum Edge8 {
        TopLeft = 1,
        Top = 2,
        TopRight = 3,
        Left = 4,
        Right = 6,
        BottomLeft = 7,
        Bottom = 8,
        BottomRight = 9,
    }

    private void Path_PointerEntered(object sender, PointerRoutedEventArgs e) {
        FrameworkElement frameworkElement = (FrameworkElement)sender;
        frameworkElement.Opacity = 1;
    }

    private void Path_PointerExited(object sender, PointerRoutedEventArgs e) {
        FrameworkElement frameworkElement = (FrameworkElement)sender;
        frameworkElement.Opacity = 0;
    }

    private void Path_PointerPressed(object sender, PointerRoutedEventArgs e) {
        //if (Object.ReferenceEquals(Path1, sender)) {
        //    Point1 = Point1 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path2, sender)) {
        //    Point2 = Point2 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path3, sender)) {
        //    Point3 = Point3 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path4, sender)) {
        //    Point4 = Point4 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path5, sender)) {
        //    Point5 = Point5 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path6, sender)) {
        //    Point6 = Point6 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path7, sender)) {
        //    Point7 = Point7 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path8, sender)) {
        //    Point8 = Point8 is null ? SelectedColor : null;
        //} else if (Object.ReferenceEquals(Path9, sender)) {
        //    Point9 = Point9 is null ? SelectedColor : null;
        //}
    }

    private async void UserControl_PointerMoved(object sender, PointerRoutedEventArgs e) {
        if (viewModel is null) {
            return;
        }
        var position = e.GetCurrentPoint(this);
        GetTileSubPosition(position.Position, out int tileX, out int tileY, out int subX, out int subY);

        ////this.HoverTileX = tileX * viewModel.TileWidth;
        ////this.HoverTileY = tileY * viewModel.TileHeight;

        //TileViewModel tempModel = new(tileX, tileY);

        //tempModel.Grid[subX, subY] = new() { Color = Color.FromArgb(255, 0, 255, 255) };
        ////var currentPath = CalculatePathes(c)[0];
        //throw new NotImplementedException();
        //this.MousePaths = CalculatePathes(tempModel, new TranslateTransform() { X = tileX * viewModel.TileWidth, Y = tileY * viewModel.TileHeight });

        var transform = new TranslateTransform() { X = tileX * viewModel.TileWidth, Y = tileY * viewModel.TileHeight };
        TerranFormViewModel mouseOverTerrain = new(TerranType.Cut, new(viewModel.CoreViewModel, Guid.NewGuid(), viewModel.ProjectItem.Path) { Color = Color.FromArgb(255, 0, 255, 255) });

        var grid = new TerranFormViewModel[3, 3];
        grid[subX, subY] = mouseOverTerrain;

        string path = CalculatePath(transform, grid, (subX, subY), mouseOverTerrain, Edge4.Left);
        this.MousePaths = new[] { new TerrainData(mouseOverTerrain, path) };
        if (e.Pointer.IsInContact) {

            viewModel.TileModel[tileX + tileY * viewModel.Columns].Points[subX + subY * 3] = position.Properties.IsRightButtonPressed ? null : selectedColor;
        }
    }

    private void GetTileSubPosition(Point e, out int tileX, out int tileY, out int subX, out int subY) {
        if (viewModel is null) {
            throw new InvalidOperationException();
        }
        var position = e;
        tileX = (int)(position.X / viewModel.TileWidth);
        tileY = (int)(position.Y / viewModel.TileHeight);
        subX = (int)((position.X % viewModel.TileWidth) / (viewModel.TileWidth / 3.0));
        subY = (int)((position.Y % viewModel.TileHeight) / (viewModel.TileHeight / 3.0));
    }

    private void UserControl_Tapped(object sender, TappedRoutedEventArgs e) {
        if (viewModel is null) {
            return;
        }
        var position = e.GetPosition(this);
        GetTileSubPosition(position, out int tileX, out int tileY, out int subX, out int subY);
        viewModel.TileModel[tileX + tileY * viewModel.Columns].Points[subX + subY * 3] = selectedColor;

    }

    private void UserControl_RightTapped(object sender, RightTappedRoutedEventArgs e) {
        if (viewModel is null) {
            return;
        }
        var position = e.GetPosition(this);
        GetTileSubPosition(position, out int tileX, out int tileY, out int subX, out int subY);
        viewModel.TileModel[tileX + tileY * viewModel.Columns].Points[subX + subY * 3] = null;

    }
}

internal record TerrainData(TerranFormViewModel Model, string Path);

file enum CommandType {
    Absolute,
    Relativ
}

file class PathBuilder {
    private readonly StringBuilder builder = new();
    private readonly Transform absoluteTransform;
    private readonly Transform relativTransform;

    public PathBuilder(Transform transform, Transform relativTransform) {
        this.absoluteTransform = transform;
        this.relativTransform = relativTransform;
    }

    public PathBuilder Move(Vector2 to, CommandType type = CommandType.Relativ) {
        Append("m", type);
        Append(to, type);
        return this;
    }

    public PathBuilder Line(Vector2 to, CommandType type = CommandType.Relativ) {
        Append("l", type);
        Append(to, type);
        return this;
    }

    public PathBuilder HorizontalLine(double to, CommandType type = CommandType.Relativ) {
        Append("h", type);
        Append(GetTransform(type).TransformPoint(new(to, 0)).X);
        return this;
    }
    public PathBuilder VerticalLine(double to, CommandType type = CommandType.Relativ) {
        Append("v", type);
        Append(GetTransform(type).TransformPoint(new(0, to)).Y);
        return this;
    }

    public PathBuilder CubicBézierCurve(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("c", type);
        Append(controlPoint1, type);
        Append(controlPoint2, type);
        Append(endPoint, type);
        return this;
    }

    public PathBuilder QuadraticBézierCurve(Vector2 controlPoint, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("q", type);
        Append(controlPoint, type);
        Append(endPoint, type);
        return this;
    }
    public PathBuilder SmoothCubicBézierCurve(Vector2 controlPoint1, Vector2 controlPoint2, CommandType type = CommandType.Relativ) {
        Append("s", type);
        Append(controlPoint1, type);
        Append(controlPoint2, type);
        return this;
    }
    public PathBuilder SmoothQuadraticBézierCurve(Vector2 controlPoint, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("t", type);
        Append(controlPoint, type);
        Append(endPoint, type);
        return this;
    }
    public PathBuilder EllipticalArc(Vector2 size, double rotationAngle, bool isLargeArc, bool sweepDirectionFlag, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("a", type);
        Append(size, type, true);  // I would think that x and y should not be switched??
        Append(rotationAngle);
        Append(isLargeArc);
        Append(sweepDirectionFlag);
        Append(endPoint, type);
        return this;
    }

    public PathBuilder AppendPath(string path) {
        this.builder.Append(path.TrimEnd('z', 'Z'));
        return this;
    }

    public string Close() {
        builder.Append("z");
        return builder.ToString();
    }

    private Transform GetTransform(CommandType type) => type == CommandType.Relativ ? relativTransform : absoluteTransform;

    private void Append(string command, CommandType type) {
        builder.Append($"{(type == CommandType.Relativ ? command.ToLower() : command.ToUpper())}");
    }
    private void Append(Vector2 vector, CommandType type, bool switchComponents = false) {
        var transformed = GetTransform(type).TransformPoint(new(vector.X, vector.Y));
        if (switchComponents) {
            transformed = new(transformed.Y, transformed.X);
        }
        builder.Append(Invariant($"{transformed.X},{transformed.Y} "));
    }
    private void Append(double value) {
        builder.Append(Invariant($"{value} "));
    }
    private void Append(int value) {
        builder.Append($"{value} ");
    }
    private void Append(bool value) {
        builder.Append($"{(value ? '1' : '0')} ");
    }

    public override string ToString() => builder.ToString();
}

