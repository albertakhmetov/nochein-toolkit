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
namespace Nochein.Toolkit.Services;

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;

public class EventBusTests
{
    [Fact]
    public void Publish_NoSubscription_NoExceptions()
    {
        var eventBus = new EventBus();

        eventBus.Publish("test message");
    }

    [Fact]
    public void Publish_SubscribedType_Observed()
    {
        const string expectedItem = "test message";

        using var waitHandle = new ManualResetEventSlim();

        var eventBus = new EventBus();

        var receivedItem = default(string); 

        using var subscription = eventBus
            .Observe<string>()
            .Subscribe(eventItem =>
            {
                receivedItem = eventItem;
                waitHandle.Set();
            });

        eventBus.Publish(expectedItem);

        waitHandle.Wait(1000).Should().BeTrue();
        receivedItem.Should().Be(expectedItem);
    }

    [Fact]
    public void Publish_DifferentType_NotObserved()
    {
        object expectedItem = 123;

        using var waitHandle = new ManualResetEventSlim();

        var eventBus = new EventBus();

        var receivedItem = default(string);

        using var subscription = eventBus
            .Observe<string>()
            .Subscribe(eventItem =>
            {
                receivedItem = eventItem;
                waitHandle.Set();
            });

        eventBus.Publish(expectedItem);

        waitHandle.Wait(100).Should().BeFalse();
        receivedItem.Should().BeNull();
    }
}
