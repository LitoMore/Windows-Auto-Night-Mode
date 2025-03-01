﻿#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeSvc.Interfaces;
using System;
using AutoDarkModeLib;
using AutoDarkModeLib.Interfaces;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.SwitchComponents
{
    abstract class BaseComponent<T> : ISwitchComponent
    {
        protected NLog.Logger Logger { get; private set; }
        protected GlobalState GlobalState { get; } = GlobalState.Instance();
        protected ISwitchComponentSettings<T> Settings { get; set; }
        protected ISwitchComponentSettings<T> SettingsBefore { get; set; }
        public bool Initialized { get; private set; }
        public BaseComponent()
        {
            Logger = NLog.LogManager.GetLogger(GetType().ToString());
        }
        public virtual int PriorityToLight { get; }
        public virtual int PriorityToDark { get; }
        public virtual HookPosition HookPosition { get; } = HookPosition.PostSync;
        public bool ForceSwitch { get; set; }
        public virtual bool Enabled
        {
            get { return Settings.Enabled; }
        }
        public void Switch(Theme newTheme, SwitchEventArgs e)
        {
            Logger.Debug($"switch invoked for {GetType().Name}");
            ForceSwitch = false;
            if (Enabled)
            {
                if (!Initialized)
                {
                    try
                    {
                        EnableHook();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"error while running enable hook for {GetType().Name}");
                    }
                }
                try
                {
                    HandleSwitch(newTheme, e);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"uncaught exception in component {GetType().Name}, source: {ex.Source}, message: ");
                }
            }
            else if (Initialized)
            {
                try
                {
                    DisableHook();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"error while running disable hook for {GetType().Name}");
                }
            }
        }

        public virtual void UpdateSettingsState(object newSettings)
        {
            if (newSettings is ISwitchComponentSettings<T> temp)
            {
                SettingsBefore = Settings;
                Settings = temp;
            }
            else
            {
                Logger.Error($"could not convert generic settings object to ${typeof(T)}, no settings update performed.");
            }
        }
        public virtual void EnableHook()
        {
            Logger.Debug($"running enable hook for {GetType().Name}");
            Initialized = true;
        }
        public virtual void DisableHook()
        {
            Logger.Debug($"running disable hook for {GetType().Name}");
            Initialized = false;
        }
        /// <summary>
        /// True when the component should be compatible with the ThemeHandler switching mode
        /// </summary>
        public abstract bool ThemeHandlerCompatibility { get; }

        /// <summary>
        /// Entrypoint, called when a component needs to be updated
        /// </summary>
        /// <param name="newTheme">the new theme to apply</param>
        /// <param name="e">the switch event args</param>
        protected abstract void HandleSwitch(Theme newTheme, SwitchEventArgs e);
        /// <summary>
        /// Determines whether the component needs to be triggered to update to the correct system state
        /// </summary>
        /// <returns>true if the component needs to be executed; false otherwise</returns>
        public abstract bool ComponentNeedsUpdate(Theme newTheme);
    }
}
