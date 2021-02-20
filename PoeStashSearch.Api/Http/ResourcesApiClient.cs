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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PoeStashSearch.Api.Models;

namespace PoeStashSearch.Api.Http {
	public class ResourcesApiClient : IDisposable, IResourcesApiClient {
		public ResourcesApiClient() {
			_httpClient = new HttpClient { BaseAddress = new Uri("https://api.pathofexile.com/") };
		}

		#region IDisposable Implementation
		public void Dispose() => Dispose(true);

		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			if (disposing) {
				_httpClient.Dispose();
			}

			_disposed = true;
		}
		#endregion

		#region IResourcesApiClient Implementation
		public async Task<ICollection<League>> GetAllLeaguesAsync(CancellationToken stoppingToken = default) {
			var leagues = await _httpClient.GetFromJsonAsync<List<League>>("leagues", _jsonSerializerOptions, stoppingToken);

			return leagues ?? new List<League>();
		}

		public Task<ICollection<String>> GetAllRealmsAsync(CancellationToken stoppingToken = default) {
			return _realmsResultTask;
		}
		#endregion

		private bool _disposed;
		private readonly HttpClient _httpClient;
		private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
			PropertyNameCaseInsensitive = true
		};
		private static readonly Task<ICollection<String>> _realmsResultTask = Task.FromResult((ICollection<String>)new List<String> { "pc", "sony", "xbox" });
	}
}
