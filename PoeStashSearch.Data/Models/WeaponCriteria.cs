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

namespace PoeStashSearch.Data.Models {
	public record WeaponCriteria {
		public decimal? AttacksPerSecondMaximum { get; init; }

		public decimal? AttacksPerSecondMinimum { get; init; }

		public decimal? CriticalStrikeChanceMaximum { get; init; }

		public decimal? CriticalStrikeChanceMinimum { get; init; }

		public decimal? PhysicalDamageBottomEndMaximum { get; init; }

		public decimal? PhysicalDamageBottomEndMinimum { get; init; }

		public decimal? PhysicalDamageTopEndMaximum { get; init; }

		public decimal? PhysicalDamageTopEndMinimum { get; init; }
	}
}
