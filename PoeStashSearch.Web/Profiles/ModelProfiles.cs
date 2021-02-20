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
using System.Text.Json;
using AutoMapper;
using PoeStashSearch.Api.Models;
using PoeStashSearch.Data.Models;
using ApiItem = PoeStashSearch.Api.Models.Item;
using ApiItemInfluenceTypes = PoeStashSearch.Api.Models.ItemInfluenceTypes;
using ApiItemRarity = PoeStashSearch.Api.Models.ItemRarity;
using ApiItemSocket = PoeStashSearch.Api.Models.ItemSocket;
using DataItem = PoeStashSearch.Data.Models.Item;
using DataItemInfluenceTypes = PoeStashSearch.Data.Models.ItemInfluenceTypes;
using DataItemRarity = PoeStashSearch.Data.Models.ItemRarity;
using DataItemSocket = PoeStashSearch.Data.Models.ItemSocket;

namespace PoeStashSearch.Web.Profiles {
	public class ModelProfiles : Profile {
		public ModelProfiles() {
			CreateMap<ApiItem, DataItem>()
				.ForMember(d => d.Armour, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.Armour)?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.AttacksPerSecond, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.AttacksPerSecond)?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.ChanceToBlock, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.ChanceToBlock)?.Values?.First().First() is JsonElement jsonElement) {
						return decimal.TryParse(jsonElement.GetString()?.Split('%')[0], out var maybeDecimal) ? maybeDecimal : (decimal?)null;
					}

					return null;
				}))
				.ForMember(d => d.CriticalStrikeChance, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.CriticalStrikeChance)?.Values?.First().First() is JsonElement jsonElement) {
						return decimal.TryParse(jsonElement.GetString()?.Split('%')[0], out var maybeDecimal) ? maybeDecimal : (decimal?)null;
					}

					return null;
				}))
				.ForMember(d => d.EnergyShield, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.EnergyShield)?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.EvasionRating, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.EvasionRating)?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.Name} {s.TypeLine}".Trim()))
				.ForMember(d => d.PhysicalDamageMaximum, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.PhysicalDamage)?.Values?.First().First() is JsonElement jsonElement) {
						return decimal.TryParse(jsonElement.GetString()?.Split('-')[1], out var maybeDecimal) ? maybeDecimal : (decimal?)null;
					}

					return null;
				}))
				.ForMember(d => d.PhysicalDamageMinimum, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.PhysicalDamage)?.Values?.First().First() is JsonElement jsonElement) {
						return decimal.TryParse(jsonElement.GetString()?.Split('-')[0], out var maybeDecimal) ? maybeDecimal : (decimal?)null;
					}

					return null;
				}))
				.ForMember(d => d.Quality, opt => opt.MapFrom((s, _) => {
					if (s.Properties?.FirstOrDefault(p => p.Type == ItemPropertyType.Quality)?.Values?.First().First() is JsonElement jsonElement) {
						var qualityString = jsonElement.GetString() ?? String.Empty;

						return decimal.TryParse(qualityString.Substring(1, qualityString.Length - 2), out var maybeDecimal) ? maybeDecimal : (decimal?)null;
					}

					return null;
				}))
				.ForMember(d => d.RequiredDexterity, opt => opt.MapFrom((s, _) => {
					if (s.Requirements?.FirstOrDefault(r => "Dex".Equals(r.Name))?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.RequiredIntelligence, opt => opt.MapFrom((s, _) => {
					if (s.Requirements?.FirstOrDefault(r => "Int".Equals(r.Name))?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.RequiredLevel, opt => opt.MapFrom((s, _) => {
					if (s.Requirements?.FirstOrDefault(r => "Level".Equals(r.Name))?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.RequiredStrength, opt => opt.MapFrom((s, _) => {
					if (s.Requirements?.FirstOrDefault(r => "Str".Equals(r.Name))?.Values?.First().First() is JsonElement jsonElement && decimal.TryParse(jsonElement.GetString(), out var maybeDecimal)) {
						return maybeDecimal;
					}

					return (decimal?)null;
				}))
				.ForMember(d => d.StashLocationX, opt => opt.MapFrom(s => s.X))
				.ForMember(d => d.StashLocationY, opt => opt.MapFrom(s => s.Y))
				.ForMember(d => d.StatDescriptions, opt => opt.MapFrom(_ => new List<ItemStatDescription>()));
			CreateMap<ApiItemInfluenceTypes, DataItemInfluenceTypes>();
			CreateMap<ApiItemRarity, DataItemRarity>();
			CreateMap<ApiItemSocket, DataItemSocket>();
		}
	}
}
