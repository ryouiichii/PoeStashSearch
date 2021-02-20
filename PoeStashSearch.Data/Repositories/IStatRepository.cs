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
using System.Collections.Generic;
using PoeStashSearch.Data.Models;

namespace PoeStashSearch.Data.Repositories {
	public interface IStatRepository {
		ICollection<StatDescription> GetAllStatDescriptions();

		ICollection<StatDescription> GetStatDescriptionsForItemDefinition(long itemDefinitionId);

		int SaveClusterJewelStatDescriptions(IEnumerable<ClusterJewelStatDescription> clusterJewelStatDescriptions);

		int SaveClusterJewelStatDescriptions(params ClusterJewelStatDescription[] clusterJewelStatDescriptions);

		int SaveItemTagsForStatDescriptions(IEnumerable<StatDescriptionItemTag> statDescriptionItemTags);

		int SaveItemTagsForStatDescriptions(params StatDescriptionItemTag[] statDescriptionItemTags);

		int SaveStatDescriptions(IEnumerable<StatDescription> statDescriptions);

		int SaveStatDescriptions(params StatDescription[] statDescriptions);
	}
}
