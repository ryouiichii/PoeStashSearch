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
using System.Threading;
using System.Threading.Tasks;
using PoeStashSearch.Api.Models;

namespace PoeStashSearch.Api.Http {
	public interface IStashApiClient {
		Task<StashTabCollection> GetAllTabsAsync(String accountName, String league, String realm, CancellationToken stoppingToken = default);

		Task<ItemCollection> GetTabAsync(String accountName, String league, String realm, int tabId, CancellationToken stoppingToken = default);

		String SessionId { set; }
	}
}
