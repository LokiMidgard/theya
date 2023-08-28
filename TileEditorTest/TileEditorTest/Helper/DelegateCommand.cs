using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TileEditorTest.Helper;
internal class DelegateCommand : ICommand {
    private readonly Action execute;
    private readonly Func<bool>? canExecute;

    public event EventHandler? CanExecuteChanged;

    public DelegateCommand(Action execute) {
        this.execute = execute;
    }

    public void FireCanExecuteChanged() {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public DelegateCommand(Action execute, Func<bool> canExecute) {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) {
        bool result = canExecute?.Invoke() ?? true;
        return result;
    }

    public void Execute(object? parameter) {
        execute();
    }
}
