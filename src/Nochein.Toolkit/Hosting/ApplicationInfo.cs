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
namespace Nochein.Toolkit.Hosting;

using System;
using System.Diagnostics;
using System.Linq;

public sealed class ApplicationInfo
{
    public required string AppUserModelId { get; init; }

    public required FileInfo File { get; init; }

    public required DirectoryInfo UserDataDirectory { get; init; }

    public string? LegalCopyright { get; init; }

    public string? CompanyName { get; init; }

    public string? ProductName { get; init; }

    public string? ProductVersion { get; init; }

    public string? ProductDescription { get; init; }
}
