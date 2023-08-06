using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest;

public static class WindowHelper
{
    private static readonly ObservableCollection<Window> winows = new();
    public static ReadOnlyObservableCollection<Window> ActiveWindows { get; } = new(winows);

    static public Window CreateWindow()
    {
        Window newWindow = new Window
        {
            SystemBackdrop = new MicaBackdrop()
        };
        TrackWindow(newWindow);
        return newWindow;
    }

    static public void TrackWindow(Window window)
    {
        window.Closed += (sender, args) =>
        {
            winows.Remove(window);
        };
        winows.Add(window);
    }


    static public Window? GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot != null)
        {
            foreach (Window window in winows)
            {
                if (element.XamlRoot == window.Content.XamlRoot)
                {
                    return window;
                }
            }
        }
        return null;
    }

}