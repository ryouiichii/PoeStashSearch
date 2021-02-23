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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoeStashSearch.Api.Http;
using PoeStashSearch.Api.Models;
using PoeStashSearch.Data;
using PoeStashSearch.Data.Models;
using PoeStashSearch.Web.Events;
using PoeStashSearch.Web.Settings;
using ApiItem = PoeStashSearch.Api.Models.Item;
using DataItem = PoeStashSearch.Data.Models.Item;

namespace PoeStashSearch.Web.Services {
	class ItemRetrievalService : BackgroundService {
		public ItemRetrievalService(IDatabaseContext databaseContext, IEventBus eventBus, ILogger<ItemRetrievalService> logger, IMapper objectMapper, ISettingsManager settingsManager, IStashApiClient stashApiClient) {
			_databaseContext = databaseContext;
			_eventBus = eventBus;
			_logger = logger;
			_objectMapper = objectMapper;
			_settingsManager = settingsManager;
			_stashApiClient = stashApiClient;
		}

		#region BackgroundService Implementation
		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			var itemDefinitions = _databaseContext.Items.GetAllItemDefinitions();
			var settings = _settingsManager.GetAllSettings();

			_eventBus.Subscribe(nameof(SettingsManager), eventArgs => {
				if (eventArgs is SettingsManagerEventArgs settingsManagerEventArgs) {
					_logger.LogInformation("Received new settings");

					settings = settingsManagerEventArgs.Settings;
					_stashApiClient.SessionId = settingsManagerEventArgs.Settings.SessionId;
				}

				return Task.CompletedTask;
			}, nameof(ItemRetrievalService));

			//Wait until settings have been populated before starting main loop
			while (String.IsNullOrWhiteSpace(settings?.SessionId) && !stoppingToken.IsCancellationRequested) {
				_logger.LogDebug("Waiting for user to supply settings...");

				await Task.Delay(TimeSpan.FromSeconds(SERVICE_STARTUP_PAUSE_DURATION), stoppingToken);
			}

			_stashApiClient.SessionId = settings!.SessionId;

			while (!stoppingToken.IsCancellationRequested) {
				try {
					_logger.LogInformation("Fetching list of tabs");
					await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = "Fetching list of tabs" });

					var stashTabs = (await _stashApiClient.GetAllTabsAsync(settings.AccountName, settings.League, settings.Realm, stoppingToken)).Tabs?.Where(t => "NormalStash".Equals(t.Type) || "PremiumStash".Equals(t.Type) || "QuadStash".Equals(t.Type)).ToArray() ?? Array.Empty<StashTab>();
					var allDataItems = new List<DataItem>();

					for (var i = 0; i < stashTabs.Length; i++) {
						_logger.LogInformation("Fetching tab \"{TabName}\" ({TabIndex} of {TabCount})", stashTabs[i].Name, i + 1, stashTabs.Length);
						await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = $"Fetching tab \"{stashTabs[i].Name}\" ({i + 1} of {stashTabs.Length})" });

						var items = (await _stashApiClient.GetTabAsync(settings.AccountName, settings.League, settings.Realm, stashTabs[i].Index, stoppingToken)).Items ?? new List<ApiItem>();

