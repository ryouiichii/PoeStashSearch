/*
Copyright (C) 2021 ryouiichii

This file is part of PoeStashSearch

PoeStashSearch is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

PoeStashSearch is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with PoeStashSearch.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PoeStashSearch.Data;
using PoeStashSearch.Web.Events;
using DataSettings = PoeStashSearch.Data.Models.Settings;

namespace PoeStashSearch.Web.Settings {
	public class SettingsManager : ISettingsManager {
		public SettingsManager(IDatabaseContext databaseContext, IEventBus eventBus, ILogger<SettingsManager> logger) {
			_databaseContext = databaseContext;
			_eventBus = eventBus;
			_logger = logger;
		}

		#region ISettingsManager Implementation
		public DataSettings? GetAllSettings() {
			return _databaseContext.Settings.GetAllSettings();
		}

		public async Task<bool> SaveSettingsAsync(DataSettings settings) {
			if (String.IsNullOrWhiteSpace(settings.AccountName) || settings.ItemRetrievalServicePauseBetweenTabs < 1 || settings.ItemRetrievalServiceSleepDuration < 1 || String.IsNullOrWhiteSpace(settings.League) || String.IsNullOrWhiteSpace(settings.Realm) || String.IsNullOrWhiteSpace(settings.SessionId)) {
				_logger.LogDebug("One or more invalid settings provided, not saving");

				return false;
			}

			_databaseContext.Settings.SaveSettings(settings);

			await _eventBus.PublishAsync(nameof(SettingsManager), new SettingsManagerEventArgs {
				Settings = settings
			});

			_logger.LogDebug("Successfully saved settings");

			return true;
		}
		#endregion

		private readonly IDatabaseContext _databaseContext;
		private readonly IEventBus _eventBus;
		private readonly ILogger<SettingsManager> _logger;
	}
}
