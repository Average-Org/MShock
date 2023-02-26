/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Auxiliary;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB.Models;

namespace TShockAPI.DB
{
	public class ItemBanManager
	{
		/// <summary>
		/// Retrieves a List of all item bans
		/// </summary>
		public List<ItemBan> RetrieveAllItembans()
		{
			return StorageProvider.GetMongoCollection<ItemBan>("ItemBans").Find(x => true).ToList();
		}

		public async void AddNewBan(string itemname = "")
		{
			try
			{
				var item = await IModel.CreateAsync(CreateRequest.Bson<ItemBan>(x =>
				{
					x.Name = itemname;
				}));

			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public void RemoveBan(string itemname)
		{
			if (!ItemIsBanned(itemname, null))
				return;
			try
			{
				StorageProvider.GetMongoCollection<ItemBan>("ItemBans").FindOneAndDelete(x => x.Name == itemname);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public bool ItemIsBanned(string name)
		{
			if (RetrieveAllItembans().Contains(new ItemBan(name)))
			{
				return true;
			}
			return false;
		}

		public bool ItemIsBanned(string name, TSPlayer ply)
		{
			ItemBan b = GetItemBanByName(name);
			return b != null && !b.HasPermissionToUseItem(ply);
		}

		public bool AllowGroup(string item, string group)
		{
			string groupsNew = "";
			ItemBan b = GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					ItemBan ban = GetItemBanByName(item);
					var temp = ban.AllowedGroups;
					temp.Add(group);
					ban.AllowedGroups = temp;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}

			return false;
		}

		public bool RemoveGroup(string item, string group)
		{
			ItemBan b = GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					ItemBan ban = GetItemBanByName(item);
					var temp = ban.AllowedGroups;
					temp.Remove(group);
					ban.AllowedGroups = temp;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		public ItemBan GetItemBanByName(string name) => RetrieveAllItembans().FirstOrDefault(x => x.Name == name, null);
	}
}
