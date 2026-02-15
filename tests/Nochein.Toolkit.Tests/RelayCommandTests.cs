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
using FluentAssertions;

public class RelayCommandTests
{
    [Fact]
    public void Constructor_NullAction_ThrowsException()
    {
#pragma warning disable CS8625 // For testing purposes
        var action = () => new RelayCommand(null);
#pragma warning restore CS8625

        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("this is a test")]
    public void Execute_InvokeAction(object? expectedValue)
    {
        var invokes = new List<object?>();

        var command = new RelayCommand(x => invokes.Add(x));
        command.Execute(expectedValue);

        invokes.Should().ContainSingle().Which.Should().Be(expectedValue);
    }

    [Fact]
    public void Execute_CanExecuteIsFalse_DoNotInvokeAction()
    {
        var invokes = new List<object?>();

        var command = new RelayCommand(x => invokes.Add(x), _ => false);
        command.Execute(null);

        invokes.Should().BeEmpty();
    }

    [Fact]
    public void CanExecute_NotSet_ReturnsTrue()
    {
        var command = new RelayCommand(x => { });

        command.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("this is a test", true)]
    public void CanExecute(object? parameter, bool canExecuteResult)
    {
        var command = new RelayCommand(x => { }, x => x is not null);

        command.CanExecute(parameter).Should().Be(canExecuteResult);
    }

    [Fact]
    public void NotifyCanExecuteChanged_RaisesCanExecuteChanged()
    {
        var command = new RelayCommand(x => { }, x => x is not null);

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
        var command = new RelayCommand(x => { }, x => x is not null);

        command.NotifyCanExecuteChanged();
    }
}
