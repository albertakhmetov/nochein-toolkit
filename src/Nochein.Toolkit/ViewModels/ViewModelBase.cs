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
namespace Nochein.Toolkit.ViewModels;

using System;
using System.Collections.Generic;
using System.Text;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    protected ViewModelBase()
    {
        UIContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No UI Context");
    }

    ~ViewModelBase()
    {
        Dispose(disposing: false);
    }

    public bool IsDisposed { get; private set; }

    public SynchronizationContext UIContext { get; private set; }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        IsDisposed = true;
    }
}