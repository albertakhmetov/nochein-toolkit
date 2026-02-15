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
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

public class AsyncRelayCommandTests
{
    [Fact]
    public void Constructor_NullAction_ThrowsException()
    {
#pragma warning disable CS8625 // For testing purposes
        var action = () => new AsyncRelayCommand(null);
#pragma warning restore CS8625

        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("this is a test")]
    public void Execute_InvokeAction(object? expectedValue)
    {
        var invokes = new List<object?>();

        var command = new AsyncRelayCommand((x, _) =>
        {
            invokes.Add(x);
            return Task.CompletedTask;
        });

        command.Execute(expectedValue);

        invokes.Should().ContainSingle().Which.Should().Be(expectedValue);
    }


    [Fact]
    public void Execute_CanExecuteIsFalse_DoNotInvokeAction()
    {
        var invokes = new List<object?>();

        var command = new AsyncRelayCommand(
            (x, _) =>
            {
                invokes.Add(x);
                return Task.CompletedTask;
            },
            _ => false);

        command.Execute(null);

        invokes.Should().BeEmpty();
    }

    [Fact]
    public void Execute_Executing_IsExecutingShouldBeTrue()
    {
        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (_, _) =>
        {
            await tcs.Task;
        });

        using var monitor = command.Monitor();

        command.Execute(null);

        using (new AssertionScope())
        {
            command.IsExecuting.Should().BeTrue();
            monitor.Should()
                .Raise(nameof(ICommand.CanExecuteChanged))
                .WithSender(command)
                .WithArgs<EventArgs>();
        }

        tcs.SetResult();
    }

    [Fact]
    public void Execute_Executed_IsExecutingShouldBeFalse()
    {
        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (_, _) =>
        {
            await tcs.Task;
        });

        command.Execute(null);

        using var monitor = command.Monitor();
        tcs.SetResult();

        using (new AssertionScope())
        {
            command.IsExecuting.Should().BeFalse();
            monitor.Should()
                .Raise(nameof(ICommand.CanExecuteChanged))
                .WithSender(command)
                .WithArgs<EventArgs>();
        }
    }

    [Fact]
    public void Execute_Executing_CanExecuteShouldBeFalse()
    {
        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (_, _) =>
        {
            await tcs.Task;
        });

        command.Execute(null);

        command.CanExecute(null).Should().BeFalse();
        tcs.SetResult();
    }

    [Fact]
    public void Execute_Executing_OnlyOneInvokeDuringExecuting()
    {
        var expectedValue1 = "something one";
        var expectedValue2 = "something two";

        var invokes = new List<object?>();

        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (x, _) =>
        {
            invokes.Add(x);
            await tcs.Task;
        });

        command.Execute(expectedValue1);
        command.Execute(expectedValue2);

        invokes.Should().ContainSingle().Which.Should().Be(expectedValue1);
        tcs.SetResult();
    }

    [Fact]
    public void Execute_Exception_RaisesUnhandledException()
    {
        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (x, _) =>
        {
            ArgumentNullException.ThrowIfNull(x);
        });

        using var monitor = command.Monitor();

        command.Execute(null);
        command.Cancel();
        tcs.SetResult();

        monitor.Should()
            .Raise(nameof(AsyncRelayCommand.UnhandledException))
            .WithSender(command)
            .WithArgs<AsyncRelayCommand.ExceptionEventArgs>(e => e.Exception is ArgumentNullException)
            .WithArgs<EventArgs>();
    }

    [Fact]
    public void Cancel_InvokeShouldBeTerminated()
    {
        var expectedValue = "something one";

        var invokes = new List<object?>();

        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand(async (x, cancellationToken) =>
        {
            await tcs.Task;

            cancellationToken.ThrowIfCancellationRequested();

            invokes.Add(x);
        });

        using var monitor = command.Monitor();

        command.Execute(expectedValue);
        command.Cancel();
        tcs.SetResult();

        using (new AssertionScope())
        {
            invokes.Should().BeEmpty();
            monitor.Should()
                .Raise(nameof(AsyncRelayCommand.Canceled))
                .WithSender(command)
                .WithArgs<EventArgs>();
        }
    }

    [Fact]
    public void CanExecute_NotSet_ReturnsTrue()
    {
        var command = new AsyncRelayCommand((x, _) => Task.CompletedTask);

        command.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("this is a test", true)]
    public void CanExecute(object? parameter, bool canExecuteResult)
    {
        var command = new AsyncRelayCommand((x, _) => Task.CompletedTask, x => x is not null);

        command.CanExecute(parameter).Should().Be(canExecuteResult);
    }

    [Fact]
    public void NotifyCanExecuteChanged_RaisesCanExecuteChanged()
    {
        var command = new AsyncRelayCommand((x, _) => Task.CompletedTask, x => x is not null);

        using var monitor = command.Monitor();
        command.NotifyCanExecuteChanged();

        monitor.Should()
            .Raise(nameof(ICommand.CanExecuteChanged))
            .WithSender(command)
            .WithArgs<EventArgs>();
    }

    [Fact]
    public void NotifyCanExecuteChanged_EventIsNotSubscribed_InvokesSuccessfully()
    {
        var command = new AsyncRelayCommand((x, _) => Task.CompletedTask, x => x is not null);

        command.NotifyCanExecuteChanged();
    }
}
