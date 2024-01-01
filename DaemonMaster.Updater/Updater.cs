﻿/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMasterUpdater: Updater
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////
//  Parts of the code are from:     Copyright (c) 2012-2017 RBSoft (AutoUpdater.NET)
//  and running under the MIT licences! 
//
//  Permission is hereby granted, free of charge, to any person obtaining a
//  copy of this software and associated documentation files (the "Software"),
//  to deal in the Software without restriction, including without limitation
//  the rights to use, copy, modify, merge, publish, distribute, sublicense,
//  and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
/////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DaemonMaster.Updater.GitHub;
using DaemonMaster.Updater.Resources;
using DaemonMaster.Updater.Persistence;

namespace DaemonMaster.Updater
{
    public sealed class Updater
    {
        private static bool _working = false;


        /// <summary>
        /// GitHub repo path
        /// </summary>
        internal static string GitHubRepoPath { get; private set; }
        /// <summary>
        /// Accept prereleases for updates
        /// </summary>
        internal static bool AcceptPrerelease { get; private set; }
        /// <summary>
        /// GitHub access token
        /// </summary>
        internal static string AccessToken { get; private set; }

        /// <summary>
        /// Current version of the application
        /// </summary>
        internal static Version CurrentVersion { get; private set; }



        /// <summary>
        /// Name of the application to update (usually this will be set automatically, but you can set it manually if you want :))
        /// </summary>
        public static string AppName { get; set; }

        /// <summary>
        /// Name of the company (usually this will be set automatically, but you can set it manually if you want :))
        /// </summary>
        public static string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a custom persistence provider.
        /// </summary>
        public static IPersistenceProvider PersistenceProvider { get; set; }

        /// <summary>
        /// Last release from GitHub
        /// </summary>
        internal static GitHubApi.GitHubRelease LastGitHubRelease { get; private set; }

        /// <summary>
        /// StartAsync seeking for updates
        /// </summary>
        /// <param name="gitHubRepoPath">Path to the GitHub repo (like: https://github.com/myuser/myrepo )</param>
        /// <param name="showDialogs">Show error dialogs. (false = silent mode)</param>
        /// <param name="acceptPrerelease">If you want to accept prereleases as updates</param>
        /// <param name="accessToken">If you have an access token for the repo put it down here</param>
        /// <param name="myAssembly">If you want an other assembly the the calling assembly of the updater DLL file</param>
        public static async Task StartAsync(string gitHubRepoPath, bool showDialogs = false, bool acceptPrerelease = false, string accessToken = null, Assembly myAssembly = null)
        {
            //Update checker already running
            if (_working)
                return;

            _working = true;

            try
            {
                GitHubRepoPath = gitHubRepoPath;
                AcceptPrerelease = acceptPrerelease;
                AccessToken = accessToken;

                if (myAssembly == null)
                {
                    myAssembly = Assembly.GetEntryAssembly();

                    //Return if the caller is an unmanaged application (= null)
                    if (myAssembly == null)
                        return;
                }

                var fileVersion = FileVersionInfo.GetVersionInfo(myAssembly.Location);
                CompanyName = fileVersion.CompanyName;
                AppName = string.IsNullOrWhiteSpace(fileVersion.ProductName) ? myAssembly.GetName().Name : fileVersion.ProductName;
                CurrentVersion = new Version(fileVersion.ProductVersion);

                //Use default persistence provider when it is null
                if (PersistenceProvider == null)
                {
                    string registryLocation = !string.IsNullOrEmpty(CompanyName) ? $@"Software\{CompanyName}\{AppName}\Updater" : $@"Software\{AppName}\Updater";
                    PersistenceProvider = new RegistryUserPersistenceProvider(registryLocation);
                }
                
                LastGitHubRelease = await GitHubApi.GitHubGetLastReleaseAsync(GitHubRepoPath, AccessToken);


                Version skippedVersion = PersistenceProvider.GetSkippedVersion();
                if (LastGitHubRelease.Version > CurrentVersion && (skippedVersion == null || LastGitHubRelease.Version != skippedVersion))
                {
                    if (skippedVersion != null)
                        PersistenceProvider.SetSkippedVersion(null);

                    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    {
                        ShowUpdateWindow();
                    }
                    else
                    {
                        var thread = new Thread(ShowUpdateWindow);
                        thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                    }
                }
                else
                {
                    if (showDialogs)
                        MessageBox.Show(updaterLang.no_update_found_text, updaterLang.no_update_found, MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _working = false;
            }
            catch (Exception ex)
            {
                _working = false;

                if (showDialogs)
                    MessageBox.Show(ex.Message, updaterLang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ShowUpdateWindow()
        {
            var updateWindow = new UpdateWindow();
            updateWindow.ShowDialog();

            if (updateWindow.DialogResult.HasValue && updateWindow.DialogResult.Value)
            {
                CloseAllRunningInstances();
            }
        }

        /// <summary>
        /// Close all running instances with the same name and path 
        /// (this method is derived from AutoUpdater.NETs method 'Exit' / Copyright to RBSoft)
        /// </summary>
        private static void CloseAllRunningInstances(bool askForClose = false)
        {
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (string.IsNullOrWhiteSpace(currentProcess.MainModule?.FileName))
                    return;

                string fileName;
                try
                {
                    fileName = process?.MainModule?.FileName;
                    if(string.IsNullOrWhiteSpace(fileName))
                        continue;
                }
                catch(Win32Exception)
                {
                    //If the search fails here, then it is not one of the processes that were are looking for
                    continue;
                }

                if (fileName == currentProcess.MainModule.FileName)
                {
                    MessageBoxResult result;
                    if (askForClose)
                    {
                        result = MessageBox.Show(updaterLang.close_app_for_update, updaterLang.question, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                            return;
                    }

                    if (process.CloseMainWindow()) //Send a message to the process that he must close 
                    {
                        if (!process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds)) //Wait 10 seconds before the process get killed
                        {
                            result = MessageBox.Show(updaterLang.warning_process_will_be_killed, updaterLang.warning, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Cancel)
                                return;

                            process.Kill();
                        }
                    }
                }

                //Now we can close the updater itselfs
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher?.BeginInvoke(new Action(() => Application.Current.Shutdown()));
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
