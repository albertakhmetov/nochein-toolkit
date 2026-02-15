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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Windows.AppLifecycle;
using Nochein.Toolkit.Models;
using Windows.Win32;
using Windows.Win32.Foundation;

internal sealed class Instance
{
    public Instance(string appUserModelId)
    {
        ArgumentNullException.ThrowIfNull(appUserModelId);

        _appInstance = AppInstance.FindOrRegisterForKey(appUserModelId);
        _appUserModeId = appUserModelId;
        IsCurrent = _appInstance.IsCurrent;

        if (IsCurrent)
        {
            _appInstance.Activated += (_, _) => PInvoke.SetForegroundWindow((HWND)Process.GetCurrentProcess().MainWindowHandle);

            PInvoke.SetCurrentProcessExplicitAppUserModelID(appUserModelId);
        }
    }

    private readonly AppInstance _appInstance;
    private readonly string _appUserModeId;

    public bool IsCurrent { get; }

    public async Task SendAndRedirectAsync()
    {
        await SendAsync(Environment.GetCommandLineArgs()?.Skip(1));

        await RedirectAsync();
    }

    private async Task RedirectAsync()
    {
        var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        await _appInstance
            .RedirectActivationToAsync(activatedEventArgs)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task<bool> SendAsync(IEnumerable<string>? args)
    {
        var commandLineItem = new CommandLineEvent(DateTimeOffset.Now, args is null ? [] : args.ToImmutableArray());
        var data = JsonSerializer.Serialize(commandLineItem);

        using var pipeClient = new NamedPipeClientStream(
            ".",
            _appUserModeId,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        if (await TryToConnect(pipeClient))
        {
            try
            {
                using var writer = new StreamWriter(pipeClient, Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                await writer.WriteAsync(data);
                await writer.FlushAsync();

                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        return false;
    }

    private static async Task<bool> TryToConnect(NamedPipeClientStream pipeClient)
    {
        const int MAX_ATTEMPT_COUNT = 5;
        const int CONNECT_TIMEOUT = 1000;
        const int RETRY_DELAY = 100;

        for (var attempt = 0; attempt < MAX_ATTEMPT_COUNT; attempt++)
        {
            try
            {
                await pipeClient.ConnectAsync(CONNECT_TIMEOUT);

                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                if (attempt == MAX_ATTEMPT_COUNT - 1)
                {
                    return false;
                }

                await Task.Delay(RETRY_DELAY);
            }
        }

        return false;
    }
}
