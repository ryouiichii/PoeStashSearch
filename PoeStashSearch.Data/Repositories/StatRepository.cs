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
using System.Linq;
using PoeStashSearch.Data.Models;

namespace PoeStashSearch.Data.Repositories {
	public class StatRepository : RepositoryBase, IStatRepository {
		public StatRepository(String connectionString) : base(connectionString) {
		}

		#region IStatRepository Implementation
		public ICollection<StatDescription> GetAllStatDescriptions() {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			var statDescriptions = new List<StatDescription>();
			command.CommandText = "SELECT DisplayText, Id, NumericValueCount, RegularExpression, TextId FROM StatDescription ORDER BY DisplayText ASC;";

			connection.Open();
			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				statDescriptions.Add(new() {
					DisplayText = (String)reader["DisplayText"],
					Id = (long)reader["Id"],
					NumericValueCount = (long)reader["NumericValueCount"],
					RegularExpression = (String)reader["RegularExpression"],
					TextId = (String)reader["TextId"]
				});
			}

			return statDescriptions;
		}

		public ICollection<StatDescription> GetStatDescriptionsForItemDefinition(long itemDefinitionId) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			var statDescriptions = new List<StatDescription>();
			command.CommandText = "SELECT sd.Id, sd.RegularExpression FROM ItemDefinition AS id" +
			                      " INNER JOIN ItemDefinitionItemTag AS idig ON id.Id = idig.ItemDefinitionId" +
			                      " INNER JOIN ItemTag AS it ON it.Id = idig.ItemTagId" +
			                      " INNER JOIN StatDescriptionItemTag AS sdit ON (sdit.ItemTagId = it.Id AND sdit.Domain = id.Domain)" +
			                      " INNER JOIN StatDescription AS sd ON sd.Id = sdit.StatDescriptionId " +
			                      "WHERE id.Id = $itemDefinitionId;";
			var itemDefinitionIdParameter = command.CreateParameter();

			itemDefinitionIdParameter.ParameterName = "$itemDefinitionId";
			itemDefinitionIdParameter.Value = itemDefinitionId;
			command.Parameters.Add(itemDefinitionIdParameter);

			connection.Open();
			using var reader = command.ExecuteReader();
			while (reader.Read()) {
				statDescriptions.Add(new() {
					Id = (long)reader["Id"],
					RegularExpression = (String)reader["RegularExpression"]
				});
			}

