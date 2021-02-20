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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using PoeStashSearch.Api.Http;
using PoeStashSearch.Data;
using PoeStashSearch.Data.Repositories;
using PoeStashSearch.Web.Events;
using PoeStashSearch.Web.Settings;

namespace PoeStashSearch.Web {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services) {
			services.AddRazorPages();
			services.AddServerSideBlazor();
			services.AddSingleton<IDatabaseContext, SqliteDatabaseContext>(provider => new SqliteDatabaseContext(CONNECTION_STRING, provider.GetService<IItemRepository>()!, provider.GetService<ISettingsRepository>()!, provider.GetService<IStatRepository>()!));
			services.AddSingleton<IEventBus, EventBus>();
			services.AddSingleton<IItemRepository, ItemRepository>(_ => new ItemRepository(CONNECTION_STRING));
			services.AddSingleton<IResourcesApiClient, ResourcesApiClient>();
			services.AddSingleton<ISettingsManager, SettingsManager>();
			services.AddSingleton<ISettingsRepository, SettingsRepository>(_ => new SettingsRepository(CONNECTION_STRING));
			services.AddSingleton<IStatRepository, StatRepository>(_ => new StatRepository(CONNECTION_STRING));
			services.AddSingleton<IStashApiClient, StashApiClient>();
			services.AddBlazorise(options => {
				options.ChangeTextOnKeyPress = true;
			}).AddBootstrapProviders().AddFontAwesomeIcons();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			} else {
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			//app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.ApplicationServices.UseBootstrapProviders().UseFontAwesomeIcons();

			app.UseEndpoints(endpoints => {
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}

		private const String CONNECTION_STRING = "Data Source=poe-stash-search.db;Mode=ReadWriteCreate;Foreign Keys=True;Cache=Shared";
	}
}
