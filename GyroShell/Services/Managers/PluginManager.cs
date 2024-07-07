﻿#region Copyright (BSD 3-Clause License)
/*
 * GyroShell - A modern, extensible, fast, and customizable shell platform.
 * Copyright 2022-2024 Pdawg
 *
 * Licensed under the BSD 3-Clause License.
 * SPDX-License-Identifier: BSD-3-Clause
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using GyroShell.Library.Interfaces;
using GyroShell.Library.Models.InternalData;
using GyroShell.Library.Services.Bridges;
using GyroShell.Library.Services.Environment;
using GyroShell.Library.Services.Managers;

namespace GyroShell.Services.Managers
{
    public class PluginManager : IPluginManager
    {
        private readonly Dictionary<AssemblyLoadContext, IPlugin> loadedPlugins = new Dictionary<AssemblyLoadContext, IPlugin>();

        private readonly ISettingsService m_settingsService;
        private readonly IPluginServiceBridge m_pluginServiceBridge;


        public PluginManager(ISettingsService settingsService, IPluginServiceBridge pluginServiceBridge)
        {
            m_settingsService = settingsService;

            m_pluginServiceBridge = pluginServiceBridge;

            foreach (string pluginName in m_settingsService.PluginsToLoad)
            {
                LoadAndRunPlugin(pluginName);
            }

            IsUnloadRestartPending = false;
        }

        private void OnPluginCreated(object sender, FileSystemEventArgs e)
        {
            GetPlugins();
        }

        private bool _isUnloadRestartPending;
        public bool IsUnloadRestartPending
        {
            get => _isUnloadRestartPending;
            set => _isUnloadRestartPending = value;
        }

        public void LoadAndRunPlugin(string pluginName)
        {
            foreach (string dllFile in Directory.GetFiles(m_settingsService.ModulesFolderPath, "*.dll").Where(file => Path.GetFileName(file) == pluginName))
            {
                try
                {
                    AssemblyLoadContext localPluginLoadContext = new AssemblyLoadContext($"PluginLoadContext_{pluginName}", true);
                    Assembly assembly = localPluginLoadContext.LoadFromAssemblyPath(Path.GetFullPath(dllFile));

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && type.Name == "PluginRoot")
                        {
                            IPlugin plugin = Activator.CreateInstance(type) as IPlugin;
                            plugin.Initialize(m_pluginServiceBridge.CreatePluginServiceProvider(plugin.PluginInformation.RequiredServices));
                            loadedPlugins[localPluginLoadContext] = plugin;
                            if (m_settingsService.SettingExists($"LoadPlugin_{pluginName}"))
                            {
                                m_settingsService.SetSetting($"LoadPlugin_{pluginName}", true);
                            }
                            else
                            {
                                m_settingsService.AddSetting($"LoadPlugin_{pluginName}", true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[-] PluginManager: Error loading and running plugin from {Path.GetFileName(dllFile)}: {ex.Message}");
                }
            }
        }

        public List<PluginUIModel> GetPlugins()
        {
            AssemblyLoadContext pluginLoadContext = new AssemblyLoadContext("PluginLoadContext", isCollectible: true);
            List<PluginUIModel> returnList = new List<PluginUIModel>();
            foreach (string dllFile in Directory.GetFiles(m_settingsService.ModulesFolderPath, "*.dll").Where(file => !file.Contains("GyroShell.Library")))
            {
                try
                {
                    Assembly assembly = pluginLoadContext.LoadFromAssemblyPath(Path.GetFullPath(dllFile));

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && type.Name == "PluginRoot")
                        {
                            IPlugin plugin = Activator.CreateInstance(type) as IPlugin; 

                            returnList.Add(
                                new PluginUIModel
                                {
                                    PluginName = plugin.PluginInformation.Name,
                                    FullName = Path.GetFileName(dllFile),
                                    Description = plugin.PluginInformation.Description,
                                    PublisherName = plugin.PluginInformation.Publisher,
                                    PluginVersion = "Version " + plugin.PluginInformation.Version,
                                    PluginId = plugin.PluginInformation.PluginId,
                                    IsLoaded = loadedPlugins.Any(kv => kv.Value.PluginInformation.Name == plugin.PluginInformation.Name)
                                });

                            GC.Collect();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[-] PluginManager: Error getting plugin from {dllFile}: {ex.Message}");
                    return new List<PluginUIModel>();
                }
            }
            List<string> pluginsToLoad = m_settingsService.PluginsToLoad;
            foreach (string pluginName in pluginsToLoad)
            {
                if (!returnList.Any(p => p.FullName == pluginName))
                {
                    m_settingsService.RemoveSetting("LoadPlugin_" + pluginName);
                }
            }
            pluginLoadContext.Unload();
            return returnList;
        }

        public void UnloadPlugin(string pluginName)
        {
            foreach (KeyValuePair<AssemblyLoadContext, IPlugin> plugin in loadedPlugins.Where(asm => asm.Key.Name.Contains(pluginName)))
            {
                AssemblyLoadContext pluginContext = plugin.Key;
                if (loadedPlugins.TryGetValue(pluginContext, out IPlugin pluginObj))
                {
                    pluginObj.Shutdown();
                    pluginContext.Unload();
                    IsUnloadRestartPending = true;
                    if (m_settingsService.SettingExists($"LoadPlugin_{pluginName}"))
                    {
                        m_settingsService.SetSetting($"LoadPlugin_{pluginName}", false);
                    }
                    else
                    {
                        m_settingsService.AddSetting($"LoadPlugin_{pluginName}", false);
                    }
                }
                else
                {
                    Debug.WriteLine($"[-] PluginManager: plugin '{plugin.Key.Name}' not found or already unloaded.");
                }
            }
        }
    }
}