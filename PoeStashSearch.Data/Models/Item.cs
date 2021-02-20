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

namespace PoeStashSearch.Data.Models {
	public record Item {
		public decimal? Armour { get; init; }

		public decimal? AttacksPerSecond { get; init; }

		public decimal? ChanceToBlock { get; init; }

		public decimal? CriticalStrikeChance { get; init; }

		public decimal? EnergyShield { get; init; }

		public decimal? EvasionRating { get; init; }

		public decimal? Experience { get; init; }

		public String FullName { get; init; }

		public String Id { get; init;  }

		public ItemInfluenceTypes? Influences { get; init; }

		public bool IsCorrupted { get; init; }

		public bool IsIdentified { get; init; }

		public long ItemLevel { get; init; }

		public long ItemDefinitionId { get; init; }

		public decimal? PhysicalDamageMaximum { get; init; }

		public decimal? PhysicalDamageMinimum { get; init; }

		public decimal? Quality { get; init; }

		public ItemRarity Rarity { get; init; }

		public decimal? RequiredDexterity { get; init; }

		public decimal? RequiredIntelligence { get; init; }

		public decimal? RequiredLevel { get; init; }

		public decimal? RequiredStrength { get; init; }

		public ICollection<ItemSocket>? Sockets { get; init; }

		public long StashLocationX { get; init; }

		public long StashLocationY { get; init; }

		public String StashName { get; init; }

		public long StashIndex { get; init; }

		public ICollection<ItemStatDescription> StatDescriptions { get; init; }

		public decimal? WeaponRange { get; init; }
	}
}
