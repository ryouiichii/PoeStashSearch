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
using PoeStashSearch.Data.Models;

namespace PoeStashSearch.Data.Repositories {
	public interface IItemRepository {
		ICollection<ItemCategory> GetAllItemCategories();

		ICollection<ItemDefinition> GetAllItemDefinitions();

		ICollection<Item> GetItemsWithStatDescriptions(ArmourCriteria? armourCriteria, InfluenceCriteria? influenceCriteria, ItemDefinitionCriteria? itemDefinitionCriteria, MiscellaneousCriteria? miscellaneousCriteria, RequirementCriteria? requirementCriteria, SocketCriteria? socketCriteria, WeaponCriteria? weaponCriteria, IEnumerable<StatDescriptionCriteria> statDescriptionCriterias);

		ICollection<Item> GetItemsWithStatDescriptions(ArmourCriteria? armourCriteria, InfluenceCriteria? influenceCriteria, ItemDefinitionCriteria? itemDefinitionCriteria, MiscellaneousCriteria? miscellaneousCriteria, RequirementCriteria? requirementCriteria, SocketCriteria? socketCriteria, WeaponCriteria? weaponCriteria, params StatDescriptionCriteria[] statDescriptionCriterias);

		int SaveItemCategories(IEnumerable<String> itemCategories);

		int SaveItemCategories(params String[] itemCategories);

		int SaveItemDefinitions(IEnumerable<ItemDefinition> itemDefinitions);

		int SaveItemDefinitions(params ItemDefinition[] itemDefinitions);

		int SaveItems(bool deleteExistingItems, IEnumerable<Item> items);

		int SaveItems(bool deleteExistingItems, params Item[] items);

		int SaveItemTags(IEnumerable<String> itemTags);

		int SaveItemTags(params String[] itemTags);
	}
}
