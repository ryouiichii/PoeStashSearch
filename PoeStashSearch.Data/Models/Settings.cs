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

namespace PoeStashSearch.Data.Models {
	public record Settings {
		public String AccountName { get; init; } = null!;

		public long ItemRetrievalServicePauseBetweenTabs { get; init; }

		public long ItemRetrievalServiceSleepDuration { get; init; }

		public String League { get; init; } = null!;

		public String Realm { get; init; } = null!;

		public String SessionId { get; init; } = null!;
	}
}
