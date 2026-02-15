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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;

public class ObservableObjectTests
{
    [Fact]
    public void SetProperty_StringValue_SetValue()
    {
        const string expectedValue = "this is a test";

        var obj = new ObservableObjectClassProperty<string>();

        obj.Property = expectedValue;

        using (new AssertionScope())
        {
            obj.Property.Should().Be(expectedValue);
            obj.IsSet.Should().BeTrue();
        }
    }

    [Fact]
    public void SetProperty_StringValue_RaisesPropertyChanged()
    {
        const string expectedValue = "this is a test";

        var obj = new ObservableObjectClassProperty<string>();

        using var monitor = obj.Monitor();
        obj.Property = expectedValue;

        monitor.Should()
            .Raise(nameof(INotifyPropertyChanged.PropertyChanged))
            .WithSender(obj)
            .WithArgs<PropertyChangedEventArgs>(args => args.PropertyName == nameof(obj.Property));
    }

    [Fact]
    public void SetProperty_SameStringValue_DoesNotRaisePropertyChanged()
    {
        const string expectedValue = "this is a test";

        var obj = new ObservableObjectClassProperty<string>()
        {
            Property = expectedValue
        };

        using var monitor = obj.Monitor();
        obj.Property = expectedValue;

        using (new AssertionScope())
        {
            monitor.Should().NotRaise(nameof(INotifyPropertyChanged.PropertyChanged));
            obj.IsSet.Should().BeFalse();
        }
    }

    [Fact]
    public void SetProperty_SameClassValue_DoesNotRaisePropertyChanged()
    {
        var expectedValue = new ClassValue { Property = "Expected string" };

        var obj = new ObservableObjectClassProperty<ClassValue>()
        {
            Property = expectedValue
        };

        using var monitor = obj.Monitor();
        obj.Property = expectedValue;

        using (new AssertionScope())
        {
            monitor.Should().NotRaise(nameof(INotifyPropertyChanged.PropertyChanged));
            obj.IsSet.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void SetProperty_SameIntValue_DoesNotRaisePropertyChanged(int expectedValue)
    {
        var obj = new ObservableObjectStructProperty<int>()
        {
            NullableProperty = expectedValue
        };

        using var monitor = obj.Monitor();
        obj.NullableProperty = expectedValue;

        using (new AssertionScope())
        {
            monitor.Should().NotRaise(nameof(INotifyPropertyChanged.PropertyChanged));
            obj.IsSet.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(100)]
    public void SetProperty_SameNullableIntValue_DoesNotRaisePropertyChanged(int? expectedValue)
    {
        var obj = new ObservableObjectStructProperty<int>()
        {
            NullableProperty = expectedValue
        };

        using var monitor = obj.Monitor();
        obj.NullableProperty = expectedValue;

        using (new AssertionScope())
        {
            monitor.Should().NotRaise(nameof(INotifyPropertyChanged.PropertyChanged));
            obj.IsSet.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(CompareOptions.None)]
    [InlineData(CompareOptions.IgnoreCase)]
    public void SetProperty_SameEnumValue_DoesNotRaisePropertyChanged(CompareOptions expectedValue)
    {
        var obj = new ObservableObjectStructProperty<CompareOptions>()
        {
            Property = expectedValue
        };

        using var monitor = obj.Monitor();
        obj.Property = expectedValue;

        using (new AssertionScope())
        {
            monitor.Should().NotRaise(nameof(INotifyPropertyChanged.PropertyChanged));
            obj.IsSet.Should().BeFalse();
        }
    }

    private sealed class ClassValue
    {
        public string? Property { get; set; }
    }

    private sealed class ObservableObjectClassProperty<T> : ObservableObject where T : class
    {
        public bool IsSet { get; private set; }

        public T? Property
        {
            get;
            set
            {
                IsSet = SetProperty(ref field, value);
            }
        }
    }

    private sealed class ObservableObjectStructProperty<T> : ObservableObject where T : struct
    {
        public bool IsSet { get; private set; }

        public T Property
        {
            get;
            set
            {
                IsSet = SetProperty(ref field, value);
            }
        }

        public T? NullableProperty
        {
            get;
            set
            {
                IsSet = SetProperty(ref field, value);
            }
        }
    }
}
