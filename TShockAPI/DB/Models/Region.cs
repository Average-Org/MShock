using Auxiliary;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TShockAPI.DB.Models
{
	public class Region : BsonModel
	{
		private string _name = string.Empty;
		public string Name
		{
			get
			  => _name;
			set
			{
				_ = this.SaveAsync(x => x.Name, value);
				_name = value;
			}
		}

		private Rectangle _area { get; set; }
		public Rectangle Area
		{
			get
			  => _area;
			set
			{
				_ = this.SaveAsync(x => x.Area, value);
				_area = value;
			}
		}

		private string _owner = string.Empty;
		public string Owner
		{
			get => _owner;
			set
			{
				_ = this.SaveAsync(x => x.Owner, value);
				_owner = value;
			}
		}

		private bool _disableBuild;
		public bool DisableBuild
		{
			get => _disableBuild;
			set
			{
				_ = this.SaveAsync(x => x.DisableBuild, value);
				_disableBuild = value;
			}
		}

		private string _worldID = string.Empty;
		public string WorldID
		{
			get => _worldID;
			set
			{

				_ = this.SaveAsync(x => x.WorldID, value);
				_worldID = value;
			}
		}

		private List<int> _allowedIDs = new List<int>();
		public List<int> AllowedIDs
		{
			get => _allowedIDs;
			set
			{

				_ = this.SaveAsync(x => x.AllowedIDs, value);
				_allowedIDs = value;
			}
		}

		private List<string> _allowedGroups = new List<string>();
		public List<string> AllowedGroups
		{
			get => _allowedGroups;
			set
			{
				_ = this.SaveAsync(x => x.AllowedGroups, value);
				_allowedGroups = value;
			}
		}

		private int _z { get; set; }
		public int Z
		{
			get => _z;
			set
			{
				_ = this.SaveAsync(x => x.Z, value);
				_z = value;
			}
		}

		/// <summary>
		/// Checks if a given (x, y) coordinate is in the region's area
		/// </summary>
		/// <param name="x">X coordinate to check</param>
		/// <param name="y">Y coordinate to check</param>
		/// <returns>Whether the coordinate exists in the region's area</returns>
		public bool InArea(int x, int y) //overloaded with x,y
		{
			/*
			DO NOT CHANGE TO Area.Contains(x, y)!
			Area.Contains does not account for the right and bottom 'border' of the rectangle,
			which results in regions being trimmed.
			*/
			return x >= Area.X && x <= Area.X + Area.Width && y >= Area.Y && y <= Area.Y + Area.Height;
		}

		/// <summary>
		/// Sets the group names which are allowed to use the region
		/// </summary>
		/// <param name="groups">String of group names to set</param>
		public void SetAllowedGroups(string groups)
		{
			// prevent null pointer exceptions
			if (!string.IsNullOrEmpty(groups))
			{
				List<String> groupList = groups.Split(',').ToList();

				for (int i = 0; i < groupList.Count; i++)
				{
					groupList[i] = groupList[i].Trim();
				}

				AllowedGroups = groupList;
			}
		}

		/// <summary>
		/// Checks if a given point is in the region's area
		/// </summary>
		/// <param name="point">Point to check</param>
		/// <returns>Whether the point exists in the region's area</returns>
		public bool InArea(Rectangle point)
		{
			return InArea(point.X, point.Y);
		}

		/// <summary>
		/// Adds a user to a region's allowed user list
		/// </summary>
		/// <param name="regionName">Name of the region to modify</param>
		/// <param name="userName">Username to add</param>
		/// <returns>true if added successfully</returns>
		public bool AddNewUser(string regionName, string userName)
		{
			try
			{
				string mergedIDs = string.Empty;

				Region region = TShock.Regions.GetRegionByName(regionName);
				var temp = region.AllowedIDs;

				int id = TShock.UserAccounts.GetUserAccountByName(userName).ID;

				temp.Add(id);
				region.AllowedIDs = temp;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Removes a group's access to the region
		/// </summary>
		/// <param name="groupName">Group name to remove</param>
		/// <returns></returns>
		public bool RemoveGroup(string groupName)
		{
			var success = AllowedGroups.Remove(groupName);
			if (success == true)
			{
				_ = this.SaveAsync(x => x.AllowedGroups, AllowedGroups);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Sets the Z index of a given region
		/// </summary>
		/// <param name="name">Region name</param>
		/// <param name="z">New Z index</param>
		/// <returns>Whether the change was successful</returns>
		public bool SetZ(string name, int z)
		{
			try
			{
				var region = TShock.Regions.GetRegionByName(name);
				if (region != null)
					region.Z = z;
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}


		/// <summary>
		/// Checks if a given player has permission to build in the region
		/// </summary>
		/// <param name="ply">Player to check permissions with</param>
		/// <returns>Whether the player has permission</returns>
		public bool HasPermissionToBuildInRegion(TSPlayer ply)
		{
			/*DO THIS LATER, ONCE CONFIGURATION HAS BEEN RE-DONE*/
			/*	if (!DisableBuild)
				{
					return true;
				}*/
			if (!ply.IsLoggedIn)
			{
				if (!ply.HasBeenNaggedAboutLoggingIn)
				{
					ply.SendMessage(GetString("You must be logged in to take advantage of protected regions."), Color.Red);
					ply.HasBeenNaggedAboutLoggingIn = true;
				}
				return false;
			}

			return ply.HasPermission(Permissions.editregion) || AllowedIDs.Contains(ply.Account.ID) || AllowedGroups.Contains(ply.Group.Name) || Owner == ply.Account.Name;
		}

		/// <summary>
		/// Sets the user IDs which are allowed to use the region
		/// </summary>
		/// <param name="ids">String of IDs to set</param>
		public void SetAllowedIDs(String ids)
		{
			String[] idArr = ids.Split(',');
			List<int> idList = new List<int>();

			foreach (String id in idArr)
			{
				int i = 0;
				if (int.TryParse(id, out i) && i != 0)
				{
					idList.Add(i);
				}
			}
			AllowedIDs = idList;
		}

		/// <summary>
		/// Removes a user's access to the region
		/// </summary>
		/// <param name="id">User ID to remove</param>
		/// <returns>true if the user was found and removed from the region's allowed users</returns>
		public bool RemoveID(int id)
		{
			var success = AllowedIDs.Remove(id);
			if (success == true)
			{
				_ = this.SaveAsync(x => x.AllowedIDs, AllowedIDs);
				return true;
			}
			return false;
		}

		public Region(Rectangle region, string name, string owner, bool disablebuild, string RegionWorldIDz, int z)
			: this()
		{
			Area = region;
			Name = name;
			Owner = owner;
			DisableBuild = disablebuild;
			WorldID = RegionWorldIDz;
			Z = z;
		}

		public Region()
		{
			Area = Rectangle.Empty;
			Name = string.Empty;
			DisableBuild = true;
			WorldID = string.Empty;
			AllowedIDs = new List<int>();
			AllowedGroups = new List<string>();
			Z = 0;
		}

	}
}
