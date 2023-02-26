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
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TShockAPI.DB.Models;

namespace TShockAPI.DB
{
	/// <summary>
	/// Class that manages bans.
	/// </summary>
	public class PlayerBanManager
	{
		/// <summary>
		/// Retrieves a List of all player bans
		/// </summary>
		public List<PlayerBan> RetrieveAllPlayerBans()
		{
			return StorageProvider.GetMongoCollection<PlayerBan>("PlayerBans").Find(x => true).ToList();
		}


		/// <summary>
		/// Event invoked when a ban is checked for validity
		/// </summary>
		public static event EventHandler<BanEventArgs> OnBanValidate;
		/// <summary>
		/// Event invoked before a ban is added
		/// </summary>
		public static event EventHandler<BanPreAddEventArgs> OnBanPreAdd;
		/// <summary>
		/// Event invoked after a ban is added
		/// </summary>
		public static event EventHandler<BanEventArgs> OnBanPostAdd;

		/// <summary>
		/// Initializes a new instance of the <see cref="TShockAPI.DB.PlayerBanManager"/> class.
		/// </summary>
		/// <param name="db">A valid connection to the TShock database</param>
		public PlayerBanManager(IDbConnection db)
		{
			OnBanValidate += BanValidateCheck;
			OnBanPreAdd += BanAddedCheck;
		}

