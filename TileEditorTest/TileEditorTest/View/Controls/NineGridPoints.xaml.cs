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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Controls;
public sealed partial class NineGridPoints : UserControl {

    [Notify]
    private Color? selectedColor;

    [Notify]
    private Color? point1;
    [Notify]
    private Color? point2 = Color.FromArgb(255, 0, 0, 255);
    [Notify]
    private Color? point3;
    [Notify]
    private Color? point4;
    [Notify]
    private Color? point5;
    [Notify]
    private Color? point6;
    [Notify]
    private Color? point7;
    [Notify]
    private Color? point8;
    [Notify]
    private Color? point9;

    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p1;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p2;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p3;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p4;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p5;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p6;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p7;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p8;
    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData? p9;


    public NineGridPoints() {
        SelectedColor = Color.FromArgb(255, 255, 0, 0);
        this.InitializeComponent();
        pathHolder.SizeChanged += (sender, e) => this.RecalculatePath();
    }

    [Notify(Getter.Private, global::PropertyChanged.SourceGenerator.Setter.Private)]
    private TerrainData[] paths = Array.Empty<TerrainData>();

    private void OnAnyPropertyChanged(string propertyName) {
        if (propertyName.StartsWith("Point")) {
            RecalculatePath();
        }
    }

    private void RecalculatePath() {




        Color?[,] colorsToHandle = new[,] {
            {point1, point4, point7},
            {point2, point5, point8},
            {point3, point6, point9 }
        };

        List<TerrainData> paths = CalculatePathes(colorsToHandle);

        this.Paths = paths.ToArray();

        // the buttons

        //var pathControls = new[,] {
        //    {P1, P2, P3},
        //    {P4, P5, P6},
        //    {P7, P8, P9 }
        //};
        for (int y = 0; y < 3; y++) {
            for (int x = 0; x < 3; x++) {
                Color?[,] c = new Color?[3, 3];
                c[x, y] = Color.FromArgb(255, 0, 255, 255);
                var currentPath = CalculatePathes(c)[0];
                //var pathFromCode = XamlReader.Load($"<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Path.Data>{currentPath.Path}</Path.Data></Path>") as Microsoft.UI.Xaml.Shapes. Path;
                //pathControls[x, y].Data = pathFromCode!.Data;
                var index = x + y * 3;
                switch (index) {
                    case 0:
                        P1 = currentPath; break;
                    case 1:
                        P2 = currentPath; break;
                    case 2:
                        P3 = currentPath; break;
                    case 3:
                        P4 = currentPath; break;
                    case 4:
                        P5 = currentPath; break;
                    case 5:
                        P6 = currentPath; break;
                    case 6:
                        P7 = currentPath; break;
                    case 7:
                        P8 = currentPath; break;
                    case 8:
                        P9 = currentPath; break;
                    default:
                        break;
                }

            }
        }
    }

    private List<TerrainData> CalculatePathes(Color?[,] colorsToHandle) {
        List<TerrainData> paths = new();
        while (true) {


            (int x, int y) start = (-1, -1);


            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    if (colorsToHandle[x, y] is not null) {
                        start = (x, y);
                        goto afterLoop;
                    }
                }
            }
        afterLoop:

            if (start.y == -1) {
                break;
            }


            var currentColor = colorsToHandle[start.x, start.y].Value;
            // as coordinate system we use values from 0-6 on x and y
            // that way we always will have integer numbers below
            PathBuilder builder = new(pathHolder.ActualSize / 6.0f);

            // we have 24 Points to check, in a 7×7 grid (not all points are used)
            var handledEdges = new bool[7, 7];
            var handleSubTiles = new bool[3, 3];
            // Start always here    
            builder.Move(new(start.x * 2, start.y * 2 + 1), CommandType.Absolute);
            HandelLeft(start.x, start.y);

            paths.Add(new(new Color() { B = currentColor.B, G = currentColor.G, R = currentColor.R, A = 120 }, currentColor, builder.ToString()));
            // removing handled color
            for (int x = 0; x < 3; x++) {
                for (int y = 0; y < 3; y++) {
                    if (handleSubTiles[x, y]) {
                        colorsToHandle[x, y] = null;
                    }
                }
            }

