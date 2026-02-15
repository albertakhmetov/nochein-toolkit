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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class ApplicationLifetime : IHostApplicationLifetime
{
    public ApplicationLifetime(ILogger<ApplicationLifetime> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();
    private readonly ILogger<ApplicationLifetime> _logger;

    public CancellationToken ApplicationStarted => _startedSource.Token;

    public CancellationToken ApplicationStopping => _stoppingSource.Token;

    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication()
    {
        lock (_stoppingSource)
        {
            try
            {
                _stoppingSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while stopping the application");
            }
        }
    }

    public void NotifyStarted()
    {
        try
        {
            _startedSource.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while starting the application");
        }
    }

    public void NotifyStopped()
    {
        try
        {
            _stoppedSource.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while stopping the application");
        }
    }
}
