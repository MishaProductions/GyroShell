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

namespace GyroShell.Library.ViewModels
{
    public sealed class StartupScreenViewModel
    {
        private readonly IEnvironmentInfoService m_envService;

        public StartupScreenViewModel(IEnvironmentInfoService envService) 
        {
            m_envService = envService;
        }

        public Version AppVersion
        {
            get => m_envService.AppVersion;
        }
    }
}