			return statDescriptions;
		}

		public int SaveClusterJewelStatDescriptions(IEnumerable<ClusterJewelStatDescription> clusterJewelStatDescriptions) {
			return SaveClusterJewelStatDescriptions(clusterJewelStatDescriptions.ToArray());
		}

		public int SaveClusterJewelStatDescriptions(params ClusterJewelStatDescription[] clusterJewelStatDescriptions) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			//This query only works with at most one numeric value to replace, which is fine for now. If these cluster
			// jewel enchants ever have two or more numeric values to replace, this query will not be sufficient.
			//This query also grabs all matching TextId rows, although realistically it should only grab the right one
			// based on the numeric value range. That is, this will create rows for both e.g. "increased" and "reduced"
			// displays even though only one (usually "increased") is actually used as an enchant.
			command.CommandText = @"WITH CorrespondingStatDescriptions AS (SELECT DisplayText, RegularExpression, TextId FROM StatDescription WHERE TextId = $statDescriptionTextId) INSERT INTO StatDescription (DisplayText, NumericValueCount, RegularExpression, TextId) SELECT 'Added Small Passive Skills grant: ' || REPLACE(DisplayText, '#', $statDescriptionValue), 0, 'Added Small Passive Skills grant: ' || REPLACE(RegularExpression, '(?<number>\d+\.?\d*)', $statDescriptionValue), $category || TextId FROM CorrespondingStatDescriptions;";
			var categoryParameter = command.CreateParameter();
			var statDescriptionTextIdParameter = command.CreateParameter();
			var statDescriptionValueParameter = command.CreateParameter();

			categoryParameter.ParameterName = "$category";
			statDescriptionTextIdParameter.ParameterName = "$statDescriptionTextId";
			statDescriptionValueParameter.ParameterName = "$statDescriptionValue";

			command.Parameters.Add(categoryParameter);
			command.Parameters.Add(statDescriptionTextIdParameter);
			command.Parameters.Add(statDescriptionValueParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var clusterJewelStatDescription in clusterJewelStatDescriptions) {
				categoryParameter.Value = clusterJewelStatDescription.Category;
				statDescriptionTextIdParameter.Value = clusterJewelStatDescription.StatDescriptionTextId;
				statDescriptionValueParameter.Value = clusterJewelStatDescription.StatDescriptionValue ?? (Object)DBNull.Value;

				command.ExecuteNonQuery();
			}

			//Search for stat description TextId values prefixed with the category and pair them up with the right
			// item definitions. These TextId values prefixed with the category were just inserted above.
			command.CommandText = "WITH CurrentItemDefinition AS (SELECT Id, Domain FROM ItemDefinition WHERE Name = $category), CurrentItemTag AS (SELECT ItemTagId FROM ItemDefinitionItemTag WHERE ItemDefinitionId IN (SELECT Id FROM CurrentItemDefinition)), CurrentStatDescription AS (SELECT Id FROM StatDescription WHERE TextId LIKE $category || '%') INSERT INTO StatDescriptionItemTag (Domain, ItemTagId, StatDescriptionId) SELECT CurrentItemDefinition.Domain, CurrentItemTag.ItemTagId, CurrentStatDescription.Id FROM CurrentItemDefinition, CurrentItemTag, CurrentStatDescription;";
			foreach (var category in clusterJewelStatDescriptions.GroupBy(c => c.Category).Select(c => c.Key)) {
				categoryParameter.Value = category;

				command.ExecuteNonQuery();
			}

			transaction.Commit();

			return clusterJewelStatDescriptions.Length;
		}

		public int SaveItemTagsForStatDescriptions(IEnumerable<StatDescriptionItemTag> statDescriptionItemTags) {
			return SaveItemTagsForStatDescriptions(statDescriptionItemTags.ToArray());
		}

		public int SaveItemTagsForStatDescriptions(params StatDescriptionItemTag[] statDescriptionItemTags) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "WITH CurrentItemTag AS (SELECT Id FROM ItemTag WHERE Name = $itemTag), CurrentStatDescription AS (SELECT Id FROM StatDescription WHERE TextId = $statDescription) INSERT INTO StatDescriptionItemTag (Domain, ItemTagId, StatDescriptionId) SELECT $domain, CurrentItemTag.Id, CurrentStatDescription.Id FROM CurrentItemTag, CurrentStatDescription;";
			var domainParameter = command.CreateParameter();
			var itemTagParameter = command.CreateParameter();
			var statDescriptionParameter = command.CreateParameter();

			domainParameter.ParameterName = "$domain";
			itemTagParameter.ParameterName = "$itemTag";
			statDescriptionParameter.ParameterName = "$statDescription";

			command.Parameters.Add(domainParameter);
			command.Parameters.Add(itemTagParameter);
			command.Parameters.Add(statDescriptionParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var statDescriptionItemTag in statDescriptionItemTags) {
				domainParameter.Value = statDescriptionItemTag.Domain ?? (Object)DBNull.Value;
				itemTagParameter.Value = statDescriptionItemTag.ItemTagName;
				statDescriptionParameter.Value = statDescriptionItemTag.StatDescriptionTextId;

				command.ExecuteNonQuery();
			}

			transaction.Commit();

			return statDescriptionItemTags.Length;
		}

		public int SaveStatDescriptions(IEnumerable<StatDescription> statDescriptions) {
			return SaveStatDescriptions(statDescriptions.ToArray());
		}

		public int SaveStatDescriptions(params StatDescription[] statDescriptions) {
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();

			command.CommandText = "INSERT INTO StatDescription (DisplayText, NumericValueCount, RegularExpression, TextId) VALUES ($displayText, $numericValueCount, $regularExpression, $textId);";
			var displayTextParameter = command.CreateParameter();
			var numericValueCountParameter = command.CreateParameter();
			var regularExpressionParameter = command.CreateParameter();
			var textIdParameter = command.CreateParameter();

			displayTextParameter.ParameterName = "$displayText";
			numericValueCountParameter.ParameterName = "$numericValueCount";
			regularExpressionParameter.ParameterName = "$regularExpression";
			textIdParameter.ParameterName = "$textId";

			command.Parameters.Add(displayTextParameter);
			command.Parameters.Add(numericValueCountParameter);
			command.Parameters.Add(regularExpressionParameter);
			command.Parameters.Add(textIdParameter);

			connection.Open();
			using var transaction = connection.BeginTransaction(true);
			command.Transaction = transaction;
			foreach (var statDescription in statDescriptions) {
				displayTextParameter.Value = statDescription.DisplayText;
				numericValueCountParameter.Value = statDescription.NumericValueCount;
				regularExpressionParameter.Value = statDescription.RegularExpression;
				textIdParameter.Value = statDescription.TextId;

				command.ExecuteNonQuery();
			}

			transaction.Commit();

			return statDescriptions.Length;
		}
		#endregion
	}
}
