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
using Microsoft.Data.Sqlite;
using PoeStashSearch.Data.Repositories;

namespace PoeStashSearch.Data {
	public class SqliteDatabaseContext : IDatabaseContext {
		public SqliteDatabaseContext(String connectionString, IItemRepository itemRepository, ISettingsRepository settingsRepository, IStatRepository statRepository) {
			_connectionString = connectionString;
			Items = itemRepository;
			Settings = settingsRepository;
			Stats = statRepository;
		}

		#region IDatabaseContext Implementation
		public IItemRepository Items { get; init; }

		public ISettingsRepository Settings { get; init; }

		public IStatRepository Stats { get; init; }

		//Returns false if the database did not exist and was created, otherwise true
		public bool EnsureDatabaseIsInitialized() {
			using var connection = new SqliteConnection(_connectionString);
			using var command = connection.CreateCommand();

			//Only look for the StatDescription table, if it exists then we assume all other tables exist as well
			command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='StatDescription'";

			connection.Open();
			var statDescriptionTableCount = (long?)command.ExecuteScalar();

			if (statDescriptionTableCount == 1) {
				return true;
			}

			command.CommandText = "CREATE TABLE ItemCategory (" +
			                      " Id INTEGER PRIMARY KEY NOT NULL," +
			                      " Name TEXT NOT NULL);" +
			                      "CREATE UNIQUE INDEX IDX_ItemCategory_Name ON ItemCategory(Name);" +
			                      "CREATE TABLE ItemDefinition (" +
			                      " Id INTEGER PRIMARY KEY NOT NULL," +
			                      " Domain TEXT NOT NULL," +
			                      " ItemCategoryId INTEGER NOT NULL REFERENCES ItemCategory(Id) ON DELETE CASCADE," +
			                      " Name TEXT NOT NULL);" +
			                      "CREATE UNIQUE INDEX IDX_ItemDefinition_Name ON ItemDefinition(Name);" +
			                      "CREATE TABLE Item (" +
			                      " Id TEXT PRIMARY KEY NOT NULL," +
			                      " Armour REAL," +
			                      " AttacksPerSecond REAL," +
			                      " ChanceToBlock REAL," +
			                      " CriticalStrikeChance REAL," +
			                      " EnergyShield REAL," +
			                      " EvasionRating REAL," +
			                      " Experience REAL," +
			                      " FullName TEXT NOT NULL," +
			                      " IsCorrupted BOOLEAN NOT NULL CHECK (IsCorrupted IN (0, 1))," +
			                      " IsCrusaderInfluenced BOOLEAN NOT NULL CHECK (IsCrusaderInfluenced IN (0, 1))," +
			                      " IsElderInfluenced BOOLEAN NOT NULL CHECK (IsElderInfluenced IN (0, 1))," +
			                      " IsHunterInfluenced BOOLEAN NOT NULL CHECK (IsHunterInfluenced IN (0, 1))," +
			                      " IsIdentified BOOLEAN NOT NULL CHECK (IsIdentified IN (0, 1))," +
			                      " IsRedeemerInfluenced BOOLEAN NOT NULL CHECK (IsRedeemerInfluenced IN (0, 1))," +
			                      " IsShaperInfluenced BOOLEAN NOT NULL CHECK (IsShaperInfluenced IN (0, 1))," +
			                      " IsWarlordInfluenced BOOLEAN NOT NULL CHECK (IsWarlordInfluenced IN (0, 1))," +
			                      " ItemDefinitionId INTEGER NOT NULL REFERENCES ItemDefinition(Id) ON DELETE CASCADE," +
			                      " ItemLevel INTEGER NOT NULL," +
			                      " PhysicalDamageMaximum REAL," +
			                      " PhysicalDamageMinimum REAL," +
			                      " Quality REAL," +
			                      " Rarity INTEGER NOT NULL," +
			                      " RequiredDexterity REAL," +
			                      " RequiredIntelligence REAL," +
			                      " RequiredLevel REAL," +
			                      " RequiredStrength REAL," +
			                      " StashIndex INTEGER NOT NULL," +
			                      " StashLocationX INTEGER NOT NULL," +
			                      " StashLocationY INTEGER NOT NULL," +
			                      " StashName TEXT NOT NULL," +
			                      " WeaponRange REAL);" +
			                      "CREATE TABLE ItemSocket (" +
			                      " ItemId TEXT NOT NULL REFERENCES Item(Id) ON DELETE CASCADE," +
			                      " Colour TEXT NOT NULL," +
			                      " [Group] INTEGER NOT NULL);" +
			                      "CREATE INDEX IDX_ItemSocket_ItemId ON ItemSocket(ItemId);" +
			                      "CREATE TABLE ItemTag (" +
			                      " Id INTEGER PRIMARY KEY NOT NULL," +
			                      " Name TEXT NOT NULL);" +
			                      "CREATE UNIQUE INDEX IDX_ItemTag_Name ON ItemTag(Name);" +
			                      "CREATE TABLE ItemDefinitionItemTag (" +
			                      " ItemDefinitionId INTEGER NOT NULL REFERENCES ItemDefinition(Id) ON DELETE CASCADE," +
			                      " ItemTagId INTEGER NOT NULL REFERENCES ItemTag(Id) ON DELETE CASCADE," +
			                      " PRIMARY KEY (ItemDefinitionId, ItemTagId));" +
								  "CREATE TABLE ItemStatDescription (" +
			                      " ItemId TEXT NOT NULL REFERENCES Item(Id) ON DELETE CASCADE," +
			                      " StatDescriptionId INTEGER NOT NULL REFERENCES StatDescription(Id) ON DELETE CASCADE," +
			                      " NumericValue01 REAL," +
			                      " NumericValue02 REAL," +
			                      " NumericValue03 REAL," +
			                      " NumericValue04 REAL," +
			                      " NumericValue05 REAL," +
			                      " NumericValue06 REAL," +
			                      " NumericValue07 REAL," +
			                      " NumericValuesAverage REAL);" +
			                      "CREATE INDEX IDX_ItemStatDescription_StatDescriptionId ON ItemStatDescription(StatDescriptionId);" +
			                      "CREATE TABLE Settings (" +
			                      " AccountName TEXT NOT NULL," +
			                      " ItemRetrievalServicePauseBetweenTabs INTEGER NOT NULL," +
			                      " ItemRetrievalServiceSleepDuration INTEGER NOT NULL," +
			                      " League TEXT NOT NULL," +
			                      " Realm TEXT NOT NULL," +
			                      " SessionId TEXT NOT NULL);" +
			                      "CREATE TABLE StatDescription (" +
			                      " Id INTEGER PRIMARY KEY NOT NULL," +
			                      " DisplayText TEXT NOT NULL," +
			                      " NumericValueCount INTEGER NOT NULL," +
			                      " RegularExpression TEXT NOT NULL," +
			                      " TextId TEXT NOT NULL);" +
			                      "CREATE INDEX IDX_StatDescription_TextId ON StatDescription(TextId);" +
			                      "CREATE TABLE StatDescriptionItemTag (" +
			                      " Domain TEXT," +
			                      " ItemTagId INTEGER NOT NULL REFERENCES ItemTag(Id) ON DELETE CASCADE," +
			                      " StatDescriptionId INTEGER NOT NULL REFERENCES StatDescription(Id) ON DELETE CASCADE," +
			                      " PRIMARY KEY (Domain, ItemTagId, StatDescriptionId));";

			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			command.ExecuteNonQuery();
			transaction.Commit();

			return false;
		}

		public int ExecuteNonQuery(String commandText) {
			using var connection = new SqliteConnection(_connectionString);
			using var command = connection.CreateCommand();

			command.CommandText = commandText;

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;

			var rowsAffected = command.ExecuteNonQuery();

			transaction.Commit();

			return rowsAffected;
		}
		#endregion

		private readonly String _connectionString;
	}
}