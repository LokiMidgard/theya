﻿using PropertyChanged.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TileEditorTest.Model;

namespace TileEditorTest.ViewModel.Controls;
public partial class TileImageSelectorViewModel : IAsyncDisposable {
    [Notify]
    private int x;
    [Notify]
    private int y;
    [Notify]
    private ProjectPath? selectedTileSet;

    [Notify]
    private TileSetViewModel? selectedViewModel;
    private IAsyncDisposable? tileSetDisposable;




    public CoreViewModel Core { get; }

    public TileImageSelectorViewModel(CoreViewModel core) {
        this.Core = core;
    }

    private async void OnSelectedTileSetChanged() {
        if (this.tileSetDisposable is not null) {
            await this.tileSetDisposable.DisposeAsync();
        }
        this.SelectedViewModel = null;
        if (selectedTileSet is not null && Core.GetProjectItem<TileSetFile>(selectedTileSet.Value) is ProjectItem<TileSetFile> item) {
            this.tileSetDisposable = App.GetViewModel(item, Core, true).Of<TileSetViewModel>(out var vmTask);
            this.SelectedViewModel = await vmTask;
        }
    }

    public ValueTask DisposeAsync() {
        return tileSetDisposable is not null
            ? tileSetDisposable.DisposeAsync()
            : ValueTask.CompletedTask;
    }
}
