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
using PoeStashSearch.Data.Models;

namespace PoeStashSearch.Data.Repositories {
	public class SettingsRepository : RepositoryBase, ISettingsRepository {
		public SettingsRepository(String connectionString) : base(connectionString) {
		}

		#region ISettingsRepository Implementation
		public Settings? GetAllSettings() {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			Settings settings = default!;
			command.CommandText = "SELECT AccountName, ItemRetrievalServicePauseBetweenTabs, ItemRetrievalServiceSleepDuration, League, Realm, SessionId FROM Settings;";

			connection.Open();
			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				settings = new() {
					AccountName = (String)reader["AccountName"],
					ItemRetrievalServicePauseBetweenTabs = (long)reader["ItemRetrievalServicePauseBetweenTabs"],
					ItemRetrievalServiceSleepDuration = (long)reader["ItemRetrievalServiceSleepDuration"],
					League = (String)reader["League"],
					Realm = (String)reader["Realm"],
					SessionId = (String)reader["SessionId"]
				};
			}

			return settings;
		}

		public int SaveSettings(Settings settings) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "DELETE FROM Settings; INSERT INTO Settings (AccountName, ItemRetrievalServicePauseBetweenTabs, ItemRetrievalServiceSleepDuration, League, Realm, SessionId) VALUES ($accountName, $itemRetrievalServicePauseBetweenTabs, $itemRetrievalServiceSleepDuration, $league, $realm, $sessionId);";
			var accountNameParameter = command.CreateParameter();
			var itemRetrievalServicePauseBetweenTabs = command.CreateParameter();
			var itemRetrievalServiceSleepDuration = command.CreateParameter();
			var leagueParameter = command.CreateParameter();
			var realmParameter = command.CreateParameter();
			var sessionIdParameter = command.CreateParameter();

			accountNameParameter.ParameterName = "$accountName";
			itemRetrievalServicePauseBetweenTabs.ParameterName = "$itemRetrievalServicePauseBetweenTabs";
			itemRetrievalServiceSleepDuration.ParameterName = "$itemRetrievalServiceSleepDuration";
			leagueParameter.ParameterName = "$league";
			realmParameter.ParameterName = "$realm";
			sessionIdParameter.ParameterName = "$sessionId";

			command.Parameters.Add(accountNameParameter);
			command.Parameters.Add(itemRetrievalServicePauseBetweenTabs);
			command.Parameters.Add(itemRetrievalServiceSleepDuration);
			command.Parameters.Add(leagueParameter);
			command.Parameters.Add(realmParameter);
			command.Parameters.Add(sessionIdParameter);

			accountNameParameter.Value = settings.AccountName;
			itemRetrievalServicePauseBetweenTabs.Value = settings.ItemRetrievalServicePauseBetweenTabs;
			itemRetrievalServiceSleepDuration.Value = settings.ItemRetrievalServiceSleepDuration;
			leagueParameter.Value = settings.League;
			realmParameter.Value = settings.Realm;
			sessionIdParameter.Value = settings.SessionId;

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;

			command.ExecuteNonQuery();

			transaction.Commit();

			return 1;
		}
		#endregion
	}
}