		internal bool CheckBan(TSPlayer player)
		{
			List<string> identifiers = new List<string>
			{
				$"{Identifier.UUID}{player.UUID}",
				$"{Identifier.Name}{player.Name}",
				$"{Identifier.IP}{player.IP}"
			};

			if (player.Account != null)
			{
				identifiers.Add($"{Identifier.Account}{player.Account.Name}");
			}

			Ban ban = TShock.Bans.Bans.FirstOrDefault(b => identifiers.Contains(b.Value.Identifier) && TShock.Bans.IsValidBan(b.Value, player)).Value;

			if (ban != null)
			{
				if (ban.ExpirationDateTime == DateTime.MaxValue)
				{
					player.Disconnect(GetParticularString("{0} is ban number, {1} is ban reason", $"#{ban.TicketNumber} - You are banned: {ban.Reason}"));
					return true;
				}

				TimeSpan ts = ban.ExpirationDateTime - DateTime.UtcNow;
				player.Disconnect(GetParticularString("{0} is ban number, {1} is ban reason, {2} is a timestamp", $"#{ban.TicketNumber} - You are banned: {ban.Reason} ({ban.GetPrettyExpirationString()} remaining)"));
				return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether or not a ban is valid
		/// </summary>
		/// <param name="ban"></param>
		/// <param name="player"></param>
		/// <returns></returns>
		public bool IsValidBan(Ban ban, TSPlayer player)
		{
			BanEventArgs args = new BanEventArgs
			{
				Ban = ban,
				Player = player
			};

			OnBanValidate?.Invoke(this, args);

			return args.Valid;
		}

		internal void BanValidateCheck(object sender, BanEventArgs args)
		{
			//Only perform validation if the event has not been cancelled before we got here
			if (args.Valid)
			{
				//We consider a ban to be valid if the start time is before now and the end time is after now
				args.Valid = DateTime.UtcNow > args.Ban.BanDateTime && DateTime.UtcNow < args.Ban.ExpirationDateTime;
			}
		}

		internal void BanAddedCheck(object sender, BanPreAddEventArgs args)
		{
			//Only perform validation if the event has not been cancelled before we got here
			if (args.Valid)
			{
				//We consider a ban valid to add if no other *current* bans exist for the identifier provided.
				//E.g., if a previous ban has expired, a new ban is valid.
				//However, if a previous ban on the provided identifier is still in effect, a new ban is not valid
				args.Valid = !Bans.Any(b => b.Value.Identifier == args.Identifier && b.Value.ExpirationDateTime > DateTime.UtcNow);
				args.Message = args.Valid ? null : GetString("The ban is invalid because a current ban for this identifier already exists.");
			}
		}

		/// <summary>
		/// Adds a new ban for the given identifier. Returns a Ban object if the ban was added, else null
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="reason"></param>
		/// <param name="banningUser"></param>
		/// <param name="fromDate"></param>
		/// <param name="toDate"></param>
		/// <returns></returns>
		public AddBanResult InsertBan(string identifier, string reason, string banningUser, DateTime fromDate, DateTime toDate)
		{
			BanPreAddEventArgs args = new BanPreAddEventArgs
			{
				Identifier = identifier,
				Reason = reason,
				BanningUser = banningUser,
				BanDateTime = fromDate,
				ExpirationDateTime = toDate
			};
			return InsertBan(args);
		}

		/// <summary>
		/// Adds a new ban for the given data. Returns a Ban object if the ban was added, else null
		/// </summary>
		/// <param name="args">A predefined instance of <see cref="BanPreAddEventArgs"/></param>
		/// <returns></returns>
		public async Task<AddBanResult> InsertBan(BanPreAddEventArgs args)
		{
			OnBanPreAdd?.Invoke(this, args);

			if (!args.Valid)
			{
				string message = args.Message ?? GetString("The ban was not valid for an unknown reason.");
				return new AddBanResult { Message = message };
			}

			var createdModel = await IModel.CreateAsync(CreateRequest.Bson<PlayerBan>(x =>
			{
				x.IP = args.Identifier
			}));

			if (database.GetSqlType() == SqlType.Mysql)
			{
				query += "SELECT LAST_INSERT_ID();";
			}
			else
			{
				query += "SELECT CAST(last_insert_rowid() as INT);";
			}

			int ticketId = database.QueryScalar<int>(query, args.Identifier, args.Reason, args.BanningUser, args.BanDateTime.Ticks, args.ExpirationDateTime.Ticks);

			if (ticketId == 0)
			{
				return new AddBanResult { Message = GetString("Inserting the ban into the database failed.") };
			}

			Ban b = new Ban(ticketId, args.Identifier, args.Reason, args.BanningUser, args.BanDateTime, args.ExpirationDateTime);
			_bans.Add(ticketId, b);

			OnBanPostAdd?.Invoke(this, new BanEventArgs { Ban = b });

			return new AddBanResult { Ban = b };
		}

		/// <summary>
		/// Attempts to remove a ban. Returns true if the ban was removed or expired. False if the ban could not be removed or expired
		/// </summary>
		/// <param name="ticketNumber">The ticket number of the ban to change</param>
		/// <param name="fullDelete">If true, deletes the ban from the database. If false, marks the expiration time as now, rendering the ban expired. Defaults to false</param>
		/// <returns></returns>
		public bool RemoveBan(int ticketNumber, bool fullDelete = false)
		{
			int rowsModified;
			if (fullDelete)
			{
				rowsModified = database.Query("DELETE FROM PlayerBans WHERE TicketNumber=@0", ticketNumber);
				_bans.Remove(ticketNumber);
			}
			else
			{
				rowsModified = database.Query("UPDATE PlayerBans SET Expiration=@0 WHERE TicketNumber=@1", DateTime.UtcNow.Ticks, ticketNumber);
				_bans[ticketNumber].ExpirationDateTime = DateTime.UtcNow;
			}

			return rowsModified > 0;
		}

		/// <summary>
		/// Retrieves a single ban from a ban's ticket number
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Ban GetBanById(int id)
		{
			if (Bans.ContainsKey(id))
			{
				return Bans[id];
			}

			using (var reader = database.QueryReader("SELECT * FROM PlayerBans WHERE TicketNumber=@0", id))
			{
				if (reader.Read())
				{
					var ticketNumber = reader.Get<int>("TicketNumber");
					var identifier = reader.Get<string>("Identifier");
					var reason = reader.Get<string>("Reason");
					var banningUser = reader.Get<string>("BanningUser");
					var date = reader.Get<long>("Date");
					var expiration = reader.Get<long>("Expiration");

					return new Ban(ticketNumber, identifier, reason, banningUser, date, expiration);
				}
			}

			return null;
		}

		/// <summary>
		/// Retrieves an enumerable of all bans for a given identifier
		/// </summary>
		/// <param name="identifier">Identifier to search with</param>
		/// <param name="currentOnly">Whether or not to exclude expired bans</param>
		/// <returns></returns>
		public IEnumerable<Ban> RetrieveBansByIdentifier(string identifier, bool currentOnly = true)
		{
			string query = "SELECT * FROM PlayerBans WHERE Identifier=@0";
			if (currentOnly)
			{
				query += $" AND Expiration > {DateTime.UtcNow.Ticks}";
			}

			using (var reader = database.QueryReader(query, identifier))
			{
				while (reader.Read())
				{
					var ticketNumber = reader.Get<int>("TicketNumber");
					var ident = reader.Get<string>("Identifier");
					var reason = reader.Get<string>("Reason");
					var banningUser = reader.Get<string>("BanningUser");
					var date = reader.Get<long>("Date");
					var expiration = reader.Get<long>("Expiration");

					yield return new Ban(ticketNumber, ident, reason, banningUser, date, expiration);
				}
			}
		}

		/// <summary>
		/// Retrieves an enumerable of bans for a given set of identifiers
		/// </summary>
		/// <param name="currentOnly">Whether or not to exclude expired bans</param>
		/// <param name="identifiers"></param>
		/// <returns></returns>
		public IEnumerable<Ban> GetBansByIdentifiers(bool currentOnly = true, params string[] identifiers)
		{
			//Generate a sequence of '@0, @1, @2, ... etc'
			var parameters = string.Join(", ", Enumerable.Range(0, identifiers.Count()).Select(p => $"@{p}"));

			string query = $"SELECT * FROM PlayerBans WHERE Identifier IN ({parameters})";
			if (currentOnly)
			{
				query += $" AND Expiration > {DateTime.UtcNow.Ticks}";
			}

			using (var reader = database.QueryReader(query, identifiers))
			{
				while (reader.Read())
				{
					var ticketNumber = reader.Get<int>("TicketNumber");
					var identifier = reader.Get<string>("Identifier");
					var reason = reader.Get<string>("Reason");
					var banningUser = reader.Get<string>("BanningUser");
					var date = reader.Get<long>("Date");
					var expiration = reader.Get<long>("Expiration");

					yield return new Ban(ticketNumber, identifier, reason, banningUser, date, expiration);
				}
			}
		}
		/// <summary>
		/// Removes all bans from the database
		/// </summary>
		/// <returns><c>true</c>, if bans were cleared, <c>false</c> otherwise.</returns>
		public bool ClearBans()
		{
			try
			{
				StorageProvider.GetMongoCollection<PlayerBan>("PlayerBans").DeleteMany(x => true);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		internal Dictionary<BanSortMethod, string> SortToOrderByMap = new Dictionary<BanSortMethod, string>
		{
			{ BanSortMethod.AddedNewestToOldest, "Date DESC" },
			{ BanSortMethod.AddedOldestToNewest, "Date ASC" },
			{ BanSortMethod.ExpirationSoonestToLatest, "Expiration ASC" },
			{ BanSortMethod.ExpirationLatestToSoonest, "Expiration DESC" }
		};
	}

	/// <summary>
	/// Enum containing sort options for ban retrieval
	/// </summary>
	public enum BanSortMethod
	{
		/// <summary>
		/// Bans will be sorted on expiration date, from soonest to latest
		/// </summary>
		ExpirationSoonestToLatest,
		/// <summary>
		/// Bans will be sorted on expiration date, from latest to soonest
		/// </summary>
		ExpirationLatestToSoonest,
		/// <summary>
		/// Bans will be sorted by the date they were added, from newest to oldest
		/// </summary>
		AddedNewestToOldest,
		/// <summary>
		/// Bans will be sorted by the date they were added, from oldest to newest
		/// </summary>
		AddedOldestToNewest,
		/// <summary>
		/// Bans will be sorted by their ticket number
		/// </summary>
		TicketNumber
	}

	/// <summary>
	/// Result of an attempt to add a ban
	/// </summary>
	public class AddBanResult
	{
		/// <summary>
		/// Message generated from the attempt
		/// </summary>
		public string Message { get; set; }
		/// <summary>
		/// Ban object generated from the attempt, or null if the attempt failed
		/// </summary>
		public PlayerBan Ban { get; set; }
	}

	/// <summary>
	/// Event args used for completed bans
	/// </summary>
	public class BanEventArgs : EventArgs
	{
		/// <summary>
		/// Complete ban object
		/// </summary>
		public PlayerBan Ban { get; set; }

		/// <summary>
		/// Player ban is being applied to
		/// </summary>
		public TSPlayer Player { get; set; }

		/// <summary>
		/// Whether or not the operation should be considered to be valid
		/// </summary>
		public bool Valid { get; set; } = true;
	}

	/// <summary>
	/// Event args used for ban data prior to a ban being formalized
	/// </summary>
	///

	public enum BanType
	{
		IP = 0,
		Account = 1,
		UUID = 2,
	}
	
	public class BanPreAddEventArgs : EventArgs
	{
		/// <summary>
		/// An IP address to ban, may or may not be empty
		/// </summary>
		public string IP { get; set; }

		/// <summary>
		/// An account name to ban, may or may not be empty
		/// </summary>
		public string AccountName { get; set; }

		/// <summary>
		/// A UUID to ban, may or may not be empty
		/// </summary>
		public string UUID { get; set; }

		/// <summary>
		/// Type of ban, allowing us to know whether it was an IP / Account / UUID ban
		/// </summary>
		public BanType Type { get; set; }

		/// <summary>
		/// Gets or sets the ban reason.
		/// </summary>
		/// <value>The ban reason.</value>
		public string Reason { get; set; }

		/// <summary>
		/// Gets or sets the name of the user who added this ban entry.
		/// </summary>
		/// <value>The banning user.</value>
		public string BanningUser { get; set; }

		/// <summary>
		/// DateTime from which the ban will take effect
		/// </summary>
		public DateTime BanDateTime { get; set; }

		/// <summary>
		/// DateTime at which the ban will end
		/// </summary>
		public DateTime ExpirationDateTime { get; set; }

		/// <summary>
		/// Whether or not the operation should be considered to be valid
		/// </summary>
		public bool Valid { get; set; } = true;

		/// <summary>
		/// Optional message to explain why the event was invalidated, if it was
		/// </summary>
		public string Message { get; set; }
		}
	}


