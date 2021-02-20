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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PoeStashSearch.Data.Models;
using Serilog;

namespace PoeStashSearch.Console {
	class RePoeParser {
		public RePoeParser(ILogger logger) {
			_logger = logger;
		}

		public async Task<ICollection<ItemDefinition>> ParseBaseItemsAsync(String fullBaseItemsFilePath) {
			_logger.Debug("Attempting to parse base items from {PathToBaseItemsJson}", fullBaseItemsFilePath);

			using var baseItemsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(fullBaseItemsFilePath));
			var rootElement = baseItemsDocument.RootElement;

			var itemDefinitions = new List<ItemDefinition>();
			var itemDomainsOfInterest = new List<String> { "abyss_jewel", "affliction_jewel", "area", "item", "misc" };
			foreach (var currentItemDefinition in rootElement.EnumerateObject()) {
				if (currentItemDefinition.Value.TryGetProperty("domain", out var maybeDomain) && itemDomainsOfInterest.Contains(maybeDomain.GetString() ?? String.Empty)) {
					var itemClass = currentItemDefinition.Value.TryGetProperty("item_class", out var maybeItemClass) ? maybeItemClass.GetString() : String.Empty;
					var itemName = currentItemDefinition.Value.TryGetProperty("name", out var maybeName) ? maybeName.GetString() : String.Empty;
					var itemTags = currentItemDefinition.Value.TryGetProperty("tags", out var maybeTags) ? maybeTags.EnumerateArray().Select(t => t.GetString()).Distinct().ToList() : new List<String?>();
					//Add implicit(s) to tags
					itemTags.AddRange(currentItemDefinition.Value.TryGetProperty("implicits", out var maybeImplicits) ? maybeImplicits.EnumerateArray().Select(i => i.GetString() ?? String.Empty).Where(i => !String.IsNullOrWhiteSpace(i)) : new List<String>());

					//Filter duplicates e.g. "Avian Twins Talisman"
					if (!itemDefinitions.Any(i => i.Name.Equals(itemName!))) {
						//None of these will be null, so suppress warning here
						var itemDefinition = new ItemDefinition {
							Category = itemClass!,
							Domain = maybeDomain.GetString()!,
							Name = itemName!,
							Tags = itemTags!
						};

						itemDefinitions.Add(itemDefinition);

						_logger.Verbose("Found item definition with domain \"{Domain}\", name \"{Name}\", tags {Tags}", itemDefinition.Domain, itemDefinition.Name, itemDefinition.Tags);
					}
				}
			}

			_logger.Debug("Successfully completed parse of base items from {PathToBaseItemsJson}", fullBaseItemsFilePath);

			return itemDefinitions;
		}

		public async Task<ICollection<ClusterJewelStatDescription>> ParseClusterJewelsAsync(String fullClusterJewelsFilePath) {
			_logger.Debug("Attempting to parse cluster jewels from {PathToClusterJewelsJson}", fullClusterJewelsFilePath);

			using var clusterJewelsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(fullClusterJewelsFilePath));
			var rootElement = clusterJewelsDocument.RootElement;

			var clusterJewelStatDescriptions = new List<ClusterJewelStatDescription>();
			foreach (var currentClusterJewelCategory in rootElement.EnumerateObject()) {
				if (currentClusterJewelCategory.Value.TryGetProperty("name", out var maybeName) && currentClusterJewelCategory.Value.TryGetProperty("passive_skills", out var maybePassiveSkills)) {
					foreach (var currentPassiveSkill in maybePassiveSkills.EnumerateArray()) {
						if (currentPassiveSkill.TryGetProperty("stats", out var maybeStats) && currentPassiveSkill.TryGetProperty("tag", out var maybeTag)) {
							foreach (var currentStatDescription in maybeStats.EnumerateObject()) {
								if (currentStatDescription.Value.TryGetInt64(out var maybeStatValue)) {
									var clusterJewelStatDescription = new ClusterJewelStatDescription {
										Category = maybeName.GetString()!,
										StatDescriptionTextId = currentStatDescription.Name,
										StatDescriptionValue = maybeStatValue,
										Tag = maybeTag.GetString()!
									};

									clusterJewelStatDescriptions.Add(clusterJewelStatDescription);

									_logger.Verbose("Found cluster jewel with tag \"{Tag}\", stat description text id \"{StatDescription}\", stat description value \"{StatDescriptionValue}\" for category \"{Category}\"", clusterJewelStatDescription.Tag, clusterJewelStatDescription.StatDescriptionTextId, clusterJewelStatDescription.StatDescriptionValue, clusterJewelStatDescription.Category);
								}
							}
						}
					}
				}
			}

			_logger.Debug("Successfully completed parse of cluster jewels from {PathToClusterJewelsJson}", fullClusterJewelsFilePath);

