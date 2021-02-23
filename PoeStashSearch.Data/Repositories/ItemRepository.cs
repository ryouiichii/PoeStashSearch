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
using System.Text;
using PoeStashSearch.Data.Models;

namespace PoeStashSearch.Data.Repositories {
	public class ItemRepository : RepositoryBase, IItemRepository {
		public ItemRepository(String connectionString) : base(connectionString) {
		}

		#region IItemRepository Implementation
		public ICollection<ItemCategory> GetAllItemCategories() {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			var itemCategories = new List<ItemCategory>();
			command.CommandText = "SELECT Id, Name FROM ItemCategory ORDER BY Name ASC;";

			connection.Open();
			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				itemCategories.Add(new() {
					Id = (long)reader["Id"],
					Name = (String)reader["Name"]
				});
			}

			return itemCategories;
		}

		public ICollection<ItemDefinition> GetAllItemDefinitions() {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			var itemDefinitions = new List<ItemDefinition>();
			command.CommandText = "SELECT Id, Name FROM ItemDefinition;";

			connection.Open();
			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				itemDefinitions.Add(new() {
					Id = (long)reader["Id"],
					Name = (String)reader["Name"]
				});
			}

			return itemDefinitions;
		}

		public ICollection<Item> GetItemsWithStatDescriptions(ArmourCriteria? armourCriteria, InfluenceCriteria? influenceCriteria, ItemDefinitionCriteria? itemDefinitionCriteria, MiscellaneousCriteria? miscellaneousCriteria, RequirementCriteria? requirementCriteria, SocketCriteria? socketCriteria, WeaponCriteria? weaponCriteria, IEnumerable<StatDescriptionCriteria> statDescriptionCriterias) {
			return GetItemsWithStatDescriptions(armourCriteria, influenceCriteria, itemDefinitionCriteria, miscellaneousCriteria, requirementCriteria, socketCriteria, weaponCriteria, statDescriptionCriterias.ToArray());
		}

		public ICollection<Item> GetItemsWithStatDescriptions(ArmourCriteria? armourCriteria, InfluenceCriteria? influenceCriteria, ItemDefinitionCriteria? itemDefinitionCriteria, MiscellaneousCriteria? miscellaneousCriteria, RequirementCriteria? requirementCriteria, SocketCriteria? socketCriteria, WeaponCriteria? weaponCriteria, params StatDescriptionCriteria[] statDescriptionCriterias) {
			var nonNullStatDescriptionCriterias = statDescriptionCriterias.Where(s => s != null).ToArray();
			var searchCriteriaExist = armourCriteria != null || influenceCriteria != null || itemDefinitionCriteria != null || miscellaneousCriteria != null || nonNullStatDescriptionCriterias.Any() || requirementCriteria != null || socketCriteria != null || weaponCriteria != null;
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			var commandTextBuilder = new StringBuilder($"SELECT FullName, StashIndex, StashLocationX, StashLocationY, StashName FROM Item{(searchCriteriaExist ? " WHERE " : String.Empty)}");

			for (var i = 0; i < nonNullStatDescriptionCriterias.Length; i++) {
				commandTextBuilder.Append($"{(i > 0 ? " INTERSECT " : " Id IN (")}SELECT ItemId FROM ItemStatDescription WHERE StatDescriptionId IN ({String.Join(", ", nonNullStatDescriptionCriterias[i].StatDescriptionIds!)})");

				if (nonNullStatDescriptionCriterias[i].NumericValueMaximumAverage.HasValue) {
					commandTextBuilder.Append($" AND NumericValuesAverage <= $numericValuesAverageMax{i}");
					var numericValuesAverageMaximumParameter = command.CreateParameter();
					numericValuesAverageMaximumParameter.ParameterName = $"$numericValuesAverageMax{i}";
					numericValuesAverageMaximumParameter.Value = nonNullStatDescriptionCriterias[i].NumericValueMaximumAverage!.Value;

					command.Parameters.Add(numericValuesAverageMaximumParameter);
				}

				if (nonNullStatDescriptionCriterias[i].NumericValueMinimumAverage.HasValue) {
					commandTextBuilder.Append($" AND NumericValuesAverage >= $numericValuesAverageMin{i}");
					var numericValuesAverageMinimumParameter = command.CreateParameter();
					numericValuesAverageMinimumParameter.ParameterName = $"$numericValuesAverageMin{i}";
					numericValuesAverageMinimumParameter.Value = nonNullStatDescriptionCriterias[i].NumericValueMinimumAverage!.Value;

					command.Parameters.Add(numericValuesAverageMinimumParameter);

				}

				foreach (var numericValueRange in nonNullStatDescriptionCriterias[i].NumericValueRanges ?? new Dictionary<int, (decimal? MaximumValue, decimal? MinimumValue)>()) {
					if (numericValueRange.Value.MaximumValue.HasValue) {
						commandTextBuilder.Append($" AND NumericValue0{numericValueRange.Key} <= $numericValueMax{i}{numericValueRange.Key}");
						var numericValueRangeMaximumParameter = command.CreateParameter();
						numericValueRangeMaximumParameter.ParameterName = $"$numericValueMax{i}{numericValueRange.Key}";
						numericValueRangeMaximumParameter.Value = numericValueRange.Value.MaximumValue.Value;

						command.Parameters.Add(numericValueRangeMaximumParameter);
					}

					if (numericValueRange.Value.MinimumValue.HasValue) {
						commandTextBuilder.Append($" AND NumericValue0{numericValueRange.Key} >= $numericValueMin{i}{numericValueRange.Key}");
						var numericValueRangeMinimumParameter = command.CreateParameter();
						numericValueRangeMinimumParameter.ParameterName = $"$numericValueMin{i}{numericValueRange.Key}";
						numericValueRangeMinimumParameter.Value = numericValueRange.Value.MinimumValue.Value;

						command.Parameters.Add(numericValueRangeMinimumParameter);
					}
				}
			}

			if (nonNullStatDescriptionCriterias.Any()) {
				commandTextBuilder.Append(")");
			}

			if (influenceCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() ? " AND " : String.Empty;

				if (influenceCriteria.IsCrusaderInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsCrusaderInfluenced = $isCrusaderInfluenced");
					var isCrusaderInfluencedParameter = command.CreateParameter();
					isCrusaderInfluencedParameter.ParameterName = "$isCrusaderInfluenced";
					isCrusaderInfluencedParameter.Value = influenceCriteria.IsCrusaderInfluenced.Value;

					command.Parameters.Add(isCrusaderInfluencedParameter);

					commandTextPrefix = " AND ";
				}

				if (influenceCriteria.IsElderInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsElderInfluenced = $isElderInfluenced");
					var isElderInfluencedParameter = command.CreateParameter();
					isElderInfluencedParameter.ParameterName = "$isElderInfluenced";
					isElderInfluencedParameter.Value = influenceCriteria.IsElderInfluenced.Value;

					command.Parameters.Add(isElderInfluencedParameter);

					commandTextPrefix = " AND ";
				}

				if (influenceCriteria.IsHunterInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsHunterInfluenced = $isHunterInfluenced");
					var isHunterInfluencedParameter = command.CreateParameter();
					isHunterInfluencedParameter.ParameterName = "$isHunterInfluenced";
					isHunterInfluencedParameter.Value = influenceCriteria.IsHunterInfluenced.Value;

					command.Parameters.Add(isHunterInfluencedParameter);

					commandTextPrefix = " AND ";
				}

				if (influenceCriteria.IsRedeemerInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsRedeemerInfluenced = $isRedeemerInfluenced");
					var isRedeemerInfluencedParameter = command.CreateParameter();
					isRedeemerInfluencedParameter.ParameterName = "$isRedeemerInfluenced";
					isRedeemerInfluencedParameter.Value = influenceCriteria.IsRedeemerInfluenced.Value;

					command.Parameters.Add(isRedeemerInfluencedParameter);

					commandTextPrefix = " AND ";
				}

				if (influenceCriteria.IsShaperInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsShaperInfluenced = $isShaperInfluenced");
					var isShaperInfluencedParameter = command.CreateParameter();
					isShaperInfluencedParameter.ParameterName = "$isShaperInfluenced";
					isShaperInfluencedParameter.Value = influenceCriteria.IsShaperInfluenced.Value;

					command.Parameters.Add(isShaperInfluencedParameter);

					commandTextPrefix = " AND ";
				}

				if (influenceCriteria.IsWarlordInfluenced.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsWarlordInfluenced = $isWarlordInfluenced");
					var isWarlordInfluencedParameter = command.CreateParameter();
					isWarlordInfluencedParameter.ParameterName = "$isWarlordInfluenced";
					isWarlordInfluencedParameter.Value = influenceCriteria.IsWarlordInfluenced.Value;

					command.Parameters.Add(isWarlordInfluencedParameter);
				}
			}

			if (itemDefinitionCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null ? " AND " : String.Empty;

				if (itemDefinitionCriteria.Id.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}ItemDefinitionId = $itemDefinitionId");
					var itemDefinitionIdParameter = command.CreateParameter();
					itemDefinitionIdParameter.ParameterName = "$itemDefinitionId";
					itemDefinitionIdParameter.Value = itemDefinitionCriteria.Id.Value;

					command.Parameters.Add(itemDefinitionIdParameter);

					commandTextPrefix = " AND ";
				} else if (!String.IsNullOrWhiteSpace(itemDefinitionCriteria.Name)) {
					commandTextBuilder.Append($"{commandTextPrefix}FullName LIKE '%' || $itemName || '%'");
					var itemNameParameter = command.CreateParameter();
					itemNameParameter.ParameterName = "$itemName";
					itemNameParameter.Value = itemDefinitionCriteria.Name!;

					command.Parameters.Add(itemNameParameter);

					commandTextPrefix = " AND ";
				}

				if (itemDefinitionCriteria.ItemCategoryId.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}ItemDefinitionId IN (SELECT Id FROM ItemDefinition WHERE ItemCategoryId = $itemCategoryId)");
					var itemCategoryIdParameter = command.CreateParameter();
					itemCategoryIdParameter.ParameterName = "$itemCategoryId";
					itemCategoryIdParameter.Value = itemDefinitionCriteria.ItemCategoryId.Value;

					command.Parameters.Add(itemCategoryIdParameter);
				}
			}

			if (armourCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null || itemDefinitionCriteria != null ? " AND " : String.Empty;

				if (armourCriteria.ArmourMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(Armour IS NULL OR Armour <= $armourMaximum)");
					var armourMaximumParameter = command.CreateParameter();
					armourMaximumParameter.ParameterName = "$armourMaximum";
					armourMaximumParameter.Value = armourCriteria.ArmourMaximum.Value;

					command.Parameters.Add(armourMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.ArmourMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}Armour >= $armourMinimum");
					var armourMinimumParameter = command.CreateParameter();
					armourMinimumParameter.ParameterName = "$armourMinimum";
					armourMinimumParameter.Value = armourCriteria.ArmourMinimum.Value;

					command.Parameters.Add(armourMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.ChanceToBlockMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(ChanceToBlock IS NULL OR ChanceToBlock <= $chanceToBlockMaximum)");
					var chanceToBlockMaximumParameter = command.CreateParameter();
					chanceToBlockMaximumParameter.ParameterName = "$chanceToBlockMaximum";
					chanceToBlockMaximumParameter.Value = armourCriteria.ChanceToBlockMaximum.Value;

					command.Parameters.Add(chanceToBlockMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.ChanceToBlockMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}ChanceToBlock >= $chanceToBlockMinimum");
					var chanceToBlockMinimumParameter = command.CreateParameter();
					chanceToBlockMinimumParameter.ParameterName = "$chanceToBlockMinimum";
					chanceToBlockMinimumParameter.Value = armourCriteria.ChanceToBlockMinimum.Value;

					command.Parameters.Add(chanceToBlockMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.EnergyShieldMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(EnergyShield IS NULL OR EnergyShield <= $energyShieldMaximum)");
					var energyShieldMaximumParameter = command.CreateParameter();
					energyShieldMaximumParameter.ParameterName = "$energyShieldMaximum";
					energyShieldMaximumParameter.Value = armourCriteria.EnergyShieldMaximum.Value;

					command.Parameters.Add(energyShieldMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.EnergyShieldMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}EnergyShield >= $energyShieldMinimum");
					var energyShieldMinimumParameter = command.CreateParameter();
					energyShieldMinimumParameter.ParameterName = "$energyShieldMinimum";
					energyShieldMinimumParameter.Value = armourCriteria.EnergyShieldMinimum.Value;

					command.Parameters.Add(energyShieldMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.EvasionRatingMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(EvasionRating IS NULL OR EvasionRating <= $evasionRatingMaximum)");
					var evasionRatingMaximumParameter = command.CreateParameter();
					evasionRatingMaximumParameter.ParameterName = "$evasionRatingMaximum";
					evasionRatingMaximumParameter.Value = armourCriteria.EvasionRatingMaximum.Value;

					command.Parameters.Add(evasionRatingMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (armourCriteria.EvasionRatingMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}EvasionRating >= $evasionRatingMinimum");
					var evasionRatingMinimumParameter = command.CreateParameter();
					evasionRatingMinimumParameter.ParameterName = "$evasionRatingMinimum";
					evasionRatingMinimumParameter.Value = armourCriteria.EvasionRatingMinimum.Value;

					command.Parameters.Add(evasionRatingMinimumParameter);
				}
			}

			if (miscellaneousCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null || itemDefinitionCriteria != null || armourCriteria != null ? " AND " : String.Empty;

				if (miscellaneousCriteria.IsCorrupted.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsCorrupted = $isCorrupted");
					var isCorruptedParameter = command.CreateParameter();
					isCorruptedParameter.ParameterName = "$isCorrupted";
					isCorruptedParameter.Value = miscellaneousCriteria.IsCorrupted.Value;

					command.Parameters.Add(isCorruptedParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.IsIdentified.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}IsIdentified = $isIdentified");
					var isIdentifiedParameter = command.CreateParameter();
					isIdentifiedParameter.ParameterName = "$isIdentified";
					isIdentifiedParameter.Value = miscellaneousCriteria.IsIdentified.Value;

					command.Parameters.Add(isIdentifiedParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.ItemLevelMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(ItemLevel IS NULL OR ItemLevel <= $itemLevelMaximum)");
					var itemLevelMaximumParameter = command.CreateParameter();
					itemLevelMaximumParameter.ParameterName = "$itemLevelMaximum";
					itemLevelMaximumParameter.Value = miscellaneousCriteria.ItemLevelMaximum.Value;

					command.Parameters.Add(itemLevelMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.ItemLevelMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}ItemLevel >= $itemLevelMinimum");
					var itemLevelMinimumParameter = command.CreateParameter();
					itemLevelMinimumParameter.ParameterName = "$itemLevelMinimum";
					itemLevelMinimumParameter.Value = miscellaneousCriteria.ItemLevelMinimum.Value;

					command.Parameters.Add(itemLevelMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.QualityMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(Quality IS NULL OR Quality <= $qualityMaximum)");
					var qualityMaximumParameter = command.CreateParameter();
					qualityMaximumParameter.ParameterName = "$qualityMaximum";
					qualityMaximumParameter.Value = miscellaneousCriteria.QualityMaximum.Value;

					command.Parameters.Add(qualityMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.QualityMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}Quality >= $qualityMinimum");
					var qualityMinimumParameter = command.CreateParameter();
					qualityMinimumParameter.ParameterName = "$qualityMinimum";
					qualityMinimumParameter.Value = miscellaneousCriteria.QualityMinimum.Value;

					command.Parameters.Add(qualityMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (miscellaneousCriteria.Rarity.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}Rarity = $rarity");
					var rarityParameter = command.CreateParameter();
					rarityParameter.ParameterName = "$rarity";
					rarityParameter.Value = miscellaneousCriteria.Rarity.Value;

					command.Parameters.Add(rarityParameter);
				}
			}

			if (requirementCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null || itemDefinitionCriteria != null || armourCriteria != null || miscellaneousCriteria != null ? " AND " : String.Empty;

				if (requirementCriteria.DexterityMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(RequiredDexterity IS NULL OR RequiredDexterity <= $dexterityMaximum)");
					var dexterityMaximumParameter = command.CreateParameter();
					dexterityMaximumParameter.ParameterName = "$dexterityMaximum";
					dexterityMaximumParameter.Value = requirementCriteria.DexterityMaximum.Value;

					command.Parameters.Add(dexterityMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.DexterityMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}RequiredDexterity >= $dexterityMinimum");
					var dexterityMinimumParameter = command.CreateParameter();
					dexterityMinimumParameter.ParameterName = "$dexterityMinimum";
					dexterityMinimumParameter.Value = requirementCriteria.DexterityMinimum.Value;

					command.Parameters.Add(dexterityMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.IntelligenceMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(RequiredIntelligence IS NULL OR RequiredIntelligence <= $intelligenceMaximum)");
					var intelligenceMaximumParameter = command.CreateParameter();
					intelligenceMaximumParameter.ParameterName = "$intelligenceMaximum";
					intelligenceMaximumParameter.Value = requirementCriteria.IntelligenceMaximum.Value;

					command.Parameters.Add(intelligenceMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.IntelligenceMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}RequiredIntelligence >= $intelligenceMinimum");
					var intelligenceMinimumParameter = command.CreateParameter();
					intelligenceMinimumParameter.ParameterName = "$intelligenceMinimum";
					intelligenceMinimumParameter.Value = requirementCriteria.IntelligenceMinimum.Value;

					command.Parameters.Add(intelligenceMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.LevelMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(RequiredLevel IS NULL OR RequiredLevel <= $levelMaximum)");
					var levelMaximumParameter = command.CreateParameter();
					levelMaximumParameter.ParameterName = "$levelMaximum";
					levelMaximumParameter.Value = requirementCriteria.LevelMaximum.Value;

					command.Parameters.Add(levelMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.LevelMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}RequiredLevel >= $levelMinimum");
					var levelMinimumParameter = command.CreateParameter();
					levelMinimumParameter.ParameterName = "$levelMinimum";
					levelMinimumParameter.Value = requirementCriteria.LevelMinimum.Value;

					command.Parameters.Add(levelMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.StrengthMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(RequiredStrength IS NULL OR RequiredStrength <= $strengthMaximum)");
					var strengthMaximumParameter = command.CreateParameter();
					strengthMaximumParameter.ParameterName = "$strengthMaximum";
					strengthMaximumParameter.Value = requirementCriteria.StrengthMaximum.Value;

					command.Parameters.Add(strengthMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (requirementCriteria.StrengthMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}RequiredStrength >= $strengthMinimum");
					var strengthMinimumParameter = command.CreateParameter();
					strengthMinimumParameter.ParameterName = "$strengthMinimum";
					strengthMinimumParameter.Value = requirementCriteria.StrengthMinimum.Value;

					command.Parameters.Add(strengthMinimumParameter);
				}
			}

			if (socketCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null || itemDefinitionCriteria != null || armourCriteria != null || miscellaneousCriteria != null || requirementCriteria != null ? " AND " : String.Empty;

				if (socketCriteria.BlueLinksMinimum.HasValue || socketCriteria.GreenLinksMinimum.HasValue || socketCriteria.RedLinksMinimum.HasValue || socketCriteria.WhiteLinksMinimum.HasValue) {
					var blueLinkClause = socketCriteria.BlueLinksMinimum.HasValue ? "SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $blueLinksMinimum AND Colour = 'B')" : String.Empty;
					var greenLinkClause = socketCriteria.GreenLinksMinimum.HasValue ? $"{(socketCriteria.BlueLinksMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $greenLinksMinimum AND Colour = 'G')" : String.Empty;
					var redLinkClause = socketCriteria.RedLinksMinimum.HasValue ? $"{(socketCriteria.BlueLinksMinimum.HasValue || socketCriteria.GreenLinksMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $redLinksMinimum AND Colour = 'R')" : String.Empty;
					var whiteLinkClause = socketCriteria.WhiteLinksMinimum.HasValue ? $"{(socketCriteria.BlueLinksMinimum.HasValue || socketCriteria.GreenLinksMinimum.HasValue || socketCriteria.RedLinksMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $whiteLinksMinimum AND Colour = 'W')" : String.Empty;

					commandTextBuilder.Append($"{commandTextPrefix}Id IN (" +
					                          "  WITH ItemSocketGroups AS (" +
					                          "    SELECT ItemId, Colour, COUNT(*) AS SocketCount FROM ItemSocket GROUP BY ItemId, [Group], Colour" +
					                          $" ) {blueLinkClause}{greenLinkClause}{redLinkClause}{whiteLinkClause}" +
					                          ")");
					var blueLinksMinimumParameter = command.CreateParameter();
					var greenLinksMinimumParameter = command.CreateParameter();
					var redLinksMinimumParameter = command.CreateParameter();
					var whiteLinksMinimumParameter = command.CreateParameter();

					blueLinksMinimumParameter.ParameterName = "$blueLinksMinimum";
					greenLinksMinimumParameter.ParameterName = "$greenLinksMinimum";
					redLinksMinimumParameter.ParameterName = "$redLinksMinimum";
					whiteLinksMinimumParameter.ParameterName = "$whiteLinksMinimum";

					blueLinksMinimumParameter.Value = socketCriteria.BlueLinksMinimum ?? (Object)DBNull.Value;
					greenLinksMinimumParameter.Value = socketCriteria.GreenLinksMinimum ?? (Object)DBNull.Value;
					redLinksMinimumParameter.Value = socketCriteria.RedLinksMinimum ?? (Object)DBNull.Value;
					whiteLinksMinimumParameter.Value = socketCriteria.WhiteLinksMinimum ?? (Object)DBNull.Value;

					command.Parameters.Add(blueLinksMinimumParameter);
					command.Parameters.Add(greenLinksMinimumParameter);
					command.Parameters.Add(redLinksMinimumParameter);
					command.Parameters.Add(whiteLinksMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (socketCriteria.BlueSocketsMinimum.HasValue || socketCriteria.GreenSocketsMinimum.HasValue || socketCriteria.RedSocketsMinimum.HasValue || socketCriteria.WhiteSocketsMinimum.HasValue) {
					var blueSocketClause = socketCriteria.BlueSocketsMinimum.HasValue ? "SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $blueSocketsMinimum AND Colour = 'B')" : String.Empty;
					var greenSocketClause = socketCriteria.GreenSocketsMinimum.HasValue ? $"{(socketCriteria.BlueSocketsMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $greenSocketsMinimum AND Colour = 'G')" : String.Empty;
					var redSocketClause = socketCriteria.RedSocketsMinimum.HasValue ? $"{(socketCriteria.BlueSocketsMinimum.HasValue || socketCriteria.GreenSocketsMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $redSocketsMinimum AND Colour = 'R')" : String.Empty;
					var whiteSocketClause = socketCriteria.WhiteSocketsMinimum.HasValue ? $"{(socketCriteria.BlueSocketsMinimum.HasValue || socketCriteria.GreenSocketsMinimum.HasValue || socketCriteria.RedSocketsMinimum.HasValue ? " INTERSECT " : String.Empty)}SELECT ItemId FROM ItemSocketGroups WHERE (SocketCount >= $whiteSocketsMinimum AND Colour = 'W')" : String.Empty;

					commandTextBuilder.Append($"{commandTextPrefix}Id IN (" +
					                          "  WITH ItemSocketGroups AS (" +
					                          "    SELECT ItemId, Colour, COUNT(*) AS SocketCount FROM ItemSocket GROUP BY ItemId, Colour" +
					                          $" ) {blueSocketClause}{greenSocketClause}{redSocketClause}{whiteSocketClause}" +
					                          ")");
					var blueSocketsMinimumParameter = command.CreateParameter();
					var greenSocketsMinimumParameter = command.CreateParameter();
					var redSocketsMinimumParameter = command.CreateParameter();
					var whiteSocketsMinimumParameter = command.CreateParameter();

					blueSocketsMinimumParameter.ParameterName = "$blueSocketsMinimum";
					greenSocketsMinimumParameter.ParameterName = "$greenSocketsMinimum";
					redSocketsMinimumParameter.ParameterName = "$redSocketsMinimum";
					whiteSocketsMinimumParameter.ParameterName = "$whiteSocketsMinimum";

					blueSocketsMinimumParameter.Value = socketCriteria.BlueSocketsMinimum ?? (Object)DBNull.Value;
					greenSocketsMinimumParameter.Value = socketCriteria.GreenSocketsMinimum ?? (Object)DBNull.Value;
					redSocketsMinimumParameter.Value = socketCriteria.RedSocketsMinimum ?? (Object)DBNull.Value;
					whiteSocketsMinimumParameter.Value = socketCriteria.WhiteSocketsMinimum ?? (Object)DBNull.Value;

					command.Parameters.Add(blueSocketsMinimumParameter);
					command.Parameters.Add(greenSocketsMinimumParameter);
					command.Parameters.Add(redSocketsMinimumParameter);
					command.Parameters.Add(whiteSocketsMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (socketCriteria.LinksMaximum.HasValue || socketCriteria.LinksMinimum.HasValue) {
					var linkMaximumClause = socketCriteria.LinksMaximum.HasValue ? "SocketCount <= $linksMaximum" : String.Empty;
					var linkMinimumClause = socketCriteria.LinksMinimum.HasValue ? $"{(socketCriteria.LinksMaximum.HasValue ? " AND " : String.Empty)}SocketCount >= $linksMinimum" : String.Empty;

					commandTextBuilder.Append($"{commandTextPrefix}Id IN (" +
					                          "  WITH ItemSocketGroups AS (" +
					                          "    SELECT ItemId, COUNT(*) AS SocketCount FROM ItemSocket GROUP BY ItemId, [Group]" +
					                          $" ) SELECT ItemId FROM ItemSocketGroups WHERE {linkMaximumClause}{linkMinimumClause}" +
					                          ")");
					var linksMaximumParameter = command.CreateParameter();
					var linksMinimumParameter = command.CreateParameter();

					linksMaximumParameter.ParameterName = "$linksMaximum";
					linksMinimumParameter.ParameterName = "$linksMinimum";

					linksMaximumParameter.Value = socketCriteria.LinksMaximum ?? (Object)DBNull.Value;
					linksMinimumParameter.Value = socketCriteria.LinksMinimum ?? (Object)DBNull.Value;

					command.Parameters.Add(linksMaximumParameter);
					command.Parameters.Add(linksMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (socketCriteria.SocketsMaximum.HasValue || socketCriteria.SocketsMinimum.HasValue) {
					var socketMaximumClause = socketCriteria.SocketsMaximum.HasValue ? "SocketCount <= $socketsMaximum" : String.Empty;
					var socketMinimumClause = socketCriteria.SocketsMinimum.HasValue ? $"{(socketCriteria.SocketsMaximum.HasValue ? " AND " : String.Empty)}SocketCount >= $socketsMinimum" : String.Empty;

					commandTextBuilder.Append($"{commandTextPrefix}Id IN (" +
					                          "  WITH ItemSocketGroups AS (" +
					                          "    SELECT ItemId, COUNT(*) AS SocketCount FROM ItemSocket GROUP BY ItemId" +
					                          $" ) SELECT ItemId FROM ItemSocketGroups WHERE {socketMaximumClause}{socketMinimumClause}" +
					                          ")");
					var socketsMaximumParameter = command.CreateParameter();
					var socketsMinimumParameter = command.CreateParameter();

					socketsMaximumParameter.ParameterName = "$socketsMaximum";
					socketsMinimumParameter.ParameterName = "$socketsMinimum";

					socketsMaximumParameter.Value = socketCriteria.SocketsMaximum ?? (Object)DBNull.Value;
					socketsMinimumParameter.Value = socketCriteria.SocketsMinimum ?? (Object)DBNull.Value;

					command.Parameters.Add(socketsMaximumParameter);
					command.Parameters.Add(socketsMinimumParameter);
				}
			}

			if (weaponCriteria != null) {
				var commandTextPrefix = nonNullStatDescriptionCriterias.Any() || influenceCriteria != null || itemDefinitionCriteria != null || armourCriteria != null || miscellaneousCriteria != null || requirementCriteria != null || socketCriteria != null ? " AND " : String.Empty;

				if (weaponCriteria.AttacksPerSecondMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(AttacksPerSecond IS NULL OR AttacksPerSecond <= $attacksPerSecondMaximum)");
					var attacksPerSecondMaximumParameter = command.CreateParameter();
					attacksPerSecondMaximumParameter.ParameterName = "$attacksPerSecondMaximum";
					attacksPerSecondMaximumParameter.Value = weaponCriteria.AttacksPerSecondMaximum.Value;

					command.Parameters.Add(attacksPerSecondMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.AttacksPerSecondMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}AttacksPerSecond >= $attacksPerSecondMinimum");
					var attacksPerSecondMinimumParameter = command.CreateParameter();
					attacksPerSecondMinimumParameter.ParameterName = "$attacksPerSecondMinimum";
					attacksPerSecondMinimumParameter.Value = weaponCriteria.AttacksPerSecondMinimum.Value;

					command.Parameters.Add(attacksPerSecondMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.CriticalStrikeChanceMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(CriticalStrikeChance IS NULL OR CriticalStrikeChance <= $criticalStrikeChanceMaximum)");
					var criticalStrikeChanceMaximumParameter = command.CreateParameter();
					criticalStrikeChanceMaximumParameter.ParameterName = "$criticalStrikeChanceMaximum";
					criticalStrikeChanceMaximumParameter.Value = weaponCriteria.CriticalStrikeChanceMaximum.Value;

					command.Parameters.Add(criticalStrikeChanceMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.CriticalStrikeChanceMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}CriticalStrikeChance >= $criticalStrikeChanceMinimum");
					var criticalStrikeChanceMinimumParameter = command.CreateParameter();
					criticalStrikeChanceMinimumParameter.ParameterName = "$criticalStrikeChanceMinimum";
					criticalStrikeChanceMinimumParameter.Value = weaponCriteria.CriticalStrikeChanceMinimum.Value;

					command.Parameters.Add(criticalStrikeChanceMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.PhysicalDamageBottomEndMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(PhysicalDamageMinimum IS NULL OR PhysicalDamageMinimum <= $physicalDamageBottomEndMaximum)");
					var physicalDamageBottomEndMaximumParameter = command.CreateParameter();
					physicalDamageBottomEndMaximumParameter.ParameterName = "$physicalDamageBottomEndMaximum";
					physicalDamageBottomEndMaximumParameter.Value = weaponCriteria.PhysicalDamageBottomEndMaximum.Value;

					command.Parameters.Add(physicalDamageBottomEndMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.PhysicalDamageBottomEndMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}PhysicalDamageMinimum >= $physicalDamageBottomEndMinimum");
					var physicalDamageBottomEndMinimumParameter = command.CreateParameter();
					physicalDamageBottomEndMinimumParameter.ParameterName = "$physicalDamageBottomEndMinimum";
					physicalDamageBottomEndMinimumParameter.Value = weaponCriteria.PhysicalDamageBottomEndMinimum.Value;

					command.Parameters.Add(physicalDamageBottomEndMinimumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.PhysicalDamageTopEndMaximum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}(PhysicalDamageMaximum IS NULL OR PhysicalDamageMaximum <= $physicalDamageTopEndMaximum)");
					var physicalDamageTopEndMaximumParameter = command.CreateParameter();
					physicalDamageTopEndMaximumParameter.ParameterName = "$physicalDamageTopEndMaximum";
					physicalDamageTopEndMaximumParameter.Value = weaponCriteria.PhysicalDamageTopEndMaximum.Value;

					command.Parameters.Add(physicalDamageTopEndMaximumParameter);

					commandTextPrefix = " AND ";
				}

				if (weaponCriteria.PhysicalDamageTopEndMinimum.HasValue) {
					commandTextBuilder.Append($"{commandTextPrefix}PhysicalDamageMaximum >= $physicalDamageTopEndMinimum");
					var physicalDamageTopEndMinimumParameter = command.CreateParameter();
					physicalDamageTopEndMinimumParameter.ParameterName = "$physicalDamageTopEndMinimum";
					physicalDamageTopEndMinimumParameter.Value = weaponCriteria.PhysicalDamageTopEndMinimum.Value;

					command.Parameters.Add(physicalDamageTopEndMinimumParameter);
				}
			}

			commandTextBuilder.Append(";");

			var items = new List<Item>();
			command.CommandText = commandTextBuilder.ToString();

			connection.Open();

			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				items.Add(new() {
					FullName = (String)reader["FullName"],
					StashIndex = (long)reader["StashIndex"],
					StashLocationX = (long)reader["StashLocationX"],
					StashLocationY = (long)reader["StashLocationY"],
					StashName = (String)reader["StashName"]
				});
			}

			return items;
		}

		public int SaveItemCategories(IEnumerable<String> itemCategories) {
			return SaveItemCategories(itemCategories.ToArray());
		}

		public int SaveItemCategories(params String[] itemCategories) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "INSERT INTO ItemCategory (Name) VALUES ($name);";
			var nameParameter = command.CreateParameter();

			nameParameter.ParameterName = "$name";

			command.Parameters.Add(nameParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var itemCategory in itemCategories) {
				nameParameter.Value = itemCategory;

				command.ExecuteNonQuery();
			}

			transaction.Commit();

			return itemCategories.Length;
		}

		public int SaveItemDefinitions(IEnumerable<ItemDefinition> itemDefinitions) {
			return SaveItemDefinitions(itemDefinitions.ToArray());
		}

		public int SaveItemDefinitions(params ItemDefinition[] itemDefinitions) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "WITH CurrentItemCategory AS (SELECT Id FROM ItemCategory WHERE Name = $itemCategory) INSERT INTO ItemDefinition (Domain, ItemCategoryId, Name) SELECT $domain, Id, $name FROM CurrentItemCategory;";
			var domainParameter = command.CreateParameter();
			var itemCategoryParameter = command.CreateParameter();
			var nameParameter = command.CreateParameter();

			domainParameter.ParameterName = "$domain";
			itemCategoryParameter.ParameterName = "$itemCategory";
			nameParameter.ParameterName = "$name";

			command.Parameters.Add(domainParameter);
			command.Parameters.Add(itemCategoryParameter);
			command.Parameters.Add(nameParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var itemDefinition in itemDefinitions) {
				domainParameter.Value = itemDefinition.Domain;
				itemCategoryParameter.Value = itemDefinition.Category;
				nameParameter.Value = itemDefinition.Name;

				command.ExecuteNonQuery();

				using var innerCommand = connection.CreateCommand();

				innerCommand.CommandText = "WITH CurrentItemDefinition AS (SELECT Id FROM ItemDefinition WHERE Name = $itemDefinition), CurrentItemTag AS (SELECT Id FROM ItemTag WHERE Name = $itemTag) INSERT INTO ItemDefinitionItemTag (ItemDefinitionId, ItemTagId) SELECT CurrentItemDefinition.Id, CurrentItemTag.Id FROM CurrentItemDefinition, CurrentItemTag;";
				var itemDefinitionParameter = innerCommand.CreateParameter();
				var itemTagParameter = innerCommand.CreateParameter();

				itemDefinitionParameter.ParameterName = "$itemDefinition";
				itemTagParameter.ParameterName = "$itemTag";

				innerCommand.Parameters.Add(itemDefinitionParameter);
				innerCommand.Parameters.Add(itemTagParameter);

				innerCommand.Transaction = transaction;
				itemDefinitionParameter.Value = itemDefinition.Name;
				foreach (var itemTag in itemDefinition.Tags) {
					itemTagParameter.Value = itemTag;

					innerCommand.ExecuteNonQuery();
				}
			}

			transaction.Commit();

			return itemDefinitions.Length;
		}

		public int SaveItems(bool deleteExistingItems, IEnumerable<Item> items) {
			return SaveItems(deleteExistingItems, items.ToArray());
		}

		public int SaveItems(bool deleteExistingItems, params Item[] items) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "INSERT INTO Item (Id, Armour, AttacksPerSecond, ChanceToBlock, CriticalStrikeChance, EnergyShield, EvasionRating, Experience, FullName, IsCorrupted, IsCrusaderInfluenced, IsElderInfluenced, IsHunterInfluenced, IsIdentified, IsRedeemerInfluenced, IsShaperInfluenced, IsWarlordInfluenced, ItemDefinitionId, ItemLevel, PhysicalDamageMaximum, PhysicalDamageMinimum, Quality, Rarity, RequiredDexterity, RequiredIntelligence, RequiredLevel, RequiredStrength, StashIndex, StashLocationX, StashLocationY, StashName, WeaponRange) VALUES ($id, $armour, $attacksPerSecond, $chanceToBlock, $criticalStrikeChance, $energyShield, $evasionRating, $experience, $fullName, $isCorrupted, $isCrusaderInfluenced, $isElderInfluenced, $isHunterInfluenced, $isIdentified, $isRedeemerInfluenced, $isShaperInfluenced, $isWarlordInfluenced, $itemDefinitionId, $itemLevel, $physicalDamageMaximum, $physicalDamageMinimum, $quality, $rarity, $requiredDexterity, $requiredIntelligence, $requiredLevel, $requiredStrength, $stashIndex, $stashLocationX, $stashLocationY, $stashName, $weaponRange);";
			var armourParameter = command.CreateParameter();
			var attacksPerSecondParameter = command.CreateParameter();
			var chanceToBlockParameter = command.CreateParameter();
			var criticalStrikeChanceParameter = command.CreateParameter();
			var energyShieldParameter = command.CreateParameter();
			var evasionRatingParameter = command.CreateParameter();
			var experienceParameter = command.CreateParameter();
			var fullNameParameter = command.CreateParameter();
			var idParameter = command.CreateParameter();
			var isCorruptedParameter = command.CreateParameter();
			var isCrusaderInfluencedParameter = command.CreateParameter();
			var isElderInfluencedParameter = command.CreateParameter();
			var isHunterInfluencedParameter = command.CreateParameter();
			var isIdentifiedParameter = command.CreateParameter();
			var isRedeemerInfluencedParameter = command.CreateParameter();
			var isShaperInfluencedParameter = command.CreateParameter();
			var isWarlordInfluencedParameter = command.CreateParameter();
			var itemDefinitionIdParameter = command.CreateParameter();
			var itemLevelParameter = command.CreateParameter();
			var physicalDamageMaximumParameter = command.CreateParameter();
			var physicalDamageMinimumParameter = command.CreateParameter();
			var qualityParameter = command.CreateParameter();
			var rarityParameter = command.CreateParameter();
			var requiredDexterityParameter = command.CreateParameter();
			var requiredIntelligenceParameter = command.CreateParameter();
			var requiredLevelParameter = command.CreateParameter();
			var requiredStrengthParameter = command.CreateParameter();
			var stashIndexParameter = command.CreateParameter();
			var stashLocationX = command.CreateParameter();
			var stashLocationY = command.CreateParameter();
			var stashNameParameter = command.CreateParameter();
			var weaponRangeParameter = command.CreateParameter();

			armourParameter.ParameterName = "$armour";
			attacksPerSecondParameter.ParameterName = "$attacksPerSecond";
			chanceToBlockParameter.ParameterName = "$chanceToBlock";
			criticalStrikeChanceParameter.ParameterName = "$criticalStrikeChance";
			energyShieldParameter.ParameterName = "$energyShield";
			evasionRatingParameter.ParameterName = "$evasionRating";
			experienceParameter.ParameterName = "$experience";
			fullNameParameter.ParameterName = "$fullName";
			idParameter.ParameterName = "$id";
			isCorruptedParameter.ParameterName = "$isCorrupted";
			isCrusaderInfluencedParameter.ParameterName = "$isCrusaderInfluenced";
			isElderInfluencedParameter.ParameterName = "$isElderInfluenced";
			isHunterInfluencedParameter.ParameterName = "$isHunterInfluenced";
			isIdentifiedParameter.ParameterName = "$isIdentified";
			isRedeemerInfluencedParameter.ParameterName = "$isRedeemerInfluenced";
			isShaperInfluencedParameter.ParameterName = "$isShaperInfluenced";
			isWarlordInfluencedParameter.ParameterName = "$isWarlordInfluenced";
			itemDefinitionIdParameter.ParameterName = "$itemDefinitionId";
			itemLevelParameter.ParameterName = "$itemLevel";
			physicalDamageMaximumParameter.ParameterName = "$physicalDamageMaximum";
			physicalDamageMinimumParameter.ParameterName = "$physicalDamageMinimum";
			qualityParameter.ParameterName = "$quality";
			rarityParameter.ParameterName = "$rarity";
			requiredDexterityParameter.ParameterName = "$requiredDexterity";
			requiredIntelligenceParameter.ParameterName = "$requiredIntelligence";
			requiredLevelParameter.ParameterName = "$requiredLevel";
			requiredStrengthParameter.ParameterName = "$requiredStrength";
			stashIndexParameter.ParameterName = "$stashIndex";
			stashLocationX.ParameterName = "$stashLocationX";
			stashLocationY.ParameterName = "$stashLocationY";
			stashNameParameter.ParameterName = "$stashName";
			weaponRangeParameter.ParameterName = "$weaponRange";

			command.Parameters.Add(armourParameter);
			command.Parameters.Add(attacksPerSecondParameter);
			command.Parameters.Add(chanceToBlockParameter);
			command.Parameters.Add(criticalStrikeChanceParameter);
			command.Parameters.Add(energyShieldParameter);
			command.Parameters.Add(evasionRatingParameter);
			command.Parameters.Add(experienceParameter);
			command.Parameters.Add(idParameter);
			command.Parameters.Add(fullNameParameter);
			command.Parameters.Add(isCorruptedParameter);
			command.Parameters.Add(isCrusaderInfluencedParameter);
			command.Parameters.Add(isElderInfluencedParameter);
			command.Parameters.Add(isHunterInfluencedParameter);
			command.Parameters.Add(isIdentifiedParameter);
			command.Parameters.Add(isRedeemerInfluencedParameter);
			command.Parameters.Add(isShaperInfluencedParameter);
			command.Parameters.Add(isWarlordInfluencedParameter);
			command.Parameters.Add(itemDefinitionIdParameter);
			command.Parameters.Add(itemLevelParameter);
			command.Parameters.Add(physicalDamageMaximumParameter);
			command.Parameters.Add(physicalDamageMinimumParameter);
			command.Parameters.Add(qualityParameter);
			command.Parameters.Add(rarityParameter);
			command.Parameters.Add(requiredDexterityParameter);
			command.Parameters.Add(requiredIntelligenceParameter);
			command.Parameters.Add(requiredLevelParameter);
			command.Parameters.Add(requiredStrengthParameter);
			command.Parameters.Add(stashIndexParameter);
			command.Parameters.Add(stashLocationX);
			command.Parameters.Add(stashLocationY);
			command.Parameters.Add(stashNameParameter);
			command.Parameters.Add(weaponRangeParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;

			if (deleteExistingItems) {
				using var deleteCommand = connection.CreateCommand();
				deleteCommand.CommandText = "DELETE FROM Item;";
				deleteCommand.Transaction = transaction;

				deleteCommand.ExecuteNonQuery();
			}

			foreach (var item in items) {
				armourParameter.Value = item.Armour ?? (Object)DBNull.Value;
				attacksPerSecondParameter.Value = item.AttacksPerSecond ?? (Object)DBNull.Value;
				chanceToBlockParameter.Value = item.ChanceToBlock ?? (Object)DBNull.Value;
				criticalStrikeChanceParameter.Value = item.CriticalStrikeChance ?? (Object)DBNull.Value;
				energyShieldParameter.Value = item.EnergyShield ?? (Object)DBNull.Value;
				evasionRatingParameter.Value = item.EvasionRating ?? (Object)DBNull.Value;
				experienceParameter.Value = item.Experience ?? (Object)DBNull.Value;
				fullNameParameter.Value = item.FullName;
				idParameter.Value = item.Id;
				isCorruptedParameter.Value = item.IsCorrupted;
				isCrusaderInfluencedParameter.Value = item.Influences?.Crusader ?? false;
				isElderInfluencedParameter.Value = item.Influences?.Elder ?? false;
				isHunterInfluencedParameter.Value = item.Influences?.Hunter ?? false;
				isIdentifiedParameter.Value = item.IsIdentified;
				isRedeemerInfluencedParameter.Value = item.Influences?.Redeemer ?? false;
				isShaperInfluencedParameter.Value = item.Influences?.Shaper ?? false;
				isWarlordInfluencedParameter.Value = item.Influences?.Warlord ?? false;
				itemDefinitionIdParameter.Value = item.ItemDefinitionId;
				itemLevelParameter.Value = item.ItemLevel;
				physicalDamageMaximumParameter.Value = item.PhysicalDamageMaximum ?? (Object)DBNull.Value;
				physicalDamageMinimumParameter.Value = item.PhysicalDamageMinimum ?? (Object)DBNull.Value;
				qualityParameter.Value = item.Quality ?? (Object)DBNull.Value;
				rarityParameter.Value = item.Rarity;
				requiredDexterityParameter.Value = item.RequiredDexterity ?? (Object)DBNull.Value;
				requiredIntelligenceParameter.Value = item.RequiredIntelligence ?? (Object)DBNull.Value;
				requiredLevelParameter.Value = item.RequiredLevel ?? (Object)DBNull.Value;
				requiredStrengthParameter.Value = item.RequiredStrength ?? (Object)DBNull.Value;
				stashIndexParameter.Value = item.StashIndex;
				stashLocationX.Value = item.StashLocationX;
				stashLocationY.Value = item.StashLocationY;
				stashNameParameter.Value = item.StashName;
				weaponRangeParameter.Value = item.WeaponRange ?? (Object)DBNull.Value;

				command.ExecuteNonQuery();

				using var itemSocketCommand = connection.CreateCommand();

				itemSocketCommand.CommandText = "INSERT INTO ItemSocket (ItemId, Colour, [Group]) VALUES ($id, $colour, $group);";
				var colourParameter = itemSocketCommand.CreateParameter();
				var groupParameter = itemSocketCommand.CreateParameter();

				colourParameter.ParameterName = "$colour";
				groupParameter.ParameterName = "$group";

				itemSocketCommand.Parameters.Add(colourParameter);
				itemSocketCommand.Parameters.Add(groupParameter);
				itemSocketCommand.Parameters.Add(idParameter);

				itemSocketCommand.Transaction = transaction;
				foreach (var itemSocket in item.Sockets ?? new List<ItemSocket>()) {
					colourParameter.Value = itemSocket.Colour;
					groupParameter.Value = itemSocket.Group;

					itemSocketCommand.ExecuteNonQuery();
				}

				using var itemStatDescriptionCommand = connection.CreateCommand();
				
				itemStatDescriptionCommand.CommandText = "INSERT INTO ItemStatDescription (ItemId, StatDescriptionId, NumericValue01, NumericValue02, NumericValue03, NumericValue04, NumericValue05, NumericValue06, NumericValue07, NumericValuesAverage, StatDescriptionTypeId) VALUES ($id, $statDescriptionId, $numericValue01, $numericValue02, $numericValue03, $numericValue04, $numericValue05, $numericValue06, $numericValue07, $numericValuesAverage, $statDescriptionTypeId);";
				var numericValue01Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue02Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue03Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue04Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue05Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue06Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValue07Parameter = itemStatDescriptionCommand.CreateParameter();
				var numericValuesAverageParameter = itemStatDescriptionCommand.CreateParameter();
				var statDescriptionIdParameter = itemStatDescriptionCommand.CreateParameter();
				var statDescriptionTypeIdParameter = itemStatDescriptionCommand.CreateParameter();

				numericValue01Parameter.ParameterName = "$numericValue01";
				numericValue02Parameter.ParameterName = "$numericValue02";
				numericValue03Parameter.ParameterName = "$numericValue03";
				numericValue04Parameter.ParameterName = "$numericValue04";
				numericValue05Parameter.ParameterName = "$numericValue05";
				numericValue06Parameter.ParameterName = "$numericValue06";
				numericValue07Parameter.ParameterName = "$numericValue07";
				numericValuesAverageParameter.ParameterName = "$numericValuesAverage";
				statDescriptionIdParameter.ParameterName = "$statDescriptionId";
				statDescriptionTypeIdParameter.ParameterName = "$statDescriptionTypeId";

				itemStatDescriptionCommand.Parameters.Add(idParameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue01Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue02Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue03Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue04Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue05Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue06Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValue07Parameter);
				itemStatDescriptionCommand.Parameters.Add(numericValuesAverageParameter);
				itemStatDescriptionCommand.Parameters.Add(statDescriptionIdParameter);
				itemStatDescriptionCommand.Parameters.Add(statDescriptionTypeIdParameter);

				itemStatDescriptionCommand.Transaction = transaction;
				foreach (var itemStatDescription in item.StatDescriptions) {
					numericValue01Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(0) ?? (Object)DBNull.Value;
					numericValue02Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(1) ?? (Object)DBNull.Value;
					numericValue03Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(2) ?? (Object)DBNull.Value;
					numericValue04Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(3) ?? (Object)DBNull.Value;
					numericValue05Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(4) ?? (Object)DBNull.Value;
					numericValue06Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(5) ?? (Object)DBNull.Value;
					numericValue07Parameter.Value = itemStatDescription.NumericValues.ElementAtOrDefault(6) ?? (Object)DBNull.Value;
					numericValuesAverageParameter.Value = itemStatDescription.NumericValuesAverage ?? (Object)DBNull.Value;
					statDescriptionIdParameter.Value = itemStatDescription.StatDescriptionId;
					statDescriptionTypeIdParameter.Value = itemStatDescription.StatDescriptionType;

					itemStatDescriptionCommand.ExecuteNonQuery();
				}
			}

			transaction.Commit();

			return items.Length;
		}

		public int SaveItemTags(IEnumerable<String> itemTags) {
			return SaveItemTags(itemTags.ToArray());
		}

		public int SaveItemTags(params String[] itemTags) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "INSERT INTO ItemTag (Name) VALUES ($name);";
			var nameParameter = command.CreateParameter();

			nameParameter.ParameterName = "$name";

			command.Parameters.Add(nameParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var itemTag in itemTags) {
				nameParameter.Value = itemTag;

				command.ExecuteNonQuery();
			}

			transaction.Commit();

			return itemTags.Length;
		}
		#endregion
	}
}
