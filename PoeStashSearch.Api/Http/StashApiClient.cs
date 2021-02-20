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
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using PoeStashSearch.Api.Models;

namespace PoeStashSearch.Api.Http {
	public class StashApiClient : IDisposable, IStashApiClient {
		#region IDisposable Implementation
		public void Dispose() => Dispose(true);

		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			if (disposing) {
				_httpClient?.Dispose();
			}

			_disposed = true;
		}
		#endregion

		#region IStashApiClient Implementation
		public async Task<StashTabCollection> GetAllTabsAsync(String accountName, String league, String realm, CancellationToken stoppingToken = default) {
			EnsureSessionIdIsSet();

			var queryStringParameters = new Dictionary<String, String> {
				["accountName"] = accountName,
				["league"] = league,
				["realm"] = realm,
				["tabs"] = "1"
			};
			var stashTabCollection = await _httpClient!.GetFromJsonAsync<StashTabCollection>(QueryHelpers.AddQueryString("get-stash-items", queryStringParameters), _jsonSerializerOptions, stoppingToken);

			return stashTabCollection ?? new StashTabCollection { TabCount = 0, Tabs = new List<StashTab>() };
		}

		public async Task<ItemCollection> GetTabAsync(String accountName, String league, String realm, int tabId, CancellationToken stoppingToken = default) {
			EnsureSessionIdIsSet();

			var queryStringParameters = new Dictionary<String, String> {
				["accountName"] = accountName,
				["league"] = league,
				["realm"] = realm,
				["tabIndex"] = tabId.ToString(),
				["tabs"] = "0"
			};
			var itemCollection = await _httpClient!.GetFromJsonAsync<ItemCollection>(QueryHelpers.AddQueryString("get-stash-items", queryStringParameters), _jsonSerializerOptions, stoppingToken);

			return itemCollection ?? new ItemCollection { Items = new List<Item>() };
		}

		public String SessionId {
			set {
				if (String.IsNullOrWhiteSpace(value)) {
					throw new ArgumentNullException(nameof(SessionId), "Value must be non-empty.");
				}

				_httpClient?.Dispose();

				var cookieContainer = new CookieContainer();
				cookieContainer.Add(BASE_ADDRESS, new Cookie("POESESSID", value));
				var httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer };

				_httpClient = new HttpClient(httpClientHandler) { BaseAddress = BASE_ADDRESS };
			}
		}
		#endregion

		private void EnsureSessionIdIsSet() {
			if (_httpClient is null) {
				throw new NullReferenceException("SessionId has not been set.");
			}
		}

		private static readonly Uri BASE_ADDRESS = new ("https://www.pathofexile.com/character-window/");
		private bool _disposed;
		private HttpClient? _httpClient;
		private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
			PropertyNameCaseInsensitive = true
		};
	}
}
