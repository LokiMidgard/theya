using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Shapes;
using PropertyChanged.SourceGenerator;

namespace TileEditorTest.View.Controls;
public partial class AlignmentGrid : ContentControl {



    private readonly Canvas containerCanvas = new();


    /// <summary>
    /// Gets or sets the step to use horizontally.
    /// </summary>
    [Notify]
    public Brush? lineBrush;

    /// <summary>
    /// Gets or sets the step to use horizontally.
    /// </summary>
    [Notify]
    public int tileWidth;

    /// <summary>
    /// Gets or sets the step to use horizontally.
    /// </summary>
    [Notify]
    public int tileHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlignmentGrid"/> class.
    /// </summary>
    public AlignmentGrid() {
        SizeChanged += AlignmentGrid_SizeChanged;

        IsHitTestVisible = false;
        IsTabStop = false;
        Opacity = 0.5;

        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        Content = containerCanvas;
    }

    private void Rebuild() {
        containerCanvas.Children.Clear();
        if (tileWidth == 0 || tileHeight == 0) {
            return;
        }
        var brush = LineBrush ?? (Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"];

        var horizontalLength = ((int)ActualWidth) / tileWidth * tileWidth;
        var verticalLength = ((int)ActualHeight) / tileHeight * tileHeight;

        if (tileWidth > 0) {
            for (double x = 0; x < ActualWidth; x += tileWidth) {
                var line = new Rectangle {
                    Width = 1,
                    Height = verticalLength,
                    Fill = brush
                };
                Canvas.SetLeft(line, x);

                containerCanvas.Children.Add(line);
            }
        }

        if (tileHeight > 0) {
            for (double y = 0; y < ActualHeight; y += tileHeight) {
                var line = new Rectangle {
                    Width = horizontalLength,
                    Height = 1,
                    Fill = brush
                };
                Canvas.SetTop(line, y);

                containerCanvas.Children.Add(line);
            }
        }
    }

    private void AlignmentGrid_SizeChanged(object sender, SizeChangedEventArgs e) => Rebuild();
    private void OnAnyPropertyChanged(string propertyName) => Rebuild();
}