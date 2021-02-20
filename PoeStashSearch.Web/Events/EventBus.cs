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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PoeStashSearch.Web.Events {
	public class EventBus : IEventBus {
		public EventBus(ILogger<EventBus> logger) {
			_channelToNamedReceiversMap = new Dictionary<String, IDictionary<String, Func<EventArgs, Task>>>();
			_logger = logger;
		}

		#region IEventBus Implementation
		public async Task PublishAsync(String channelName, EventArgs eventArgs) {
			if (_channelToNamedReceiversMap.TryGetValue(channelName, out var maybeNamedReceivers)) {
				foreach (var kvPair in maybeNamedReceivers) {
					try {
						await kvPair.Value(eventArgs);

						_logger.LogDebug("Successfully published {@EventArgs} to {ReceiverName} on channel {ChannelName}", eventArgs, kvPair.Key, channelName);
					} catch (Exception ex) {
						_logger.LogError(ex, "Exception while attempting to publish {@EventArgs} to {ReceiverName} on channel {ChannelName}", eventArgs, kvPair.Key, channelName);
					}
				}
			}
		}

		public void Subscribe(String channelName, Func<EventArgs, Task> receiver, String receiverName) {
			if (_channelToNamedReceiversMap.TryGetValue(channelName, out var maybeNamedReceivers)) {
				maybeNamedReceivers[receiverName] = receiver;
			} else {
				_channelToNamedReceiversMap[channelName] = new Dictionary<String, Func<EventArgs, Task>> {
					{ receiverName, receiver }
				};
			}
		}
		#endregion

		private readonly IDictionary<String, IDictionary<String, Func<EventArgs, Task>>> _channelToNamedReceiversMap;
		private readonly ILogger<EventBus> _logger;
	}
}