						_logger.LogInformation("Processing items for tab \"{TabName}\" ({TabIndex} of {TabCount})", stashTabs[i].Name, i + 1, stashTabs.Length);
						await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = $"Processing items for tab \"{stashTabs[i].Name}\" ({i + 1} of {stashTabs.Length})" });

						foreach (var item in items) {
							var itemDefinition = itemDefinitions.FirstOrDefault(id => item.TypeLine?.Contains(id.Name) ?? false);
							if (!String.IsNullOrWhiteSpace(itemDefinition?.Name)) {
								_logger.LogDebug("Found matching item definition \"{ItemDefinitionName}\" for \"{ItemName} {ItemTypeLine}\"", itemDefinition.Name, item.Name, item.TypeLine);

								var dataItem = _objectMapper.Map<DataItem>(item) with { ItemDefinitionId = itemDefinition.Id, StashIndex = stashTabs[i].Index, StashName = stashTabs[i].Name! };
								var statDescriptions = _databaseContext.Stats.GetStatDescriptionsForItemDefinition(itemDefinition.Id);

								AddStatDescriptionsToItem(dataItem, item.CraftedMods ?? _emptyStringList, StatDescriptionType.Crafted, statDescriptions);
								AddStatDescriptionsToItem(dataItem, item.EnchantMods ?? _emptyStringList, StatDescriptionType.Enchant, statDescriptions);
								AddStatDescriptionsToItem(dataItem, item.ExplicitMods ?? _emptyStringList, StatDescriptionType.Explicit, statDescriptions);
								AddStatDescriptionsToItem(dataItem, item.FracturedMods ?? _emptyStringList, StatDescriptionType.Fractured, statDescriptions);
								AddStatDescriptionsToItem(dataItem, item.ImplicitMods ?? _emptyStringList, StatDescriptionType.Implicit, statDescriptions);

								allDataItems.Add(dataItem);
							} else {
								_logger.LogDebug("No matching item definition found for \"{ItemName} {ItemTypeLine}\"", item.Name, item.TypeLine);
							}
						}

						_logger.LogInformation("Successfully processed items for tab \"{TabName}\" ({TabIndex} of {TabCount})", stashTabs[i].Name, i + 1, stashTabs.Length);
						await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = $"Successfully processed items for tab \"{stashTabs[i].Name}\" ({i + 1} of {stashTabs.Length})" });

						await Task.Delay(TimeSpan.FromSeconds(settings.ItemRetrievalServicePauseBetweenTabs), stoppingToken);
					}

					_logger.LogInformation("Saving {ItemCount} processed items", allDataItems.Count);
					await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = $"Saving {allDataItems.Count} processed items" });

					var savedItemCount = _databaseContext.Items.SaveItems(true, allDataItems);

					_logger.LogInformation("Successfully saved {ItemCount} processed items from {TabCount} tabs, sleeping", savedItemCount, stashTabs.Length);
					await _eventBus.PublishAsync(nameof(ItemRetrievalService), new ItemRetrievalServiceEventArgs { Status = $"Successfully saved {savedItemCount} processed items from {stashTabs.Length} tabs, sleeping" });
				} catch (Exception ex) {
					_logger.LogError(ex, "Exception while attempting to retrieve and process items");
				}

				await Task.Delay(TimeSpan.FromSeconds(settings.ItemRetrievalServiceSleepDuration), stoppingToken);
			}
		}
		#endregion

		private void AddStatDescriptionsToItem(DataItem dataItem, IEnumerable<String> itemMods, StatDescriptionType itemModType, ICollection<StatDescription> statDescriptions) {
			foreach (var itemMod in itemMods) {
				foreach (var statDescription in statDescriptions) {
					var regexMatch = Regex.Match(itemMod, statDescription.RegularExpression);
					if (regexMatch.Success) {
						var numericValues = new decimal?[Constants.MaximumStatDescriptionNumericValueCount];

						if (regexMatch.Groups["number"].Success) {
							for (var j = 0; j < Constants.MaximumStatDescriptionNumericValueCount; j++) {
								numericValues[j] = regexMatch.Groups["number"].Captures.Count > j ? (decimal.TryParse(regexMatch.Groups["number"].Captures[j].Value, out var maybeNumericValue) ? (decimal?) maybeNumericValue : null) : null;
							}
						}

						dataItem.StatDescriptions.Add(new() {
							NumericValues = numericValues,
							NumericValuesAverage = numericValues.Average(),
							StatDescriptionId = statDescription.Id,
							StatDescriptionType = itemModType
						});

						break;
					}
				}
			}
		}

		private const int SERVICE_STARTUP_PAUSE_DURATION = 1;
		private readonly IDatabaseContext _databaseContext;
		private static readonly List<String> _emptyStringList = new List<String>();
		private readonly IEventBus _eventBus;
		private readonly ILogger<ItemRetrievalService> _logger;
		private readonly IMapper _objectMapper;
		private readonly ISettingsManager _settingsManager;
		private readonly IStashApiClient _stashApiClient;
	}
}
