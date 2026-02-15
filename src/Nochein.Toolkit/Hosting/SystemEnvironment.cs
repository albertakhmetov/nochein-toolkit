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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Nochein.Toolkit.Models;
using Nochein.Toolkit.Native;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

public sealed class SystemEnvironment : IDisposable
{
    public SystemEnvironment()
    {
        _window = new NativeWindow();
        _window.Subscribe(WM_WININICHANGE, Process).DisposeWith(_disposable);

        _appThemeSubject = new(GetAppTheme());
        _systemThemeSubject = new(GetSystemTheme());
        _iconWidthSubject = new(PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON));
        _iconHeightSubject = new(PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON));

        AppTheme = _appThemeSubject.AsObservable();
        SystemTheme = _systemThemeSubject.AsObservable();

        IconWidth = _iconWidthSubject.AsObservable();
        IconHeight = _iconHeightSubject.AsObservable();
    }

    private const uint WM_WININICHANGE = 0x001A;

    private readonly NativeWindow _window;
    private readonly CompositeDisposable _disposable = [];
    private readonly BehaviorSubject<WindowTheme> _appThemeSubject, _systemThemeSubject;
    private readonly BehaviorSubject<int> _iconWidthSubject, _iconHeightSubject;

    public bool IsDisposed { get; private set; }

    public IObservable<WindowTheme> AppTheme { get; }

    public IObservable<WindowTheme> SystemTheme { get; }

    public IObservable<int> IconWidth { get; }

    public IObservable<int> IconHeight { get; }

    public void Dispose()
    {
        if (IsDisposed is false)
        {
            _window.Dispose();
            _disposable.Dispose();

            IsDisposed = true;
        }
    }

    [DllImport("UxTheme.dll", EntryPoint = "#132", SetLastError = true)]
    private static extern bool ShouldAppsUseDarkMode();

    [DllImport("UxTheme.dll", EntryPoint = "#138", SetLastError = true)]
    private static extern bool ShouldSystemUseDarkMode();

    private void Process(uint message, nuint wParam, nint lParam)
    {
        if (message != WM_WININICHANGE)
        {
            return;
        }

        if (Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
        {
            _appThemeSubject.OnNext(GetAppTheme());
            _systemThemeSubject.OnNext(GetSystemTheme());
            return;
        }

        // TODO: add the ability to update icon size and dpi
    }

    private static WindowTheme GetAppTheme() => ShouldAppsUseDarkMode() ? WindowTheme.Dark : WindowTheme.Light;

    private static WindowTheme GetSystemTheme() => ShouldSystemUseDarkMode() ? WindowTheme.Dark : WindowTheme.Light;
}
