using Auxiliary;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TShockAPI.DB.Models
{
	public class PlayerBan : BsonModel
	{
		private string _accName = String.Empty;

		public string AccountName
		{
			get
			  => _accName;
			set
			{
				_ = this.SaveAsync(x => x.AccountName, value);
				_accName = value;
			}
		}



		private string _ipAddr = String.Empty;

		public string IP
		{
			get
			  => _ipAddr;
			set
			{
				_ = this.SaveAsync(x => x.IP, value);
				_ipAddr = value;
			}
		}

		private string _reason = String.Empty;

		public string Reason
		{
				get
				  => _reason;
				set
				{
					_ = this.SaveAsync(x => x.Reason, value);
					_reason = value;
				}
		}

		private string _bannedBy = String.Empty;

		public string BannedBy
		{
				get
				  => _bannedBy;
				set
				{
					_ = this.SaveAsync(x => x.BannedBy, value);
					_bannedBy = value;
				}
		}

		private DateTime _banDate = DateTime.UtcNow;

		public DateTime BannedOn
		{
			get => _banDate;
			set
			{
				_ = this.SaveAsync(x => x.BannedOn, value);
				_banDate = value;
			}
		}

		private DateTime _expirationDate = DateTime.MaxValue;

		public DateTime ExpirationDate
		{
			get => _expirationDate;
			set
			{
				_ = this.SaveAsync(x => x.ExpirationDate, value);
				_expirationDate = value;
			}
		}
		
		private string _uuid = String.Empty;
		public string UUID
		{
			get => _uuid;
			set
			{
				_ = this.SaveAsync(x => x.UUID, value);
				_uuid = value;
			}
		}

		public string GetPrettyExpirationString()
		{
			if (ExpirationDate == DateTime.MaxValue)
			{
				return "Never";
			}

			TimeSpan ts = (ExpirationDate - DateTime.UtcNow).Duration(); // Use duration to avoid pesky negatives for expired bans
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}

		public string GetPrettyTimeSinceBanString()
		{
			TimeSpan ts = (DateTime.UtcNow - BannedOn).Duration();
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}
	}
}