            void HandelLeft(int x, int y) {
                handleSubTiles[x, y] = true;
                if (handledEdges[x * 2, y * 2 + 1]) {
                    Finish();
                    return;
                }
                handledEdges[x * 2, y * 2 + 1] = true;
                // 4 cases, straight up, right, or left, or a corner (to the right)

                if (y == 0 || (x == 0 && colorsToHandle[x, y - 1] != currentColor)) {
                    // it is hard corner if we are at y = 0 or x = 0 and we go to right
                    builder.VerticalLine(-1)
                        .HorizontalLine(1);
                    HandelTop(x, y);
                } else if (x > 0 && colorsToHandle[x - 1, y - 1] == currentColor) {
                    // it curve to the left, if the over and diagonal is set
                    // x ? 
                    //   ↑ 
                    // 
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(-1, -1));
                    HandelBottom(x - 1, y - 1);
                } else if (colorsToHandle[x, y - 1] == currentColor) {
                    // it is straight up if the value over this matches (we already know that the value left from it did not) 
                    builder.VerticalLine(-2);
                    HandelLeft(x, y - 1);
                } else {
                    // everything should be an arc to the right.
                    builder.EllipticalArc(new(1, 1), 90, false, true, new(1, -1));
                    HandelTop(x, y);
                }
            }
            void HandelRight(int x, int y) {
                handleSubTiles[x, y] = true;
                if (handledEdges[x * 2 + 2, y * 2 + 1]) {
                    Finish();
                    return;
                }
                handledEdges[x * 2 + 2, y * 2 + 1] = true;
                // 4 cases, straight down, right, or left, or a corner (to the left)

                if (y == 2 || (x == 2 && colorsToHandle[x, y + 1] != currentColor)) {
                    // it is hard corner if we are at y = 0 or x = 0 and we go to right
                    builder.VerticalLine(1)
                        .HorizontalLine(-1);
                    HandelBottom(x, y);
                } else if (x < 2 && colorsToHandle[x + 1, y + 1] == currentColor) {
                    // it curve to the right, if the over and diagonal is set
                    // ↓   
                    // ? x 
                    // 
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(1, 1));
                    HandelTop(x + 1, y + 1);
                } else if (colorsToHandle[x, y + 1] == currentColor) {
                    // it is straight down if the value over this matches (we already know that the value right from it did not) 
                    builder.VerticalLine(+2);
                    HandelRight(x, y + 1);
                } else {
                    // everything should be an arc to the left.
                    builder.EllipticalArc(new(1, 1), 90, false, true, new(-1, 1));
                    HandelBottom(x, y);
                }

            }
            void HandelTop(int x, int y) {
                handleSubTiles[x, y] = true;
                if (handledEdges[x * 2 + 1, y * 2]) {
                    Finish();
                    return;
                }
                handledEdges[x * 2 + 1, y * 2] = true;

                // 4 cases, straight right, up arc, or down arc, or down corner
                if (x == 2 || (y == 0 && colorsToHandle[x + 1, y] != currentColor)) {
                    builder.HorizontalLine(1)
                        .VerticalLine(1);
                    HandelRight(x, y);
                } else if (y > 0 && colorsToHandle[x + 1, y - 1] == currentColor) {
                    //   x
                    // → ?
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(1, -1));
                    HandelLeft(x + 1, y - 1);
                } else if (colorsToHandle[x + 1, y] == currentColor) {
                    // strait rigt
                    builder.HorizontalLine(2);
                    HandelTop(x + 1, y);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, true, new(1, 1));

                    HandelRight(x, y);
                }
            }
            void HandelBottom(int x, int y) {
                handleSubTiles[x, y] = true;
                if (handledEdges[x * 2 + 1, y * 2 + 2]) {
                    Finish();
                    return;
                }
                handledEdges[x * 2 + 1, y * 2 + 2] = true;

                // 4 cases straight left, up arc, down arc, up corner
                if (x == 0 || (y == 2 && colorsToHandle[x - 1, y] != currentColor)) {
                    builder.HorizontalLine(-1)
                        .VerticalLine(-1);
                    HandelLeft(x, y);
                } else if (y < 2 && colorsToHandle[x - 1, y + 1] == currentColor) {
                    // ? ←
                    // x 
                    builder.EllipticalArc(new(1, 1), 90, false, false, new(-1, 1));
                    HandelRight(x - 1, y + 1);
                } else if (colorsToHandle[x - 1, y] == currentColor) {
                    builder.HorizontalLine(-2);
                    HandelBottom(x - 1, y);
                } else {
                    builder.EllipticalArc(new(1, 1), 90, false, true, new(-1, -1));
                    HandelLeft(x, y);
                }
            }
            void Finish() {

                // Handel Special case
                // ? x ?
                // x O x
                // ? x ?
                // In that case the middel value is not visited und is filled.

                if (handleSubTiles[1, 0]
                    && handleSubTiles[0, 1]
                    && handleSubTiles[2, 1]
                    && handleSubTiles[1, 2]
                    ) {
                    // now we need to check the center, if it is current color we need to set it to handeld
                    if (colorsToHandle[1, 1] == currentColor) {
                        handleSubTiles[1, 1] = true;
                    } else {
                        // Otherwise we need to cut out a circle
                        //M 30,45 A 15 15 359 0 0 60,45 A 15 15 359 0 0 30,45
                        builder.Move(new(2, 3), CommandType.Absolute)
                            .EllipticalArc(new(1, 1), 180, false, false, new(2, 0))
                            .EllipticalArc(new(1, 1), 180, false, false, new(-2, 0))
                            ;
                    }

                }

                builder.Close();
            }







        }

        return paths;
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
        if (Object.ReferenceEquals(Path1, sender)) {
            Point1 = Point1 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path2, sender)) {
            Point2 = Point2 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path3, sender)) {
            Point3 = Point3 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path4, sender)) {
            Point4 = Point4 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path5, sender)) {
            Point5 = Point5 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path6, sender)) {
            Point6 = Point6 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path7, sender)) {
            Point7 = Point7 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path8, sender)) {
            Point8 = Point8 is null ? SelectedColor : null;
        } else if (Object.ReferenceEquals(Path9, sender)) {
            Point9 = Point9 is null ? SelectedColor : null;
        }
    }
}

