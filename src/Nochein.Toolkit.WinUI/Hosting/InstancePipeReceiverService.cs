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
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nochein.Toolkit.Hosting;
using Nochein.Toolkit.Models;
using Nochein.Toolkit.Services;

internal sealed class InstancePipeReceiverService : BackgroundService
{
    public InstancePipeReceiverService(
        ApplicationInfo applicationInfo, 
        IEventBus eventBus, 
        ILogger<InstancePipeReceiverService> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationInfo);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(logger);

        _eventBus = eventBus;
        _logger = logger;     
        
        _pipeName = applicationInfo.AppUserModelId;
    }

    private readonly string _pipeName;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            using var pipeServer = new NamedPipeServerStream(
                _pipeName,
                direction: PipeDirection.In,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous);

            try
            {
                await pipeServer.WaitForConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Pipe connection broken, recreating.");
                continue;
            }

            try
            {
                using var reader = new StreamReader(pipeServer, Encoding.UTF8);
                var receivedData = await reader.ReadToEndAsync();

                var eventItem = JsonSerializer.Deserialize<CommandLineEvent>(receivedData);

                if (eventItem is not null)
                {
                    _eventBus.Publish(eventItem);
                }
                else
                {
                    _logger.LogWarning("Data can't be deserialized ({Data}).", receivedData);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error during receiving instance data.");

                continue;
            }
        }
    }
}
