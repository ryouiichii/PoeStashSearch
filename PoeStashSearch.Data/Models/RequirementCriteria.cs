﻿/*
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

namespace PoeStashSearch.Data.Models {
	public record RequirementCriteria {
		public decimal? DexterityMaximum { get; init; }

		public decimal? DexterityMinimum { get; init; }

		public decimal? IntelligenceMaximum { get; init; }

		public decimal? IntelligenceMinimum { get; init; }

		public decimal? LevelMaximum { get; init; }

		public decimal? LevelMinimum { get; init; }

		public decimal? StrengthMaximum { get; init; }

		public decimal? StrengthMinimum { get; init; }
	}
}