internal record TerrainData(Color Fill, Color Stroke, string Path);

file enum CommandType {
    Absolute,
    Relativ
}



file class PathBuilder {
    private readonly StringBuilder builder = new();
    private readonly Vector2 size;

    public PathBuilder(Vector2 size) {
        this.size = size;
    }

    public PathBuilder Move(Vector2 to, CommandType type = CommandType.Relativ) {
        Append("m", type);
        Append(to);
        return this;
    }

    public PathBuilder Line(Vector2 to, CommandType type = CommandType.Relativ) {
        Append("l", type);
        Append(to);
        return this;
    }

    public PathBuilder HorizontalLine(double to, CommandType type = CommandType.Relativ) {
        Append("h", type);
        Append(to * size.X);
        return this;
    }
    public PathBuilder VerticalLine(double to, CommandType type = CommandType.Relativ) {
        Append("v", type);
        Append(to * size.Y);
        return this;
    }

    public PathBuilder CubicBézierCurve(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("c", type);
        Append(controlPoint1);
        Append(controlPoint2);
        Append(endPoint);
        return this;
    }

    public PathBuilder QuadraticBézierCurve(Vector2 controlPoint, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("q", type);
        Append(controlPoint);
        Append(endPoint);
        return this;
    }
    public PathBuilder SmoothCubicBézierCurve(Vector2 controlPoint1, Vector2 controlPoint2, CommandType type = CommandType.Relativ) {
        Append("s", type);
        Append(controlPoint1);
        Append(controlPoint2);
        return this;
    }
    public PathBuilder SmoothQuadraticBézierCurve(Vector2 controlPoint, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("t", type);
        Append(controlPoint);
        Append(endPoint);
        return this;
    }
    public PathBuilder EllipticalArc(Vector2 size, double rotationAngle, bool isLargeArc, bool sweepDirectionFlag, Vector2 endPoint, CommandType type = CommandType.Relativ) {
        Append("a", type);
        Append(size);
        Append(rotationAngle);
        Append(isLargeArc);
        Append(sweepDirectionFlag);
        Append(endPoint);
        return this;
    }
    public string Close() {
        builder.Append("z");
        return builder.ToString();
    }

    private void Append(string command, CommandType type) {
        builder.Append($"{(type == CommandType.Relativ ? command.ToLower() : command.ToUpper())}");
    }
    private void Append(Vector2 vector) {
        Vector2 transformed = vector * size;
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

