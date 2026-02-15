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
namespace Nochein.Toolkit.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

public sealed class CommandLineEvent
{
    [JsonConstructor]
    public CommandLineEvent(DateTimeOffset timestamp, IImmutableList<string>? args)
    {
        Timestamp = timestamp;
        Args = args?.ToImmutableArray() ?? [];
    }

    public CommandLineEvent(IEnumerable<string>? args)
    {
        Timestamp = DateTimeOffset.Now;
        Args = args?.ToImmutableArray() ?? [];
    }

    public DateTimeOffset Timestamp { get; }

    public IImmutableList<string> Args { get; }
}
