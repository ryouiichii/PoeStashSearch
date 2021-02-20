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
	public record SocketCriteria {
		public long? BlueLinksMinimum { get; init; }

		public long? BlueSocketsMinimum { get; init; }

		public long? GreenLinksMinimum { get; init; }

		public long? GreenSocketsMinimum { get; init; }

		public long? LinksMaximum { get; init; }

		public long? LinksMinimum { get; init; }

		public long? RedLinksMinimum { get; init; }

		public long? RedSocketsMinimum { get; init; }

		public long? SocketsMaximum { get; init; }

		public long? SocketsMinimum { get; init; }

		public long? WhiteLinksMinimum { get; init; }

		public long? WhiteSocketsMinimum { get; init; }
	}
}
