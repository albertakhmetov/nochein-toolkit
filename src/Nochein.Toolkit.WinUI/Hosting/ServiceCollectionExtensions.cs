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
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Nochein.Toolkit.Services;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationInfo(this IServiceCollection serviceCollection, string appUserModelId)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ValidateAppUserModelId(appUserModelId);

        serviceCollection.AddSingleton<ApplicationInfo>(_ =>
        {
            var applicationFile = GetApplicationFile();
            var applicationFileVersion = FileVersionInfo.GetVersionInfo(applicationFile.FullName);

            return new ApplicationInfo
            {
                AppUserModelId = appUserModelId,
                File = applicationFile,
                UserDataDirectory = applicationFile.Directory ?? throw new InvalidOperationException("Application Directory can't be null."),
                LegalCopyright = applicationFileVersion.LegalCopyright,
                CompanyName = applicationFileVersion.CompanyName,
                ProductName = applicationFileVersion.ProductName,
                ProductDescription = applicationFileVersion.Comments,
                ProductVersion = applicationFileVersion.ProductVersion?.Split('+').FirstOrDefault()
            };
        });
    }

    private static FileInfo GetApplicationFile()
    {
        var processFileName = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Process MainModule can't be null.");

        return new FileInfo(processFileName);
    }

    private static void ValidateAppUserModelId(string appUserModelId)
    {
        if (string.IsNullOrEmpty(appUserModelId))
        {
            throw new ArgumentException("AppUserModelId cannot be null or empty.", nameof(appUserModelId));
        }

        if (appUserModelId.Length > 250)
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' exceeds maximum length of 250 characters for Named Pipe compatibility (actual: {appUserModelId.Length}).", nameof(appUserModelId));
        }

        if (appUserModelId.Length < 3)
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' is too short. Minimum 3 characters required.", nameof(appUserModelId));
        }

        if (!char.IsLetter(appUserModelId[0]))
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' must start with a letter (a-z, A-Z) for Named Pipe compatibility. Actual first character: '{appUserModelId[0]}'.", nameof(appUserModelId));
        }

        var invalidChar = appUserModelId.FirstOrDefault(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_');

        if (invalidChar != default)
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' contains invalid character '{invalidChar}'. For Named Pipe compatibility, only letters, digits, '.', '-', '_' are allowed.", nameof(appUserModelId));
        }

        if (appUserModelId.EndsWith('.'))
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' cannot end with a dot.", nameof(appUserModelId));
        }

        if (appUserModelId.All(c => c == '.' || c == '-'))
        {
            throw new ArgumentException($"AppUserModelId '{appUserModelId}' cannot consist only of dots and dashes.", nameof(appUserModelId));
        }
    }
}
