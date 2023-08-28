using CommunityToolkit.WinUI.UI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using TileEditorTest.View.Controls;
using TileEditorTest.ViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TileEditorTest.View.Dialogs;
public sealed partial class ClosedUnsavedChangesInfo : InfoBar {
    private bool undoClicked;
    private TaskCompletionSource dismiss = new();

    public string MessageText { get; }

    public ClosedUnsavedChangesInfo(ProjectPath fileToSave) {
        this.InitializeComponent();
        MessageText = $"Changes of \"{fileToSave.Value}\" will be lost.";
    }
    private void AppBarButton_Click(object sender, RoutedEventArgs e) {
        undoClicked = true;
        dismiss.TrySetResult();
    }
    private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) {
        dismiss.TrySetResult();
    }

    private Task Start() {
        TaskCompletionSource completed = new();
        this.progress.Completed += (sender, e) => completed.SetResult();
        progress.FillBehavior = Microsoft.UI.Xaml.Media.Animation.FillBehavior.Stop;

        this.progress.Begin();
        return completed.Task;
    }

    public static async Task<bool> ShowAsync(ProjectPath fileToSave, UIElement element) {
        ClosedUnsavedChangesInfo dialog = new(fileToSave) {
            IsOpen = true,
        };

        var window = App.GetWindowForElement(element) ?? throw new ArgumentException("Could not find Window for element.", nameof(element));
        var notificationArea = window.Content.FindDescendantOrSelf<NotificationArea>() ?? throw new ArgumentException("Could not find NotificationArea for element.", nameof(element));
        notificationArea.Children.Add(dialog);
        await Task.WhenAny(dialog.Start(), dialog.dismiss.Task);

        if (dialog.Parent is ItemsControl itemsControl) {
            itemsControl.Items.Remove(dialog);
        } else if (dialog.Parent is Panel panel) {
            panel.Children.Remove(dialog);
        } else if (dialog.Parent is ContentControl contentControl && Object.ReferenceEquals(contentControl.Content, dialog)) {
            contentControl.Content = null;
        }

        return dialog.undoClicked;
    }

}
