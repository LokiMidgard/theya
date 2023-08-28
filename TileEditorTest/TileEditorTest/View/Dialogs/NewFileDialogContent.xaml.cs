using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Dialogs;
public sealed partial class NewFileDialogContent : ContentDialog {
    private DelegateCommand Command { get; }
    public string FileName {
        get => fileName; set {
            fileName = value;
            Command.FireCanExecuteChanged();
        }
    }

    private string fileName;

    private Predicate<string>? isAllowed;

    private NewFileDialogContent() {

        this.InitializeComponent();
        Command = new(() => { }, () => this.FileName.Length > 0 && (isAllowed?.Invoke(this.FileName) ?? true));

        fileName = "";
    }

    public static async Task<string?> ShowDialog(XamlRoot xamlRoot, Predicate<string>? isAllowed = null) {
        NewFileDialogContent content = new() { XamlRoot = xamlRoot, isAllowed = isAllowed };

        var result = await content.ShowAsync();

        if (result == ContentDialogResult.Primary) {
            return content.FileName;
        }
        return null;
    }

}
