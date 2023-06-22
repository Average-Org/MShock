using Auxiliary;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TShockAPI.Models
{
	public class PlayerBan : BsonModel
	{
		private int _tno;
		public int TicketNumber
		{
			get
			  => _tno;
			set
			{
				_ = this.SaveAsync(x => x.TicketNumber, value);
				_tno = value;
			}
		}

		private string _reason;
		public string Reason
		{
			get => _reason;
			set
			{
				_ = this.SaveAsync(x => x.Reason, value);
				_reason = value;
			}
		}

		private string _whobanned;

		public string WhoBanned
		{
			get => _whobanned;
			set
			{
				_ = this.SaveAsync(x => x.WhoBanned, value);
				_whobanned = value;
			}
		}

		private DateTime _whenbanned;
		
		public DateTime WhenBanned
		{
			get => _whenbanned;
			set
			{
				_ = this.SaveAsync(x => x.WhenBanned, value);
				_whenbanned = value;
			}
		}

		private DateTime _expiryDate;
		public DateTime ExpiryDate
		{
			get => _expiryDate;
			set
			{
				_ = this.SaveAsync(x => x.ExpiryDate, value);
				_expiryDate = value;
			}
		}

		private string _uuid;
		public string UUID
		{
			get => _uuid;
			set
			{
				_ = this.SaveAsync(x => x.UUID, value);
				_uuid = value;
			}
		}

		private string _ip;
		public string IP
		{
			get => _ip;
			set
			{
				_ = this.SaveAsync(x => x.IP, value);
				_ip = value;
			}
		}

		private string _usernameBanned;
		public string UsernameBanned
		{
			get => _usernameBanned;
			set
			{
				_ = this.SaveAsync(x => x.UsernameBanned, value);
				_usernameBanned = value;
			}
		}

		private int _tsid;

		public int TSID
		{
			get => _tsid;
			set
			{
				_ = this.SaveAsync(x => x.TSID, value);
				_tsid = value;
			}
		}

		public PlayerBan()
		{
			// Empty constructor for deserialization
		}

		public PlayerBan(int ticketNumber, string reason, string whoBanned, DateTime whenBanned, DateTime expiryDate, string uuid, string ip, string usernameBanned, int tsid)
		{
			TicketNumber = ticketNumber;
			Reason = reason;
			WhoBanned = whoBanned;
			WhenBanned = whenBanned;
			ExpiryDate = expiryDate;
			UUID = uuid;
			IP = ip;
			UsernameBanned = usernameBanned;
			TSID = tsid;
		}

		public string GetPrettyExpirationString()
		{
			if (ExpiryDate == DateTime.MaxValue)
			{
				return "Never";
			}

			TimeSpan ts = (ExpiryDate - DateTime.UtcNow).Duration(); // Use duration to avoid pesky negatives for expired bans
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}

		/// <summary>
		/// Returns a string in the format dd:mm:hh:ss indicating the time elapsed since the ban was added.
		/// </summary>
		/// <returns></returns>
		public string GetPrettyTimeSinceBanString()
		{
			TimeSpan ts = (DateTime.UtcNow - WhenBanned).Duration();
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}
	}
}
