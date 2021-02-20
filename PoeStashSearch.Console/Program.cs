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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using PoeStashSearch.Data;
using PoeStashSearch.Data.Repositories;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using CommandLineParser = CommandLine.Parser;

namespace PoeStashSearch.Console {
	class Program {
		static async Task Main(string[] args) {
			await CommandLineParser.Default.ParseArguments<CommandLineOptions>(args).WithParsedAsync(async o => {
				var logger = new LoggerConfiguration().MinimumLevel.Is(o.LoggingLevel).WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Literate).CreateLogger();

				try {
					var destinationDatabaseDirectory = !String.IsNullOrWhiteSpace(o.DirectoryForDatabase) ? o.DirectoryForDatabase! : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
					var fullDestinationDatabasePath = Path.Combine(destinationDatabaseDirectory, "poe-stash-search.db");
					var connectionString = $"Data Source={fullDestinationDatabasePath};Mode=ReadWriteCreate;Foreign Keys=True;Cache=Shared";

					var dbContext = new SqliteDatabaseContext(connectionString, new ItemRepository(connectionString), new SettingsRepository(connectionString), new StatRepository(connectionString));
					var rePoeParser = new RePoeParser(logger);

					logger.Information("Attempting to create and populate database {PathToDatabase}", fullDestinationDatabasePath);
					dbContext.EnsureDatabaseIsInitialized();

					var baseItems = await rePoeParser.ParseBaseItemsAsync(o.PathToBaseItemsJson);
					var itemCategories = baseItems.Select(b => b.Category).Distinct();
					var itemTags = baseItems.SelectMany(b => b.Tags).Distinct();

					logger.Debug("Attempting to save item categories");
					var savedItemCategoryCount = dbContext.Items.SaveItemCategories(itemCategories);
					logger.Debug("Successfully saved {ItemClassCount} item categories", savedItemCategoryCount);

					logger.Debug("Attempting to save item tags");
					var savedItemTagCount = dbContext.Items.SaveItemTags(itemTags);
					logger.Debug("Successfully saved {ItemTagCount} item tags", savedItemTagCount);

					logger.Debug("Attempting to save item definitions");
					var savedItemDefinitionCount = dbContext.Items.SaveItemDefinitions(baseItems);
					logger.Debug("Successfully saved {ItemDefinitionCount} item definitions", savedItemDefinitionCount);

					var clusterJewelStatDescriptions = await rePoeParser.ParseClusterJewelsAsync(o.PathToClusterJewelsJson);
					var statDescriptions = (await rePoeParser.ParseStatDescriptionsAsync(o.PathToStatTranslationsJson)).Distinct().ToList();
					var statDescriptionItemTags = (await rePoeParser.ParseModsAsync(o.PathToModsJson)).Distinct().ToList();
					var actuallyUsedStatDescriptions = statDescriptions.Where(s => statDescriptionItemTags.Any(sdit => s.TextId.Equals(sdit.StatDescriptionTextId)));

					logger.Debug("Attempting to save stat descriptions");
					var savedStatDescriptionCount = dbContext.Stats.SaveStatDescriptions(actuallyUsedStatDescriptions);
					logger.Debug("Successfully saved {StatDescriptionCount} stat descriptions", savedStatDescriptionCount);

					logger.Debug("Attempting to save cluster jewel stat descriptions");
					var savedClusterJewelStatDescriptions = dbContext.Stats.SaveClusterJewelStatDescriptions(clusterJewelStatDescriptions);
					logger.Debug("Successfully saved {ClusterJewelCount} cluster jewel stat descriptions", savedClusterJewelStatDescriptions);

					logger.Debug("Attempting to save stat description<->item tag relationships");
					var savedStatDescriptionItemTagCount = dbContext.Stats.SaveItemTagsForStatDescriptions(statDescriptionItemTags);
					logger.Debug("Successfully saved {StatDescriptionItemTagCount} stat description<->item tag relationships", savedStatDescriptionItemTagCount);

					logger.Debug("Attempting to execute custom SQL");
					logger.Verbose("Attempting to fix domain for map_doesnt_consume_sextant_use stat description<->item tag relationship(s)");
					var mapDoesntConsumeSextantUseRowsAffected = dbContext.ExecuteNonQuery("UPDATE StatDescriptionItemTag SET Domain = 'area' WHERE StatDescriptionId IN (SELECT Id FROM StatDescription WHERE TextId = 'map_doesnt_consume_sextant_use');");
					logger.Verbose("Successfully fixed domain for {MapDoesntConsumeSextantUseRowsAffected} map_doesnt_consume_sextant_use stat description<->item tag relationship(s)", mapDoesntConsumeSextantUseRowsAffected);
					logger.Verbose("Attempting to fix item category name(s)");
					var itemCategoryNameRowsAffected = dbContext.ExecuteNonQuery("UPDATE ItemCategory SET Name = 'Abyss Jewel' WHERE Name = 'AbyssJewel'; UPDATE ItemCategory SET Name = 'Fishing Rod' WHERE Name = 'FishingRod';");
					logger.Verbose("Successfully fixed {ItemCategoryNameRowsAffected} item category name(s)", itemCategoryNameRowsAffected);
					logger.Debug("Successfully executed custom SQL");

					logger.Information("Successfully created and populated database {PathToDatabase}", fullDestinationDatabasePath);
				} catch (Exception ex) {
					logger.Fatal(ex, "Exception occurred while attempting to create and populate database");
				}
			});
		}
	}
}