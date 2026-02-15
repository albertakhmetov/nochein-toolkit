/*  Copyright © 2026, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of Nochein Toolkit.
 *
 *  Nochein Toolkit is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Nochein Toolkit is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Nochein Toolkit. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace Nochein.Toolkit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

public sealed class AsyncRelayCommand : ObservableObject, ICommand
{
    public AsyncRelayCommand(Func<object?, CancellationToken, Task> action, Func<object?, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
        _canExecute = canExecute;
    }

    private readonly Func<object?, CancellationToken, Task> _action;
    private readonly Func<object?, bool>? _canExecute;

    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsExecuting
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                NotifyCanExecuteChanged();
            }
        }
    }

    public event EventHandler? CanExecuteChanged;

    public event EventHandler<ExceptionEventArgs>? UnhandledException;

    public event EventHandler? Canceled;

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute is null || _canExecute(parameter));

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsExecuting = true;
            await _action.Invoke(parameter, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(this, new ExceptionEventArgs(ex));
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void Cancel() => _cancellationTokenSource?.Cancel();

    public sealed class ExceptionEventArgs(Exception exception) : EventArgs
    {
        public Exception Exception { get; } = exception;
    }
}