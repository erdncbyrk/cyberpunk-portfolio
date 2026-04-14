namespace AtumControlPanel.Models
{
    public class AccountInfo
    {
        public int AccountUniqueNumber { get; set; }
        public string AccountName { get; set; } = "";
        public string Password { get; set; } = "";
        public int AccountType { get; set; }
        public bool IsBlocked { get; set; }
        public bool ChattingBlocked { get; set; }
        public DateTime? RegisteredDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string BlockedReason { get; set; } = "";

        public const int RACE_OPERATION = 0x0080;
        public const int RACE_GAMEMASTER = 0x0100;
        public const int RACE_MONITOR = 0x0200;

        public bool IsAdmin => (AccountType & RACE_OPERATION) != 0;
        public bool IsGM => (AccountType & RACE_GAMEMASTER) != 0;
        public bool IsMonitor => (AccountType & RACE_MONITOR) != 0;

        public string GetRoleString()
        {
            var roles = new List<string>();
            if (IsAdmin) roles.Add("Admin");
            if (IsGM) roles.Add("GM");
            if (IsMonitor) roles.Add("Monitor");
            return roles.Count > 0 ? string.Join(", ", roles) : "Player";
        }
    }

    public class CharacterInfo
    {
        public int CharacterUniqueNumber { get; set; }
        public string CharacterName { get; set; } = "";
        public string AccountName { get; set; } = "";
        public int Level { get; set; }
        public long Experience { get; set; }
        public int Race { get; set; }
        public int UnitKind { get; set; }
        public int MapIndex { get; set; }
        public int ChannelIndex { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public long TotalPlayTime { get; set; }
        public int PKPoint { get; set; }
        public int Fame { get; set; }
        public long Money { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class BlockedAccountInfo
    {
        public string AccountName { get; set; } = "";
        public int BlockedType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string AdminAccountName { get; set; } = "";
        public string BlockedReason { get; set; } = "";
    }

    public class GuildInfo
    {
        public int GuildUniqueNumber { get; set; }
        public string GuildName { get; set; } = "";
        public string MasterCharacterName { get; set; } = "";
        public int MemberCount { get; set; }
        public int GuildFame { get; set; }
        public int GuildLevel { get; set; }
        public int Influence { get; set; }
        public DateTime? CreateDate { get; set; }
        public long GuildMoney { get; set; }
    }

    public class GuildMemberInfo
    {
        public int CharacterUID { get; set; }
        public string CharacterName { get; set; } = "";
        public int Rank { get; set; }
        public int Level { get; set; }
        public int UnitKind { get; set; }
        public DateTime? JoinDate { get; set; }
    }

    public class ItemInfo
    {
        public int UniqueNumber { get; set; }
        public int ItemNum { get; set; }
        public string ItemName { get; set; } = "";
        public int CurrentCount { get; set; }
        public int Possess { get; set; }
        public int ItemWindowIndex { get; set; }
        public byte Prefix { get; set; }
        public byte Suffix { get; set; }
        public int RareIndex { get; set; }
        public int CharacterUniqueNumber { get; set; }
    }

    public class LogEntry
    {
        public DateTime LogDate { get; set; }
        public string LogType { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string Detail { get; set; } = "";
        public string IPAddress { get; set; } = "";
    }

    public class CashShopItem
    {
        public int ItemNum { get; set; }
        public string ItemName { get; set; } = "";
        public int Price { get; set; }
        public int Category { get; set; }
        public bool IsNew { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsHot { get; set; }
        public int LimitedCount { get; set; }
        public int SoldCount { get; set; }
    }

    public class NoticeSettings
    {
        public bool UsingFlag { get; set; }
        public int LoopSec { get; set; } = 1800; // 30 min default
        public int IntervalSec { get; set; } = 60;  // 60 sec default
        public string EditorAccountName { get; set; } = "";
    }

    public class NoticeInfo
    {
        public int OrderIndex { get; set; }
        public string NoticeString { get; set; } = "";
    }

    public class HappyHourEventInfo
    {
        public int EventUID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int BonusType { get; set; }
        public int BonusValue { get; set; }
        public bool IsActive { get; set; }
    }

    // ─── StrategyPoint ───────────────────────────
    public class StrategyPointSchedule
    {
        public int DayOfWeek { get; set; } // 0=Sunday .. 6=Saturday
        public string DayName => DayOfWeek switch { 0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday", 4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "?" };
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int CountBCU { get; set; } = 15;
        public int CountANI { get; set; } = 15;
    }

    public class StrategyPointMapInfo
    {
        public int MapIndex { get; set; }
        public string MapName { get; set; } = "";
        public string Influence { get; set; } = ""; // BCU or ANI
        public DateTime? SummonTime { get; set; }
        public string Creation { get; set; } = ""; // Waiting, Finish, etc.
    }

    // ─── Event Monster ──────────────────────────
    public class EventMonster
    {
        public int EventMonsterUID { get; set; }
        public int ServerGroupID { get; set; }          // 0=All servers
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int SummonerMapIndex { get; set; }       // 0=Any map
        public int SummonerReqMinLevel { get; set; }    // 0=No check
        public int SummonerReqMaxLevel { get; set; }    // 0=No check
        public int SummonerExceptMonster { get; set; }  // Bit flags
        public int SummonMonsterNum { get; set; }       // Monster ID to spawn
        public int SummonMonsterCount { get; set; }     // 1-100
        public int SummonDelayTime { get; set; }        // 1-600 seconds
        public int SummonProbability { get; set; }      // 0-10000

        // Exception flag helpers
        public bool ExceptObjectMonster
        {
            get => (SummonerExceptMonster & 0x01) != 0;
            set => SummonerExceptMonster = value ? (SummonerExceptMonster | 0x01) : (SummonerExceptMonster & ~0x01);
        }
        public bool ExceptInfluenceMonster
        {
            get => (SummonerExceptMonster & 0x02) != 0;
            set => SummonerExceptMonster = value ? (SummonerExceptMonster | 0x02) : (SummonerExceptMonster & ~0x02);
        }
        public bool ExceptNotAttackMonster
        {
            get => (SummonerExceptMonster & 0x04) != 0;
            set => SummonerExceptMonster = value ? (SummonerExceptMonster | 0x04) : (SummonerExceptMonster & ~0x04);
        }

        // Status helper
        public string Status
        {
            get
            {
                var now = DateTime.Now;
                if (now < StartDateTime) return "Scheduled";
                if (now >= StartDateTime && now <= EndDateTime) return "Active";
                return "Expired";
            }
        }
    }

    // ─── Declaration of War (Mothership War) ─────
    public class DeclarationOfWarInfo
    {
        public int Influence { get; set; }          // 2=BCU(VCN), 4=ANI
        public int MSWarStep { get; set; }          // 1-5 normal, 99=next leader
        public int NCP { get; set; }
        public int MSNum { get; set; }              // Boss monster UID
        public int MSAppearanceMap { get; set; }    // Map where boss appears
        public DateTime? MSWarStepStartTime { get; set; }
        public DateTime? MSWarStepEndTime { get; set; }
        public DateTime? MSWarStartTime { get; set; }
        public DateTime? MSWarEndTime { get; set; }
        public int SelectCount { get; set; }        // Remaining time selections (max 3)
        public bool GiveUp { get; set; }
        public int MSWarEndState { get; set; }      // 0=NotStart, 1=Before, 2=Waring, 11=Win, 21=Loss

        public const int MSWAR_NOT_START = 0;
        public const int MSWARING_BEFORE = 1;
        public const int MSWARING = 2;
        public const int MSWAR_END_WIN = 11;
        public const int MSWAR_END_LOSS = 21;
        public const int MSWAR_NEXT_LEADER_STEP = 99;
        public const int MSWAR_FINAL_STEP = 5;

        public string InfluenceName => Influence == 2 ? "BCU" : Influence == 4 ? "ANI" : $"?({Influence})";

        public string ResultString => MSWarEndState switch
        {
            MSWAR_NOT_START => "-",
            MSWARING_BEFORE => "Before",
            MSWARING => "In Progress",
            MSWAR_END_WIN => "WIN",
            MSWAR_END_LOSS => "LOSS",
            _ => $"?({MSWarEndState})"
        };
    }

    public class DeclarationOfWarForbidTime
    {
        public int DayOfWeek { get; set; }          // 0=Sun..6=Sat
        public DateTime? ForbidStartTime { get; set; }
        public DateTime? ForbidEndTime { get; set; }

        public string DayName => DayOfWeek switch
        {
            0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday",
            4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "?"
        };
    }

    // ═══════════════════════════════════════════════
    //  BLOCKED ACCOUNTS / ANTI-CHEAT
    // ═══════════════════════════════════════════════
    public class BlockedAccount
    {
        public string AccountName { get; set; } = "";
        public int BlockedType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AdminAccountName { get; set; } = "";
        public string BlockedReason { get; set; } = "";
        public string BlockedReasonForOnlyAdmin { get; set; } = "";

        // Block type enum matching C++ EN_BLOCKED_TYPE
        public string BlockedTypeName => BlockedType switch
        {
            0 => "Unknown",
            1 => "Normal",
            2 => "Money Related",
            3 => "Item Related",
            4 => "SpeedHack",
            5 => "Chat Related",
            6 => "Game Bug",
            7 => "AutoBlock: MemHack",
            8 => "AutoBlock: SpdHack",
            _ => $"Type({BlockedType})"
        };

        public bool IsAutoBlock => BlockedType == 7 || BlockedType == 8;
        public bool IsExpired => EndDate < DateTime.Now;
        public bool IsPermanent => EndDate.Year >= 2100;

        public string StatusText => IsExpired ? "Expired" : (IsPermanent ? "Permanent" : $"Until {EndDate:yyyy-MM-dd}");
    }

    public class AntiCheatConfig
    {
        public string ConfigKey { get; set; } = "";
        public int ConfigValue { get; set; }
        public string Description { get; set; } = "";
    }

    public class HackAttemptLog
    {
        public long LogID { get; set; }
        public string AccountName { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string HackType { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime DetectedAt { get; set; }
        public string MapName { get; set; } = "";
        public bool WasBlocked { get; set; }
    }
}
