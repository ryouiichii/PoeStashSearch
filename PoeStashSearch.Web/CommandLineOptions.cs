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

namespace PoeStashSearch.Web {
	public class CommandLineOptions {
		[Option('a', "listenaddress", Default = "http://localhost:5000", HelpText = "The URL used to access the application.")]
		public String ListenAddress { get; set; } = null!;

		[Option('l', "loglevel", Default = LogEventLevel.Information, HelpText = "Logging level, one of: Verbose, Debug, Information, Warning, Error, Fatal.", Required = false)]
		public LogEventLevel LoggingLevel { get; set; }
	}
}
