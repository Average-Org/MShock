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
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using TShockAPI.DB.Models;

namespace TShockAPI.DB
{
	/// <summary>
	/// Represents the Region database manager.
	/// </summary>
	public class RegionManager
	{
		/// <summary>
		/// Retrieves a List of all regions
		/// </summary>
		public List<Region> RetrieveAllRegions()
		{
			return StorageProvider.GetMongoCollection<Region>("Regions").Find(x => true).ToList();
		}

		/// <summary>
		/// Adds a region to the database.
		/// </summary>
		/// <param name="tx">TileX of the top left corner.</param>
		/// <param name="ty">TileY of the top left corner.</param>
		/// <param name="width">Width of the region in tiles.</param>
		/// <param name="height">Height of the region in tiles.</param>
		/// <param name="regionname">The name of the region.</param>
		/// <param name="owner">The User Account Name of the person who created this region.</param>
		/// <param name="worldid">The world id that this region is in.</param>
		/// <param name="z">The Z index of the region.</param>
		/// <returns>Whether the region was created and added successfully.</returns>
		public async Task<bool> AddRegion(int tx, int ty, int width, int height, string regionname, string owner, string worldid, int z = 0)
		{
			var temp = GetRegionByName(regionname);

			if (temp != null)
			{
				if (!(temp.WorldID != Main.worldID.ToString()))
					return false;
			}
			try
			{
				var region = await IModel.CreateAsync(CreateRequest.Bson<Region>(x =>
				{
					x.Owner = owner;
					x.Name = regionname;
					x.WorldID = worldid;
					x.Area = new Rectangle(tx, ty, width, height);
					x.Z = z;
					x.DisableBuild = true;
				}));
				Hooks.RegionHooks.OnRegionCreated(region);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Deletes the region from this world with a given ID.
		/// </summary>
		/// <param name="name">The name of the region to delete.</param>
		/// <returns>Whether the region was successfully deleted.</returns>
		public bool DeleteRegion(string name)
		{
			try
			{
				var region = StorageProvider.GetMongoCollection<Region>("Regions").FindOneAndDelete(x => x.Name == name && x.WorldID == Main.worldID.ToString());
				Hooks.RegionHooks.OnRegionDeleted(region);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Sets the protected state of the region with a given name.
		/// </summary>
		/// <param name="name">The name of the region to change.</param>
		/// <param name="state">New protected state of the region.</param>
		/// <returns>Whether the region's state was successfully changed.</returns>
		public bool SetRegionState(string name, bool state)
		{
			try
			{
				var region = GetRegionByName(name);
				if (region != null)
					region.DisableBuild = state;
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}

		/// <summary>
		/// Checks if a given player can build in a region at the given (x, y) coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="ply">Player to check permissions with</param>
		/// <returns>Whether the player can build at the given (x, y) coordinate</returns>
		public bool CanBuild(int x, int y, TSPlayer ply)
		{
			if (!ply.HasPermission(Permissions.canbuild))
			{
				return false;
			}
			Region top = null;

			foreach (Region region in RetrieveAllRegions().ToList())
			{
				if (region.InArea(x, y))
				{
					if (top == null || region.Z > top.Z)
						top = region;
				}
			}
			return top == null || top.HasPermissionToBuildInRegion(ply);
		}

		/// <summary>
		/// Checks if any regions exist at the given (x, y) coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>Whether any regions exist at the given (x, y) coordinate</returns>
		public bool InArea(int x, int y)
		{
			return RetrieveAllRegions().Any(r => r.InArea(x, y));
		}

		/// <summary>
		/// Checks if any regions exist at the given (x, y) coordinate
		/// and returns an IEnumerable containing their names
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>The names of any regions that exist at the given (x, y) coordinate</returns>
		public IEnumerable<string> InAreaRegionName(int x, int y)
		{
			return RetrieveAllRegions().Where(r => r.InArea(x, y)).Select(r => r.Name);
		}

		/// <summary>
		/// Checks if any regions exist at the given (x, y) coordinate
		/// and returns an IEnumerable containing their <see cref="Region"/> objects
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>The <see cref="Region"/> objects of any regions that exist at the given (x, y) coordinate</returns>
		public IEnumerable<Region> InAreaRegion(int x, int y)
		{
			return RetrieveAllRegions().Where(r => r.InArea(x, y));
		}

		/// <summary>
		/// Changes the size of a given region
		/// </summary>
		/// <param name="regionName">Name of the region to resize</param>
		/// <param name="addAmount">Amount to resize</param>
		/// <param name="direction">Direction to resize in:
		/// 0 = resize height and Y.
		/// 1 = resize width.
		/// 2 = resize height.
		/// 3 = resize width and X.</param>
		/// <returns></returns>
		public bool ResizeRegion(string regionName, int addAmount, int direction)
		{
			//0 = up
			//1 = right
			//2 = down
			//3 = left

			try
			{
				Region region = GetRegionByName(regionName);

				switch (direction)
				{
					case 0:
						region.Area = new Rectangle(region.Area.X, region.Area.Y - addAmount, region.Area.Width, region.Area.Height + addAmount);
						break;
					case 1:
						region.Area = new Rectangle(region.Area.X, region.Area.Y, region.Area.Width + addAmount, region.Area.Height);
						break;
					case 2:
						region.Area = new Rectangle(region.Area.X, region.Area.Y, region.Area.Width, region.Area.Height + addAmount);
						break;
					case 3:
						region.Area = new Rectangle(region.Area.X - addAmount, region.Area.Y, region.Area.Width + addAmount, region.Area.Height);
						break;
					default:
						return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Renames a region
		/// </summary>
		/// <param name="oldName">Name of the region to rename</param>
		/// <param name="newName">New name of the region</param>
		/// <returns>true if renamed successfully, false otherwise</returns>
		public bool RenameRegion(string oldName, string newName)
		{
			Region region = GetRegionByName(oldName);
			if (region != null)
			{
				region.Name = newName;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes an allowed user from a region
		/// </summary>
		/// <param name="regionName">Name of the region to modify</param>
		/// <param name="userName">Username to remove</param>
		/// <returns>true if removed successfully</returns>
		public bool RemoveUser(string regionName, string userName)
		{
			Region r = GetRegionByName(regionName);
			int id = TShock.UserAccounts.GetUserAccountByName(userName).ID;
			if (r != null)
			{
				if (!r.AllowedIDs.Contains(id))
				{
					var temp = r.AllowedIDs;
					temp.Remove(id);
					r.AllowedIDs = temp;
					return true;
				}
			}

			return false;
		}



		/// <summary>
		/// Sets the position of a region.
		/// </summary>
		/// <param name="regionName">The region name.</param>
		/// <param name="x">The X position.</param>
		/// <param name="y">The Y position.</param>
		/// <param name="height">The height.</param>
		/// <param name="width">The width.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public bool PositionRegion(string regionName, int x, int y, int width, int height)
		{
			try
			{
				Region region = GetRegionByName(regionName);
				region.Area = new Rectangle(x, y, width, height);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Returns a region with the given name
		/// </summary>
		/// <param name="name">Region name</param>
		/// <returns>The region with the given name, or null if not found</returns>
		public Region GetRegionByName(string name)
		{
			return IModel.GetAsync(GetRequest.Bson<Region>(x => x.Name == "name")).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Changes the owner of the region with the given name
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="newOwner">New owner's username</param>
		/// <returns>Whether the change was successful</returns>
		public bool ChangeOwner(string regionName, string newOwner)
		{
			var region = GetRegionByName(regionName);
			if (region != null)
			{
				region.Owner = newOwner;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Allows a group to use a region
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="groupName">Group's name</param>
		/// <returns>Whether the change was successful</returns>
		public bool AllowGroup(string regionName, string groupName)
		{
			Region r = GetRegionByName(regionName);
			if (r != null)
			{
				var temp = r.AllowedGroups;
				temp.Add(groupName);
				r.AllowedGroups = temp;
				return true;
			}
			return false;

		}

		/// <summary>
		/// Removes a group's access to a region
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="groupName">Group name</param>
		/// <returns>Whether the change was successful</returns>
		public bool RemoveGroup(string regionName, string groupName)
		{
			Region r = GetRegionByName(regionName);
			if (r != null)
			{
				var temp = r.AllowedGroups;
				temp.Remove(groupName);
				r.AllowedGroups = temp;
				return true;
			}
			return false;

		}

		/// <summary>
		/// Returns the <see cref="Region"/> with the highest Z index of the given list
		/// </summary>
		/// <param name="regions">List of Regions to compare</param>
		/// <returns></returns>
		public Region GetTopRegion(IEnumerable<Region> regions)
		{
			Region ret = null;
			foreach (Region r in regions)
			{
				if (ret == null)
					ret = r;
				else
				{
					if (r.Z > ret.Z)
						ret = r;
				}
			}
			return ret;
		}
	}
}

