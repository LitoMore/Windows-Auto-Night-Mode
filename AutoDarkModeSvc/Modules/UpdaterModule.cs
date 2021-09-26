﻿using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class UpdaterModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool firstRun = true;
        public UpdaterModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) 
        {
            try
            {
                builder.LastUpdateLoad();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not load last update time:");
            }
        }
        public override string TimerAffinity => TimerName.IO;

        public override void Fire()
        {
            _ = Task.Run(() =>
            {
                Updater();
            });
        }

        private void Updater()
        {
            try
            {
                TimeSpan PollingCooldownTimeSpan = TimeSpan.FromDays(builder.Config.Updater.DaysBetweenUpdateCheck);
                DateTime nextUpdate = builder.UpdaterData.LastCheck.Add(PollingCooldownTimeSpan);
                if (DateTime.Now >= nextUpdate || firstRun)
                {
                    firstRun = false;
                    builder.UpdaterData.LastCheck = DateTime.Now;
                    _ = UpdateHandler.CheckNewVersion();
                    ApiResponse versionCheck = UpdateHandler.UpstreamResponse;

                    if (versionCheck.StatusCode == StatusCode.New)
                    {
                        ApiResponse autoUpdate = UpdateHandler.CanAutoInstall();
                        if (autoUpdate.StatusCode == StatusCode.New)
                        {
                            if (!builder.Config.Updater.Silent || !builder.Config.Updater.AutoInstall)
                            {
                                UpdateHandler.NotifyUpdateAvailable();
                            }
                            if (builder.Config.Updater.AutoInstall)
                            {
                                UpdateHandler.Update();
                            }
                        }
                        else if (autoUpdate.StatusCode == StatusCode.UnsupportedOperation || autoUpdate.StatusCode == StatusCode.Disabled)
                        {
                            UpdateHandler.NotifyUpdateAvailable();
                        }
                    }
                    try
                    {
                        builder.SaveUpdaterData();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "could not save update time:");
                    }
                }
                else
                {
                    Logger.Debug($"Next update check scheduled: {nextUpdate}");
                }
            } 
            catch (Exception ex)
            {
                Logger.Error(ex, "error while running update checker:");
            }
        }
    }
}
