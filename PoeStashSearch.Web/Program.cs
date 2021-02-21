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
using System.Reflection;
using AutoMapper;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PoeStashSearch.Web.Services;
using Serilog;
using Serilog.Filters;
using CommandLineParser = CommandLine.Parser;

namespace PoeStashSearch.Web {
	public class Program {
		public static void Main(String[] args) {
			CommandLineParser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(o => CreateHostBuilder(args, o).Build().Run());
		}

		public static IHostBuilder CreateHostBuilder(String[] args, CommandLineOptions options) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices(services => {
					services.AddAutoMapper(Assembly.GetExecutingAssembly());
					services.AddHostedService<ItemRetrievalService>();
				})
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
					webBuilder.UseUrls(options.ListenAddress);
				}).UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
					.ReadFrom.Configuration(hostingContext.Configuration)
					.Enrich.FromLogContext()
					.MinimumLevel.Debug()
					.Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore"))
					.Filter.ByExcluding(Matching.FromSource("Serilog.AspNetCore"))
					.Filter.ByExcluding(Matching.FromSource("System.Net.Http"))
					.WriteTo.Debug(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}]{Scope}[{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}", restrictedToMinimumLevel: options.LoggingLevel)
					.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}]{Scope}[{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}", restrictedToMinimumLevel: options.LoggingLevel)
					.WriteTo.File(buffered: true, flushToDiskInterval: TimeSpan.FromSeconds(10), outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}]{Scope}[{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}", path: "poe-stash-search.log", restrictedToMinimumLevel: options.LoggingLevel, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
				);
	}
}
