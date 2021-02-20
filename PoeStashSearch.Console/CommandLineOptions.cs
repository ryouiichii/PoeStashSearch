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
using CommandLine;
using Serilog.Events;

namespace PoeStashSearch.Console {
	class CommandLineOptions {
		[Option('d', "database", Default = "", HelpText = "Directory where the database will be saved.", Required = false)]
		public String? DirectoryForDatabase { get; set; }

		[Option('l', "loglevel", Default = LogEventLevel.Information, HelpText = "Logging level, one of: Verbose, Debug, Information, Warning, Error, Fatal.", Required = false)]
		public LogEventLevel LoggingLevel { get; set; }

		[Option('b', "baseitems", HelpText = "Full file path of RePoE's base_items.json file.", Required = true)]
		public String PathToBaseItemsJson { get; set; } = null!;

		[Option('c', "clusterjewels", HelpText = "Full file path of RePoE's cluster_jewels.json file.", Required = true)]
		public String PathToClusterJewelsJson { get; set; } = null!;

		[Option('m', "mods", HelpText = "Full file path of RePoE's mods.json file.", Required = true)]
		public String PathToModsJson { get; set; } = null!;

		[Option('s', "stattranslations", HelpText = "Full file path of RePoE's stat_translations.json file.", Required = true)]
		public String PathToStatTranslationsJson { get; set; } = null!;
	}
}
