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
namespace Nochein.Toolkit.Native;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Nochein.Toolkit.Services;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

public sealed class NativeWindow : IDisposable
{
    public unsafe NativeWindow(string? windowId = null)
    {
        _windowId = windowId ?? $"class:com.nochein.{Guid.NewGuid()}";

        _proc = OnWindowMessageReceived;

        fixed (char* className = _windowId)
        {
            var classInfo = new WNDCLASSW()
            {
                lpfnWndProc = _proc,
                lpszClassName = new PCWSTR(className),
            };

            PInvoke.RegisterClass(classInfo);

            _handle = PInvoke.CreateWindowEx(
                dwExStyle: 0,
                lpClassName: _windowId,
                lpWindowName: _windowId,
                dwStyle: 0,
                X: 0,
                Y: 0,
                nWidth: 0,
                nHeight: 0,
                hWndParent: new HWND(IntPtr.Zero),
                hMenu: null,
                hInstance: null,
                lpParam: null);
        }
    }

    ~NativeWindow()
    {
        Dispose(false);
    }

    private readonly ConcurrentDictionary<uint, ImmutableArray<Action<uint, nuint, nint>>> _subscriptions = [];

    private readonly string _windowId;
    private readonly WNDPROC _proc;

    private HWND _handle;

    public IDisposable Subscribe(uint message, Action<uint, nuint, nint> action)
    {
        lock (_subscriptions)
        {
            _subscriptions.AddOrUpdate(
                message,
                _ => [action],
                (_, list) => list.Add(action));
        }

        return Disposable.Create(() =>
        {
            lock (_subscriptions)
            {
                if (_subscriptions.TryGetValue(message, out var list))
                {
                    var newList = list.Remove(action);
                    if (newList.IsEmpty)
                    {
                        _subscriptions.TryRemove(message, out _);
                    }
                    else
                    {
                        _subscriptions.TryUpdate(message, newList, list);
                    }
                }
            }
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool isDisposing)
    {
        if (_handle != HWND.Null)
        {
            _subscriptions.Clear();

            PInvoke.DestroyWindow(hWnd: _handle);
            _handle = HWND.Null;

            PInvoke.UnregisterClass(
                lpClassName: _windowId,
                hInstance: null);
        }
    }

    private LRESULT OnWindowMessageReceived(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (_subscriptions.TryGetValue(msg, out var actions))
        {
            foreach (var action in actions)
            {
                action(msg, wParam.Value, lParam.Value);
            }
        }

        return PInvoke.DefWindowProc(
            hWnd: hwnd,
            Msg: msg,
            wParam: wParam,
            lParam: lParam);
    }

    public interface IMessageReceiver
    {
        public void Process(uint message, nuint wParam, nint lParam);
    }
}
