﻿#region Copyright (BSD 3-Clause License)
/*
 * GyroShell - A modern, extensible, fast, and customizable shell platform.
 * Copyright 2022-2024 Pdawg
 *
 * Licensed under the BSD 3-Clause License.
 * SPDX-License-Identifier: BSD-3-Clause
 */
#endregion

using GyroShell.Library.Services.Environment;
using System;
using System.IO;
using Windows.ApplicationModel;
using static GyroShell.Library.Helpers.Win32.Win32Interop;

namespace GyroShell.Services.Environment
{
    public class EnvironmentInfoService : IEnvironmentInfoService
    {
        public string SystemArchitecture { get; init; }
        public Version AppVersion { get; init; }
        public DateTime AppBuildDate { get; init; }


        private IntPtr _mainWindowHandle;
        public IntPtr MainWindowHandle
        {
            get => _mainWindowHandle;
            set => _mainWindowHandle = value;
        }

        private int _settingsInstances;
        public int SettingsInstances
        {
            get => _settingsInstances;
            set => _settingsInstances = value;
        }

        public bool IsSystemUsingDarkmode
        {
            get => ShouldSystemUseDarkMode(); 
        }

        public bool IsWindows11
        {
            get => System.Environment.OSVersion.Version.Build >= 22000;
        }

        public int MonitorWidth
        {
            get => GetSystemMetrics(SM_CXSCREEN);
        }

        public int MonitorHeight
        {
            get => GetSystemMetrics(SM_CYSCREEN);
        }

        public EnvironmentInfoService()
        {
            try
            {
                if (Package.Current.InstalledLocation != null)
                {
                    Package package = Package.Current;
                    SystemArchitecture = package.Id.Architecture.ToString();

                    PackageVersion version = package.Id.Version;
                    AppVersion = new Version(version.Major, version.Minor, version.Build, version.Revision);

                    string mainExecutablePath = Path.Combine(Package.Current.InstalledLocation.Path, "GyroShell.exe");
                    AppBuildDate = File.GetLastWriteTime(mainExecutablePath);
                }
            }
            catch
            {
                SYSTEM_INFO sysInfo = new SYSTEM_INFO();
                GetNativeSystemInfo(out sysInfo);

                ushort arch = sysInfo.wProcessorArchitecture;

                SystemArchitecture = arch switch
                {
                    0 => "X86",
                    5 => "ARM",
                    6 => "ARM64",
                    9 => "X64",
                    _ => "Unknown Processor",
                };

                AppVersion = new Version(1, 0, 0, 0);
                AppBuildDate = new DateTime(2024, 7, 4);
            }
        }
    }
}
