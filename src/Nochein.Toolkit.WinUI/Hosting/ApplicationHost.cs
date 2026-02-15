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
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Nochein.Toolkit.Services;
using Windows.Media.Protection.PlayReady;

public sealed class ApplicationHost : IAsyncDisposable
{
    public ApplicationHost(Action<IServiceCollection> configureServices, bool singleInstance = false)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IHostApplicationLifetime, ApplicationLifetime>();
        serviceCollection.AddSingleton<SystemEnvironment>();
        serviceCollection.AddSingleton<IEventBus, EventBus>();

        serviceCollection.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(NullLoggerProvider.Instance);
        });

        if (singleInstance)
        {
            serviceCollection.AddHostedService<InstancePipeReceiverService>();
        }

        configureServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();

        _applicationLifetime = (ApplicationLifetime)Services.GetRequiredService<IHostApplicationLifetime>();
        _instance = singleInstance
            ? new Instance(Services.GetRequiredService<ApplicationInfo>().AppUserModelId)
            : null;
        _logger = Services.GetRequiredService<ILogger<ApplicationHost>>();
    }

    private readonly ApplicationLifetime _applicationLifetime;
    private readonly Instance? _instance;
    private readonly ILogger _logger;

    private Stack<IHostedService>? _runningServices;

    public IServiceProvider Services { get; }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public Task RunAsync()
    {
        if (_instance?.IsCurrent is false)
        {
            return _instance.SendAndRedirectAsync();
        }

        var tcs = new TaskCompletionSource();
        var uiThread = new Thread(async () =>
        {
            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();

                using var registration = _applicationLifetime.ApplicationStopping.Register(() => Application.Current?.Exit());

                Application.Start(async _ =>
                {
                    var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

                    var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
                    SynchronizationContext.SetSynchronizationContext(context);

                    dispatcherQueue.FrameworkShutdownStarting += FrameworkShutdownStarting;

                    var app = Services.GetRequiredService<Application>();
                    app.UnhandledException += (_, e) =>
                    {
                        _logger.LogCritical(e.Exception, "Unhandled Exception");
                        tcs.TrySetException(e.Exception);
                    };

                    try
                    {
                        await StartAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Unable to start background services");
                        tcs.TrySetException(ex);

                        Application.Current?.Exit();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unable to start the application");
                tcs.TrySetException(ex);
            }

            tcs.TrySetResult();
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Name = "UI Thread";
        uiThread.Start();

        return tcs.Task;
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting the host");

        if (_runningServices is not null)
        {
            _logger.LogWarning("The host is already running");
            return;
        }

        _runningServices = new Stack<IHostedService>();

        _logger.LogInformation("Starting hosted services");

        foreach (var service in Services.GetRequiredService<IEnumerable<IHostedService>>())
        {
            try
            {
                await service.StartAsync(cancellationToken).ConfigureAwait(false);

                _runningServices.Push(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} service throws the exception during starting", service.GetType().Name);

                await StopAsync(cancellationToken).ConfigureAwait(false);

                break;
            }
        }

        _logger.LogInformation("Hosted services are started");

        _applicationLifetime.NotifyStarted();
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_runningServices is null)
        {
            return;
        }

        _logger.LogInformation("Stopping the host");

        _applicationLifetime.StopApplication();

        _logger.LogInformation("Stopping hosted services");

        while (_runningServices.Count > 0)
        {
            var service = _runningServices.Pop();

            try
            {
                await service.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} service throws the exception during shutdown", service.GetType().Name);
            }
        }

        _logger.LogInformation("Hosted services are stopped");

        _runningServices = null;
        _applicationLifetime.NotifyStopped();
    }

    private async void FrameworkShutdownStarting(DispatcherQueue sender, DispatcherQueueShutdownStartingEventArgs args)
    {
        var deferral = args.GetDeferral();

        await StopAsync(CancellationToken.None);

        deferral.Complete();
    }
}