			return clusterJewelStatDescriptions;
		}

		public async Task<ICollection<StatDescriptionItemTag>> ParseModsAsync(String fullModFilePath) {
			_logger.Debug("Attempting to parse mods from {PathToModsJson}", fullModFilePath);

			using var modDocument = JsonDocument.Parse(await File.ReadAllTextAsync(fullModFilePath));
			var rootModElement = modDocument.RootElement;

			var mods = new List<StatDescriptionItemTag>();
			foreach (var currentMod in rootModElement.EnumerateObject()) {
				if (currentMod.Value.TryGetProperty("generation_weights", out var maybeGenerationWeights) && currentMod.Value.TryGetProperty("spawn_weights", out var maybeSpawnWeights) && currentMod.Value.TryGetProperty("stats", out var maybeStats)) {
					var generationAndSpawnWeights = maybeGenerationWeights.EnumerateArray().Union(maybeSpawnWeights.EnumerateArray()).ToList();
					//If this is true, this mod is *not* an implicit
					var hasAnySpawnWeights = generationAndSpawnWeights.Any();

					foreach (var currentStat in maybeStats.EnumerateArray()) {
						if (hasAnySpawnWeights) {
							//Non-implicit mod
							foreach (var currentGenerationOrSpawnWeight in generationAndSpawnWeights) {
								var spawnWeightValue = currentGenerationOrSpawnWeight.TryGetProperty("weight", out var maybeSpawnWeightValue) ? maybeSpawnWeightValue.GetInt32() : 0;

								if (spawnWeightValue > 0) {
									var statDescriptionItemTag = new StatDescriptionItemTag {
										Domain = currentMod.Value.GetProperty("domain").GetString()!,
										ItemTagName = currentGenerationOrSpawnWeight.GetProperty("tag").GetString()!.Replace("_adjudicator", String.Empty).Replace("_basilisk", String.Empty).Replace("_crusader", String.Empty).Replace("_elder", String.Empty).Replace("_eyrie", String.Empty).Replace("_shaper", String.Empty),
										StatDescriptionTextId = currentStat.GetProperty("id").GetString()!
									};

									mods.Add(statDescriptionItemTag);

									_logger.Verbose("Found mod with domain \"{Domain}\", tag name \"{ItemTagName}\", stat description text id \"{StatDescriptionTextId}\"", statDescriptionItemTag.Domain, statDescriptionItemTag.ItemTagName, statDescriptionItemTag.StatDescriptionTextId);
								}
							}
						} else {
							//Implicit mod
							var statDescriptionItemTag = new StatDescriptionItemTag {
								Domain = currentMod.Value.GetProperty("domain").GetString()!,
								//Since this mod has no tags, just use "default" since all base items have this tag
								ItemTagName = "default",
								StatDescriptionTextId = currentStat.GetProperty("id").GetString()!
							};

							mods.Add(statDescriptionItemTag);

							_logger.Verbose("Found mod with domain \"{Domain}\", tag name \"{ItemTagName}\", stat description text id \"{StatDescriptionTextId}\"", statDescriptionItemTag.Domain, statDescriptionItemTag.ItemTagName, statDescriptionItemTag.StatDescriptionTextId);
						}
					}
				}
			}

			_logger.Debug("Successfully completed parse of mods from {PathToModsJson}", fullModFilePath);

			return mods;
		}

		public async Task<ICollection<StatDescription>> ParseStatDescriptionsAsync(String fullStatTranslationsFilePath) {
			_logger.Debug("Attempting to parse stat descriptions from {PathToStatTranslationsJson}", fullStatTranslationsFilePath);

			using var statTranslationsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(fullStatTranslationsFilePath));
			var rootStatTranslationsElement = statTranslationsDocument.RootElement;

			var statDescriptions = new List<StatDescription>();
			foreach (var currentStatTranslation in rootStatTranslationsElement.EnumerateArray()) {
				//Only grab stat translations that are actually displayed in game i.e. those without the "hidden" property
				if (currentStatTranslation.TryGetProperty("English", out var maybeEnglishStat) && currentStatTranslation.TryGetProperty("ids", out var maybeIds) && !currentStatTranslation.TryGetProperty("hidden", out _)) {
					var displayTextRegularExpressions = new List<(String DisplayText, int NumericValueCount, String RegularExpression)>();
					foreach (var currentEnglishStat in maybeEnglishStat.EnumerateArray()) {
						var formatString = currentEnglishStat.TryGetProperty("string", out var maybeFormatString) ? maybeFormatString.GetString() : String.Empty;
						if (!String.IsNullOrWhiteSpace(formatString) && currentEnglishStat.TryGetProperty("format", out var maybeFormat)) {
							var formatArray = maybeFormat.EnumerateArray().Select(f => f.GetString()).ToArray();
							var displayText = formatString;

							var numericPlaceholderMatches = Regex.Matches(formatString, @"\{(?<number>\d+)\}");
							if (numericPlaceholderMatches.Any()) {
								foreach (var numericPlaceholderMatch in numericPlaceholderMatches.Cast<Match>()) {
									if (numericPlaceholderMatch.Groups["number"].Success) {
										foreach (var numericPlaceholderMatchGroupCapture in numericPlaceholderMatch.Groups["number"].Captures.Cast<Capture>()) {
											displayText = displayText.Replace($"{{{numericPlaceholderMatchGroupCapture.Value}}}", formatArray[int.Parse(numericPlaceholderMatchGroupCapture.Value)]);
										}
									}
								}
							}

							//Need end-of-string marker so descriptions like "Adds # to # Physical Damage" don't match "Adds # to # Physical Damage to Spells"
							displayTextRegularExpressions.Add((displayText, numericPlaceholderMatches.Count, $"{displayText.Replace("+", @"\+").Replace("#", @"(?<number>\d+\.?\d*)")}$"));
						}
					}

					if (displayTextRegularExpressions.Any()) {
						foreach (var currentId in maybeIds.EnumerateArray()) {
							var currentStatDescriptions = displayTextRegularExpressions.Select(d => new StatDescription {
								DisplayText = d.DisplayText,
								NumericValueCount = d.NumericValueCount,
								RegularExpression = d.RegularExpression,
								TextId = currentId.GetString()!
							}).ToList();

							statDescriptions.AddRange(currentStatDescriptions);

							_logger.Verbose("Found stat descriptions {StatDescriptions}", currentStatDescriptions);
						}
					}
				}
			}

			_logger.Debug("Successfully completed parse of stat descriptions from {PathToStatTranslationsJson}", fullStatTranslationsFilePath);

			return statDescriptions;
		}

		private readonly ILogger _logger;
	}
}