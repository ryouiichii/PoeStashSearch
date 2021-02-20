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
using System.Text.Json.Serialization;

namespace PoeStashSearch.Api.Models {
	public record Item {
		public ICollection<String>? CraftedMods { get; init; }

		[JsonPropertyName("descrText")]
		public String? Description { get; init; }

		public ICollection<String>? EnchantMods { get; init; }

		public ICollection<String>? ExplicitMods { get; init; }

		public ICollection<String>? FracturedMods { get; init; }

		public String Id { get; init;  }

		public ICollection<String>? ImplicitMods { get; init; }

		public ItemInfluenceTypes? Influences { get; init; }

		[JsonPropertyName("corrupted")]
		public bool IsCorrupted { get; init; }

		[JsonPropertyName("identified")]
		public bool IsIdentified { get; init; }

		[JsonPropertyName("ilvl")]
		public int ItemLevel { get; init; }

		public String? Name { get; init; }

		public ICollection<ItemProperty>? Properties { get; init; }

		[JsonPropertyName("frameType")]
		public ItemRarity Rarity { get; init; }

		public ICollection<ItemRequirement>? Requirements { get; init; }

		public ICollection<ItemSocket>? Sockets { get; init; }

		public String? TypeLine { get; init; }

		public int X { get; init; }

		public int Y { get; init; }
	}
}
