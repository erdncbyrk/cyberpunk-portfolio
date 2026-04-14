using System.Data;
using System.Data.SqlClient;
using AtumControlPanel.Models;

namespace AtumControlPanel.Services
{
    public partial class DatabaseService
    {
        private readonly AppConfig _config;
        public DatabaseService(AppConfig config) => _config = config;
        public string LastError { get; set; } = "";

        /// <summary>Expose game connection string for diagnostics.</summary>
        public string GetConnectionStringForDiag() => _config.GetGameConnectionString();

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex) { LastError = ex.Message; return false; }
        }

        public async Task<List<string>> GetDatabaseListAsync()
        {
            var list = new List<string>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(r.GetString(0));
            }
            catch { }
            return list;
        }

        // ─── Authentication ──────────────────────────────
        public async Task<AccountInfo?> AuthenticateAdminAsync(string username, string password)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT TOP 1 * FROM td_Account WHERE AccountName = @name", conn);
                cmd.Parameters.AddWithValue("@name", username);
                using var r = await cmd.ExecuteReaderAsync();

                var columns = new List<string>();
                for (int i = 0; i < r.FieldCount; i++) columns.Add(r.GetName(i));

                if (!await r.ReadAsync()) { LastError = $"Account '{username}' not found."; return null; }

                var account = new AccountInfo { AccountName = username };
                foreach (var col in columns)
                {
                    try
                    {
                        int idx = r.GetOrdinal(col);
                        if (r.IsDBNull(idx)) continue;
                        switch (col.ToLower())
                        {
                            case "accountuniquenumber": case "uid": case "uniquenumber":
                                account.AccountUniqueNumber = Convert.ToInt32(r.GetValue(idx)); break;
                            case "accounttype":
                                account.AccountType = Convert.ToInt32(r.GetValue(idx)); break;
                            case "password":
                                account.Password = r.GetValue(idx).ToString() ?? ""; break;
                            case "isblocked": case "blocked":
                                account.IsBlocked = Convert.ToBoolean(r.GetValue(idx)); break;
                            case "registereddate": case "regdate": case "createdate":
                                account.RegisteredDate = Convert.ToDateTime(r.GetValue(idx)); break;
                            case "lastlogindate": case "lastlogin":
                                account.LastLoginDate = Convert.ToDateTime(r.GetValue(idx)); break;
                        }
                    }
                    catch { }
                }

                LastError = $"AccountType=0x{account.AccountType:X4}, IsAdmin={account.IsAdmin}, IsGM={account.IsGM}";
                return (account.IsAdmin || account.IsGM) ? account : null;
            }
            catch (Exception ex) { LastError = $"DB Error: {ex.Message}"; return null; }
        }

        // ─── Dashboard ──────────────────────────────────
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                stats.TotalAccounts = await SafeCountAsync(conn, "SELECT COUNT(*) FROM td_Account");
                stats.BannedAccounts = await SafeCountAsync(conn, "SELECT COUNT(*) FROM td_Account WHERE IsBlocked = 1");
                stats.TodayRegistrations = await SafeCountAsync(conn, "SELECT COUNT(*) FROM td_Account WHERE CAST(RegisteredDate AS DATE) = CAST(GETDATE() AS DATE)");

                using var conn2 = new SqlConnection(_config.GetGameConnectionString());
                await conn2.OpenAsync();
                stats.TotalCharacters = await SafeCountAsync(conn2, "SELECT COUNT(*) FROM td_Character");
                stats.TotalGuilds = await SafeCountAsync(conn2, "SELECT COUNT(*) FROM td_Guild");
                stats.TotalMoney = await SafeLongAsync(conn2, "SELECT ISNULL(SUM(CAST(Money AS BIGINT)),0) FROM td_Character");
            }
            catch (Exception ex) { stats.Error = ex.Message; }
            return stats;
        }

        // ─── Account Management ─────────────────────────
        public async Task<(List<AccountInfo> results, string info)> SearchAccountsAsync(string query)
        {
            var list = new List<AccountInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT TOP 200 * FROM td_Account WHERE AccountName LIKE @q ORDER BY AccountName", conn);
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
                using var r = await cmd.ExecuteReaderAsync();

                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new AccountInfo
                    {
                        AccountName = SafeStr(r, colMap, "AccountName") ?? "",
                        AccountUniqueNumber = SafeInt(r, colMap, "AccountUniqueNumber"),
                        AccountType = SafeInt(r, colMap, "AccountType"),
                        IsBlocked = SafeBool(r, colMap, "IsBlocked"),
                        ChattingBlocked = SafeBool(r, colMap, "ChattingBlocked"),
                        RegisteredDate = SafeDate(r, colMap, "RegisteredDate"),
                        LastLoginDate = SafeDate(r, colMap, "LastLoginDate")
                    });
                }
                return (list, $"{list.Count} results found");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        public async Task<bool> BlockAccountAsync(string accountName, string reason, string adminName, DateTime? endDate)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE td_Account SET IsBlocked = 1 WHERE AccountName = @name", conn);
                cmd.Parameters.AddWithValue("@name", accountName);
                await cmd.ExecuteNonQueryAsync();

                try
                {
                    using var cmd2 = new SqlCommand(
                        "INSERT INTO td_BlockedAccount (AccountName, BlockedType, StartDate, EndDate, AdminAccountName, BlockedReason) " +
                        "VALUES (@name, 1, GETDATE(), @end, @admin, @reason)", conn);
                    cmd2.Parameters.AddWithValue("@name", accountName);
                    cmd2.Parameters.AddWithValue("@end", (object?)endDate ?? DBNull.Value);
                    cmd2.Parameters.AddWithValue("@admin", adminName);
                    cmd2.Parameters.AddWithValue("@reason", reason);
                    await cmd2.ExecuteNonQueryAsync();
                }
                catch { }
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> SetChatBlockAsync(string accountName, bool blocked)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand($"UPDATE td_Account SET ChattingBlocked = @val WHERE AccountName = @name", conn);
                cmd.Parameters.AddWithValue("@val", blocked);
                cmd.Parameters.AddWithValue("@name", accountName);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        public async Task<List<BlockedAccountInfo>> GetBlockedAccountsAsync()
        {
            var list = new List<BlockedAccountInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT a.AccountName, b.BlockedReason, b.AdminAccountName, b.StartDate, b.EndDate " +
                    "FROM td_Account a LEFT JOIN td_BlockedAccount b ON a.AccountName = b.AccountName " +
                    "WHERE a.IsBlocked = 1 ORDER BY b.StartDate DESC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BlockedAccountInfo
                    {
                        AccountName = r.IsDBNull(0) ? "" : r.GetString(0),
                        BlockedReason = r.IsDBNull(1) ? "" : r.GetString(1),
                        AdminAccountName = r.IsDBNull(2) ? "" : r.GetString(2),
                        StartDate = r.IsDBNull(3) ? null : r.GetDateTime(3),
                        EndDate = r.IsDBNull(4) ? null : r.GetDateTime(4)
                    });
                }
            }
            catch { }
            return list;
        }

        // ─── Character Management ───────────────────────
        public async Task<(List<CharacterInfo> results, string info)> SearchCharactersAsync(string query, bool byAccount = false)
        {
            var list = new List<CharacterInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                string where = byAccount ? "AccountName LIKE @q" : "CharacterName LIKE @q";
                using var cmd = new SqlCommand($"SELECT TOP 200 * FROM td_Character WHERE {where}", conn);
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
                using var r = await cmd.ExecuteReaderAsync();

                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new CharacterInfo
                    {
                        CharacterUniqueNumber = SafeInt(r, colMap, "UniqueNumber"),
                        CharacterName = SafeStr(r, colMap, "CharacterName") ?? "",
                        AccountName = SafeStr(r, colMap, "AccountName") ?? "",
                        Level = SafeInt(r, colMap, "Level"),
                        Experience = SafeLong(r, colMap, "Experience"),
                        Race = SafeInt(r, colMap, "Race"),
                        UnitKind = SafeInt(r, colMap, "UnitKind"),
                        TotalPlayTime = SafeLong(r, colMap, "TotalPlayTime"),
                        PKPoint = SafeInt(r, colMap, "PKPoint"),
                        Fame = SafeInt(r, colMap, "Fame"),
                        Money = SafeLong(r, colMap, "Money"),
                        MapIndex = SafeInt(r, colMap, "MapIndex"),
                        PosX = SafeFloat(r, colMap, "PosX"),
                        PosY = SafeFloat(r, colMap, "PosY"),
                        PosZ = SafeFloat(r, colMap, "PosZ")
                    });
                }
                return (list, $"{list.Count} characters found");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        public async Task<bool> SetCharacterLevelAsync(int uid, int level)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE td_Character SET [Level] = @lvl WHERE UniqueNumber = @uid", conn);
                cmd.Parameters.AddWithValue("@lvl", (byte)level);
                cmd.Parameters.AddWithValue("@uid", uid);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        public async Task<bool> SetCharacterMoneyAsync(int uid, long money)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE td_Character SET Money = @money WHERE UniqueNumber = @uid", conn);
                cmd.Parameters.AddWithValue("@money", money);
                cmd.Parameters.AddWithValue("@uid", uid);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        public async Task<bool> SetCharacterExperienceAsync(int uid, long exp)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE td_Character SET Experience = @exp WHERE UniqueNumber = @uid", conn);
                cmd.Parameters.AddWithValue("@exp", exp);
                cmd.Parameters.AddWithValue("@uid", uid);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        public async Task<bool> SetCharacterFameAsync(int uid, int fame)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE td_Character SET Fame = @fame WHERE UniqueNumber = @uid", conn);
                cmd.Parameters.AddWithValue("@fame", fame);
                cmd.Parameters.AddWithValue("@uid", uid);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        // ─── Character Items ────────────────────────────
        public async Task<(List<ItemInfo> results, string info)> GetCharacterItemsAsync(int characterUID)
        {
            var list = new List<ItemInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT * FROM td_Store WHERE CharacterUniqueNumber = @uid ORDER BY ItemWindowIndex, Possess", conn);
                cmd.Parameters.AddWithValue("@uid", characterUID);
                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new ItemInfo
                    {
                        UniqueNumber = SafeInt(r, colMap, "UniqueNumber"),
                        ItemNum = SafeInt(r, colMap, "ItemNum"),
                        CurrentCount = SafeInt(r, colMap, "CurrentCount"),
                        Possess = SafeInt(r, colMap, "Possess"),
                        ItemWindowIndex = SafeInt(r, colMap, "ItemWindowIndex"),
                        Prefix = (byte)SafeInt(r, colMap, "Prefix"),
                        Suffix = (byte)SafeInt(r, colMap, "Suffix"),
                        RareIndex = SafeInt(r, colMap, "RareIndex"),
                        CharacterUniqueNumber = characterUID
                    });
                }
                return (list, $"{list.Count} items found");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        public async Task<bool> DeleteItemAsync(int itemUID)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM td_Store WHERE UniqueNumber = @uid", conn);
                cmd.Parameters.AddWithValue("@uid", itemUID);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        // ─── Guild Management ───────────────────────────
        public async Task<(List<GuildInfo> results, string info)> SearchGuildsAsync(string query)
        {
            var list = new List<GuildInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT TOP 200 * FROM td_Guild WHERE GuildName LIKE @q ORDER BY GuildName", conn);
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new GuildInfo
                    {
                        GuildUniqueNumber = SafeInt(r, colMap, "UniqueNumber"),
                        GuildName = SafeStr(r, colMap, "GuildName") ?? "",
                        MasterCharacterName = SafeStr(r, colMap, "MasterCharacterName") ?? "",
                        MemberCount = SafeInt(r, colMap, "MemberCount"),
                        GuildFame = SafeInt(r, colMap, "GuildFame"),
                        GuildLevel = SafeInt(r, colMap, "GuildLevel"),
                        Influence = SafeInt(r, colMap, "Influence"),
                        CreateDate = SafeDate(r, colMap, "CreateDate"),
                        GuildMoney = SafeLong(r, colMap, "GuildMoney")
                    });
                }
                return (list, $"{list.Count} guilds found");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        public async Task<List<GuildMemberInfo>> GetGuildMembersAsync(int guildUID)
        {
            var list = new List<GuildMemberInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT gm.*, c.CharacterName, c.[Level], c.UnitKind FROM td_GuildMember gm " +
                    "LEFT JOIN td_Character c ON gm.CharacterUniqueNumber = c.UniqueNumber " +
                    "WHERE gm.GuildUniqueNumber = @uid ORDER BY gm.Rank", conn);
                cmd.Parameters.AddWithValue("@uid", guildUID);
                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new GuildMemberInfo
                    {
                        CharacterUID = SafeInt(r, colMap, "CharacterUniqueNumber"),
                        CharacterName = SafeStr(r, colMap, "CharacterName") ?? "",
                        Rank = SafeInt(r, colMap, "Rank"),
                        Level = SafeInt(r, colMap, "Level"),
                        UnitKind = SafeInt(r, colMap, "UnitKind"),
                        JoinDate = SafeDate(r, colMap, "JoinDate")
                    });
                }
            }
            catch { }
            return list;
        }

        // ─── Log Management ────────────────────────────
        public async Task<(List<LogEntry> results, string info)> SearchLogsAsync(string table, string query, DateTime? from, DateTime? to)
        {
            var list = new List<LogEntry>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                // Try to find the log table
                var tables = new List<string>();
                using (var tcmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%' + @t + '%'", conn))
                {
                    tcmd.Parameters.AddWithValue("@t", table);
                    using var tr = await tcmd.ExecuteReaderAsync();
                    while (await tr.ReadAsync()) tables.Add(tr.GetString(0));
                }

                if (tables.Count == 0)
                {
                    // Try log database
                    try
                    {
                        var logConnStr = _config.GetGameConnectionString().Replace(_config.GameDbName, _config.GameDbName.Replace("_db_", "_db_Log_"));
                        using var logConn = new SqlConnection(logConnStr);
                        await logConn.OpenAsync();
                        using var tcmd2 = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%' + @t + '%'", logConn);
                        tcmd2.Parameters.AddWithValue("@t", table);
                        using var tr2 = await tcmd2.ExecuteReaderAsync();
                        while (await tr2.ReadAsync()) tables.Add(tr2.GetString(0));

                        if (tables.Count > 0)
                            return (list, $"Log tables found in log DB: {string.Join(", ", tables)}. Use log DB connection.");
                    }
                    catch { }
                    return (list, $"No tables matching '{table}' found");
                }

                string tbl = tables[0];
                string sql = $"SELECT TOP 500 * FROM {tbl} WHERE 1=1";
                if (!string.IsNullOrEmpty(query))
                    sql += " AND (CharacterName LIKE @q OR AccountName LIKE @q)";

                using var cmd = new SqlCommand(sql + " ORDER BY 1 DESC", conn);
                if (!string.IsNullOrEmpty(query))
                    cmd.Parameters.AddWithValue("@q", $"%{query}%");

                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    var entry = new LogEntry
                    {
                        AccountName = SafeStr(r, colMap, "AccountName") ?? "",
                        CharacterName = SafeStr(r, colMap, "CharacterName") ?? ""
                    };

                    // Try to get date from various column names
                    entry.LogDate = SafeDate(r, colMap, "LogDate") ?? SafeDate(r, colMap, "InsertDate") ?? SafeDate(r, colMap, "Date") ?? DateTime.MinValue;

                    // Build detail from all other columns
                    var details = new List<string>();
                    foreach (var kv in colMap)
                    {
                        if (kv.Key.Equals("AccountName", StringComparison.OrdinalIgnoreCase) ||
                            kv.Key.Equals("CharacterName", StringComparison.OrdinalIgnoreCase)) continue;
                        try
                        {
                            var val = r.GetValue(kv.Value);
                            if (val != DBNull.Value)
                                details.Add($"{kv.Key}={val}");
                        }
                        catch { }
                    }
                    entry.Detail = string.Join(" | ", details);
                    list.Add(entry);
                }
                return (list, $"{list.Count} logs from [{tbl}]. Available: {string.Join(", ", tables)}");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        public async Task<List<string>> GetLogTablesAsync()
        {
            var list = new List<string>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'tl_%' ORDER BY TABLE_NAME", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(r.GetString(0));
            }
            catch { }
            return list;
        }

        // ─── Admin Auto Notice ───────────────────────────
        public async Task<(NoticeSettings? settings, List<NoticeInfo> strings, string info)> LoadNoticeSystemAsync()
        {
            NoticeSettings? settings = null;
            var strings = new List<NoticeInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();

                // Load notice info (td_AdminAutoNoticeInfo)
                using (var cmd = new SqlCommand("SELECT * FROM td_AdminAutoNoticeInfo", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var colMap = BuildColumnMap(r);
                    if (await r.ReadAsync())
                    {
                        settings = new NoticeSettings
                        {
                            UsingFlag = SafeInt(r, colMap, "UsingFlag") != 0,
                            LoopSec = SafeInt(r, colMap, "LoopSec"),
                            IntervalSec = SafeInt(r, colMap, "IntervalSec"),
                            EditorAccountName = SafeStr(r, colMap, "EditorAccountName") ?? ""
                        };
                    }
                }

                // Load notice strings (td_AdminAutoNoticeString)
                using (var cmd2 = new SqlCommand("SELECT * FROM td_AdminAutoNoticeString ORDER BY NoticeStringIndex", conn))
                using (var r2 = await cmd2.ExecuteReaderAsync())
                {
                    var colMap2 = BuildColumnMap(r2);
                    while (await r2.ReadAsync())
                    {
                        strings.Add(new NoticeInfo
                        {
                            OrderIndex = SafeInt(r2, colMap2, "NoticeStringIndex"),
                            NoticeString = SafeStr(r2, colMap2, "NoticeString") ?? ""
                        });
                    }
                }

                return (settings, strings, $"{strings.Count} notices loaded. Flag={settings?.UsingFlag}, Loop={settings?.LoopSec}s, Interval={settings?.IntervalSec}s");
            }
            catch (Exception ex) { return (settings, strings, $"Error: {ex.Message}"); }
        }

        public async Task<(bool ok, string info)> SaveNoticeSystemAsync(NoticeSettings settings, List<NoticeInfo> strings, string adminName)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();

                using var tx = conn.BeginTransaction();
                try
                {
                    // Delete old info and insert new (same logic as atum_Update_AdminAutoNoticeInfo)
                    using (var cmd = new SqlCommand("DELETE FROM td_AdminAutoNoticeInfo", conn, tx))
                        await cmd.ExecuteNonQueryAsync();

                    using (var cmd = new SqlCommand(
                        "INSERT INTO td_AdminAutoNoticeInfo (UsingFlag, LoopSec, IntervalSec, EditorAccountName) VALUES (@flag, @loop, @interval, @editor)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@flag", settings.UsingFlag ? 1 : 0);
                        cmd.Parameters.AddWithValue("@loop", settings.LoopSec);
                        cmd.Parameters.AddWithValue("@interval", settings.IntervalSec);
                        cmd.Parameters.AddWithValue("@editor", adminName);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Delete old strings (same logic as atum_Insert_AdminAutoNoticeString with @i_bDeleteOldNoticeString=1)
                    using (var cmd = new SqlCommand("DELETE FROM td_AdminAutoNoticeString", conn, tx))
                        await cmd.ExecuteNonQueryAsync();

                    // Insert new strings
                    for (int i = 0; i < strings.Count; i++)
                    {
                        using var cmd2 = new SqlCommand(
                            "INSERT INTO td_AdminAutoNoticeString (NoticeStringIndex, NoticeString) VALUES (@idx, @str)", conn, tx);
                        cmd2.Parameters.AddWithValue("@idx", i);
                        cmd2.Parameters.AddWithValue("@str", strings[i].NoticeString);
                        await cmd2.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                    return (true, $"Saved {strings.Count} notices. Settings updated by {adminName}.");
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        // ─── Cash Shop ─────────────────────────────────
        public async Task<(List<CashShopItem> results, string info)> GetCashShopItemsAsync()
        {
            var list = new List<CashShopItem>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                // Find the cash shop table
                using var tcmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%CashItem%' OR TABLE_NAME LIKE '%CashShop%'", conn);
                var tables = new List<string>();
                using (var tr = await tcmd.ExecuteReaderAsync())
                    while (await tr.ReadAsync()) tables.Add(tr.GetString(0));

                if (tables.Count == 0) return (list, "No cash shop table found");

                string tbl = tables[0];
                using var cmd = new SqlCommand($"SELECT * FROM {tbl} ORDER BY 1", conn);
                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new CashShopItem
                    {
                        ItemNum = SafeInt(r, colMap, "ItemNum"),
                        ItemName = SafeStr(r, colMap, "ItemName") ?? $"Item#{SafeInt(r, colMap, "ItemNum")}",
                        Price = SafeInt(r, colMap, "Price") != 0 ? SafeInt(r, colMap, "Price") : SafeInt(r, colMap, "CashPrice"),
                        Category = SafeInt(r, colMap, "TabIndex") != 0 ? SafeInt(r, colMap, "TabIndex") : SafeInt(r, colMap, "Category"),
                        IsNew = SafeBool(r, colMap, "IsNew"),
                        IsRecommended = SafeBool(r, colMap, "IsRecommend"),
                        LimitedCount = SafeInt(r, colMap, "LimitedEditionCount"),
                        SoldCount = SafeInt(r, colMap, "SoldCount")
                    });
                }
                return (list, $"{list.Count} items from [{tbl}]");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        // ─── Happy Hour Events ──────────────────────────
        public async Task<(List<HappyHourEventInfo> results, string info)> GetHappyHourEventsAsync()
        {
            var list = new List<HappyHourEventInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                using var tcmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%HappyHour%' OR TABLE_NAME LIKE '%Event%'", conn);
                var tables = new List<string>();
                using (var tr = await tcmd.ExecuteReaderAsync())
                    while (await tr.ReadAsync()) tables.Add(tr.GetString(0));

                if (tables.Count == 0) return (list, "No event tables found");

                string tbl = tables.FirstOrDefault(t => t.Contains("HappyHour")) ?? tables[0];
                using var cmd = new SqlCommand($"SELECT TOP 100 * FROM {tbl} ORDER BY 1 DESC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                var colMap = BuildColumnMap(r);
                while (await r.ReadAsync())
                {
                    list.Add(new HappyHourEventInfo
                    {
                        EventUID = SafeInt(r, colMap, "UniqueNumber") != 0 ? SafeInt(r, colMap, "UniqueNumber") : SafeInt(r, colMap, "EventUID"),
                        StartDate = SafeDate(r, colMap, "StartDate") ?? SafeDate(r, colMap, "StartTime"),
                        EndDate = SafeDate(r, colMap, "EndDate") ?? SafeDate(r, colMap, "EndTime"),
                        BonusType = SafeInt(r, colMap, "BonusType") != 0 ? SafeInt(r, colMap, "BonusType") : SafeInt(r, colMap, "HappyHourEventType"),
                        BonusValue = SafeInt(r, colMap, "BonusValue") != 0 ? SafeInt(r, colMap, "BonusValue") : SafeInt(r, colMap, "BonusRate"),
                        IsActive = SafeBool(r, colMap, "IsActive")
                    });
                }
                return (list, $"{list.Count} events from [{tbl}]. Tables: {string.Join(", ", tables)}");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        // ─── Generic Table Query ────────────────────────
        public async Task<DataTable> QueryTableAsync(string connString, string sql)
        {
            var dt = new DataTable();
            try
            {
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { dt.TableName = $"Error: {ex.Message}"; }
            return dt;
        }

        public async Task<int> ExecuteNonQueryAsync(string connString, string sql)
        {
            using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        // ─── Helpers ────────────────────────────────────
        private static Dictionary<string, int> BuildColumnMap(SqlDataReader r)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < r.FieldCount; i++) map[r.GetName(i)] = i;
            return map;
        }

        private async Task<int> SafeCountAsync(SqlConnection conn, string sql)
        {
            try { using var cmd = new SqlCommand(sql, conn); return Convert.ToInt32(await cmd.ExecuteScalarAsync()); }
            catch { return 0; }
        }

        private async Task<long> SafeLongAsync(SqlConnection conn, string sql)
        {
            try { using var cmd = new SqlCommand(sql, conn); return Convert.ToInt64(await cmd.ExecuteScalarAsync()); }
            catch { return 0; }
        }

        private static string? SafeStr(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return null; return r.IsDBNull(i) ? null : r.GetValue(i).ToString(); }

        private static int SafeInt(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return 0; return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i)); }

        private static long SafeLong(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return 0; return r.IsDBNull(i) ? 0 : Convert.ToInt64(r.GetValue(i)); }

        private static float SafeFloat(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return 0; return r.IsDBNull(i) ? 0 : Convert.ToSingle(r.GetValue(i)); }

        private static bool SafeBool(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return false; return r.IsDBNull(i) ? false : Convert.ToBoolean(r.GetValue(i)); }

        private static DateTime? SafeDate(SqlDataReader r, Dictionary<string, int> m, string col)
        { if (!m.TryGetValue(col, out int i)) return null; return r.IsDBNull(i) ? null : Convert.ToDateTime(r.GetValue(i)); }

        // ─── StrategyPoint ──────────────────────────────
        public async Task<(List<StrategyPointSchedule> schedule, List<StrategyPointMapInfo> maps, string info)> LoadStrategyPointDataAsync()
        {
            var schedule = new List<StrategyPointSchedule>();
            var maps = new List<StrategyPointMapInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();

                // Load weekly schedule from td_RenewalStrategyPointSummonTime
                using (var cmd = new SqlCommand("SELECT DayOfWeek, StartTime, EndTime, CountBCU, CountANI FROM td_RenewalStrategyPointSummonTime ORDER BY DayOfWeek", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        schedule.Add(new StrategyPointSchedule
                        {
                            DayOfWeek = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0)),
                            StartTime = r.IsDBNull(1) ? DateTime.MinValue : Convert.ToDateTime(r.GetValue(1)),
                            EndTime = r.IsDBNull(2) ? DateTime.MinValue : Convert.ToDateTime(r.GetValue(2)),
                            CountBCU = r.IsDBNull(3) ? 15 : Convert.ToInt32(r.GetValue(3)),
                            CountANI = r.IsDBNull(4) ? 15 : Convert.ToInt32(r.GetValue(4))
                        });
                    }
                }

                // If no schedule data, create defaults for all 7 days
                if (schedule.Count == 0)
                {
                    for (int d = 0; d < 7; d++)
                        schedule.Add(new StrategyPointSchedule
                        {
                            DayOfWeek = d,
                            StartTime = DateTime.Parse("00:01:00"),
                            EndTime = DateTime.Parse("23:59:00"),
                            CountBCU = 15,
                            CountANI = 15
                        });
                }

                // Load map list from account DB with influence type from ti_MapInfo
                // Old admin tool uses: SELECT MapIndex, MapName, MapInfluenceType FROM ti_MapInfo
                //   WHERE MapIndex IN (SELECT MapIndex FROM ti_StrategyPointSummonMapIndex)
                // BCU = MapInfluenceType 1000-1999, ANI = 2000-2999
                var mapList = new Dictionary<int, (string name, int spNum, int influenceType)>();
                using (var cmd2 = new SqlCommand(
                    "SELECT sp.MapName, sp.MapIndex, sp.StratrgyPiontNum, ISNULL(mi.MapInfluenceType, 0) " +
                    "FROM ti_StrategyPointSummonMapIndex sp " +
                    "LEFT JOIN ti_MapInfo mi ON sp.MapIndex = mi.MapIndex " +
                    "ORDER BY sp.MapIndex", conn))
                using (var r2 = await cmd2.ExecuteReaderAsync())
                {
                    while (await r2.ReadAsync())
                        mapList[r2.GetInt32(1)] = (r2.GetString(0), Convert.ToInt32(r2.GetValue(2)), Convert.ToInt32(r2.GetValue(3)));
                }

                // Load summon info from GAME DB (td_StrategyPointSummonInfo is there)
                var summonInfo = new Dictionary<int, (DateTime time, int attr, int count)>();
                try
                {
                    using var conn2 = new SqlConnection(_config.GetGameConnectionString());
                    await conn2.OpenAsync();
                    using var cmd3 = new SqlCommand("SELECT MapIndex, SummonCount, SummonTime, SummonAttribute FROM td_StrategyPointSummonInfo", conn2);
                    using var r3 = await cmd3.ExecuteReaderAsync();
                    while (await r3.ReadAsync())
                    {
                        var mi = r3.GetInt32(0);
                        var sc = Convert.ToInt32(r3.GetValue(1));
                        var st = r3.IsDBNull(2) ? DateTime.MinValue : r3.GetDateTime(2);
                        var sa = r3.IsDBNull(3) ? 0 : Convert.ToInt32(r3.GetValue(3));
                        summonInfo[mi] = (st, sa, sc);
                    }
                }
                catch { /* Game DB may not be accessible */ }

                // Merge both datasets
                foreach (var (mapIndex, (mapName, spNum, influenceType)) in mapList)
                {
                    string influence = DetermineInfluence(mapIndex, mapName, influenceType);
                    DateTime? summonTime = null;
                    string creation = "Waiting";

                    if (summonInfo.TryGetValue(mapIndex, out var si))
                    {
                        summonTime = si.time.Year <= 1900 ? null : si.time;
                        // SummonCount > 0 means summon is pending, regardless of time
                        if (si.count > 0 && si.attr > 0)
                            creation = si.time > DateTime.Now ? "Waiting" : "Summoning...";
                        else
                            creation = si.time.Year <= 1900 ? "Finish" :
                                      si.time > DateTime.Now ? "Waiting" : "Finish";
                    }

                    maps.Add(new StrategyPointMapInfo
                    {
                        MapIndex = mapIndex,
                        MapName = mapName,
                        Influence = influence,
                        SummonTime = summonTime,
                        Creation = creation
                    });
                }

                // Sort by SummonTime
                maps.Sort((a, b) => (a.SummonTime ?? DateTime.MinValue).CompareTo(b.SummonTime ?? DateTime.MinValue));

                return (schedule, maps, $"Loaded {schedule.Count} days, {maps.Count} maps");
            }
            catch (Exception ex) { return (schedule, maps, $"Error: {ex.Message}"); }
        }

        public async Task<(bool ok, string info)> SaveStrategyPointScheduleAsync(List<StrategyPointSchedule> schedule)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();

                using var tx = conn.BeginTransaction();
                try
                {
                    // Delete old schedule
                    using (var cmd = new SqlCommand("DELETE FROM td_RenewalStrategyPointSummonTime", conn, tx))
                        await cmd.ExecuteNonQueryAsync();

                    // Insert new schedule
                    foreach (var day in schedule)
                    {
                        using var cmd = new SqlCommand(
                            "INSERT INTO td_RenewalStrategyPointSummonTime (DayOfWeek, StartTime, EndTime, CountBCU, CountANI) " +
                            "VALUES (@dow, @start, @end, @bcu, @ani)", conn, tx);
                        cmd.Parameters.AddWithValue("@dow", (byte)day.DayOfWeek);
                        cmd.Parameters.AddWithValue("@start", day.StartTime);
                        cmd.Parameters.AddWithValue("@end", day.EndTime);
                        cmd.Parameters.AddWithValue("@bcu", (byte)day.CountBCU);
                        cmd.Parameters.AddWithValue("@ani", (byte)day.CountANI);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                    return (true, $"Saved schedule for {schedule.Count} days.");
                }
                catch { tx.Rollback(); throw; }
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Force immediate StrategyPoint summon on a map.
        ///
        /// The server's OnDoMinutelyWarkInflWarManager() timer checks TWO things:
        /// 1. Schedule window: CurrentTime >= StartTime && CurrentTime less than EndTime (from td_RenewalStrategyPointSummonTime)
        /// 2. Map entry: SummonAttribute=TRUE && SummonCount>0 && CurrentTime >= SummonTime
        ///
        /// So we must update BOTH:
        /// - td_RenewalStrategyPointSummonTime: set today's window to include NOW
        /// - td_StrategyPointSummonInfo: set SummonCount=1, SummonAttribute=1, SummonTime=past
        ///
        /// Then send ONLY T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE (data reload) — NOT the
        /// T_PA_ADMIN_UPDATE_STRATEGYPOINT_NOTSUMMONTIME which triggers recalculation.
        /// </summary>
        public async Task<(bool ok, string info)> ForceStrategyPointSummonAsync(int mapIndex, string mapName)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                var now = DateTime.Now;
                int todayDow = (int)now.DayOfWeek; // 0=Sunday — matches C++ tm_wday

                using var tx = conn.BeginTransaction();
                try
                {
                    // ── Step 1: Update schedule window for today in GAME DB ──
                    // Server timer checks: CurrentTime >= StartTime && CurrentTime < EndTime
                    // Set a wide window: NOW-5min to NOW+60min
                    var windowStart = now.AddMinutes(-5);
                    var windowEnd = now.AddMinutes(60);

                    int schedExists;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM td_RenewalStrategyPointSummonTime WHERE DayOfWeek = @dow", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@dow", (byte)todayDow);
                        schedExists = (int)await cmd.ExecuteScalarAsync()!;
                    }

                    if (schedExists > 0)
                    {
                        using var cmd = new SqlCommand(
                            "UPDATE td_RenewalStrategyPointSummonTime SET StartTime = @start, EndTime = @end WHERE DayOfWeek = @dow", conn, tx);
                        cmd.Parameters.AddWithValue("@dow", (byte)todayDow);
                        cmd.Parameters.AddWithValue("@start", windowStart);
                        cmd.Parameters.AddWithValue("@end", windowEnd);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using var cmd = new SqlCommand(
                            "INSERT INTO td_RenewalStrategyPointSummonTime (DayOfWeek, StartTime, EndTime, CountBCU, CountANI) VALUES (@dow, @start, @end, 15, 15)", conn, tx);
                        cmd.Parameters.AddWithValue("@dow", (byte)todayDow);
                        cmd.Parameters.AddWithValue("@start", windowStart);
                        cmd.Parameters.AddWithValue("@end", windowEnd);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ── Step 2: Set target map SummonCount=1, SummonAttribute=1, SummonTime=past ──
                    int mapExists;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM td_StrategyPointSummonInfo WHERE MapIndex = @mi", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@mi", mapIndex);
                        mapExists = (int)await cmd.ExecuteScalarAsync()!;
                    }

                    // SummonTime 2 minutes in past → server timer sees CurrentTime >= SummonTime immediately
                    string timeSql = "DATEADD(MINUTE, -2, GETDATE())";

                    if (mapExists > 0)
                    {
                        using var cmd = new SqlCommand(
                            $"UPDATE td_StrategyPointSummonInfo SET SummonTime = {timeSql}, SummonCount = 1, SummonAttribute = 1 WHERE MapIndex = @mi", conn, tx);
                        cmd.Parameters.AddWithValue("@mi", mapIndex);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using var cmd = new SqlCommand(
                            $"INSERT INTO td_StrategyPointSummonInfo (MapIndex, SummonCount, SummonTime, SummonAttribute) VALUES (@mi, 1, {timeSql}, 1)", conn, tx);
                        cmd.Parameters.AddWithValue("@mi", mapIndex);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                    return (true, $"Summon prepared for {mapName} (MapIndex: {mapIndex}).");
                }
                catch { tx.Rollback(); throw; }
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Cancel a pending StrategyPoint summon on a map.
        /// </summary>
        public async Task<(bool ok, string info)> CancelStrategyPointSummonAsync(int mapIndex)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE td_StrategyPointSummonInfo SET SummonCount = 0, SummonAttribute = 0 WHERE MapIndex = @mi", conn);
                cmd.Parameters.AddWithValue("@mi", mapIndex);
                int affected = await cmd.ExecuteNonQueryAsync();
                return (affected > 0, affected > 0 ? "Summon cancelled." : "No record found.");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Determine influence using MapInfluenceType from ti_MapInfo.
        /// BCU (VCN) = MapInfluenceType 1000-1999, ANI = 2000-2999.
        /// Falls back to name/index heuristic if MapInfluenceType not available.
        /// </summary>
        private static string DetermineInfluence(int mapIndex, string mapName, int mapInfluenceType = 0)
        {
            // Use actual MapInfluenceType from ti_MapInfo (matches IS_MAP_INFLUENCE_VCN/ANI macros)
            if (mapInfluenceType >= 1000 && mapInfluenceType <= 1999) return "BCU";
            if (mapInfluenceType >= 2000 && mapInfluenceType <= 2999) return "ANI";

            // Fallback: name-based detection
            if (mapName.Contains("BCU", StringComparison.OrdinalIgnoreCase)) return "BCU";
            if (mapName.Contains("ANI", StringComparison.OrdinalIgnoreCase)) return "ANI";
            return (mapIndex % 2 == 0) ? "BCU" : "ANI";
        }

        /// <summary>
        /// Load map list with influence types for Today SP Setting generation.
        /// Returns list of (MapIndex, MapName, Influence) from account DB.
        /// </summary>
        public async Task<(List<(int MapIndex, string MapName, string Influence)> maps, string info)> LoadStrategyPointMapsWithInfluenceAsync()
        {
            var result = new List<(int, string, string)>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT sp.MapIndex, sp.MapName, ISNULL(mi.MapInfluenceType, 0) " +
                    "FROM ti_StrategyPointSummonMapIndex sp " +
                    "LEFT JOIN ti_MapInfo mi ON sp.MapIndex = mi.MapIndex " +
                    "ORDER BY sp.MapIndex", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var mapIdx = r.GetInt32(0);
                    var mapName = r.GetString(1);
                    var inflType = Convert.ToInt32(r.GetValue(2));
                    result.Add((mapIdx, mapName, DetermineInfluence(mapIdx, mapName, inflType)));
                }
                return (result, $"Loaded {result.Count} maps");
            }
            catch (Exception ex) { return (result, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Save generated Today StrategyPoint plan to Game DB (td_StrategyPointSummonInfo).
        /// Also updates today's schedule window in Game DB td_RenewalStrategyPointSummonTime.
        /// This mirrors the old admin tool's OnButtonSave (DBUpdateStrategyPointSummonInfo + DBUpdateWeekdayStrategyPointSummonTime).
        /// </summary>
        public async Task<(bool ok, string info)> SaveTodayStrategyPointPlanAsync(
            List<StrategyPointSchedule> schedule,
            List<StrategyPointMapInfo> plan)
        {
            try
            {
                // Step 1: Update schedule in Game DB (server reads from here)
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();
                try
                {
                    // Update schedule for all days (mirrors DBUpdateWeekdayStrategyPointSummonTime)
                    foreach (var day in schedule)
                    {
                        int exists;
                        using (var cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM td_RenewalStrategyPointSummonTime WHERE DayOfWeek = @dow", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@dow", (byte)day.DayOfWeek);
                            exists = (int)await cmd.ExecuteScalarAsync()!;
                        }

                        if (exists > 0)
                        {
                            using var cmd = new SqlCommand(
                                "UPDATE td_RenewalStrategyPointSummonTime SET StartTime = @start, EndTime = @end, " +
                                "CountBCU = @bcu, CountANI = @ani WHERE DayOfWeek = @dow", conn, tx);
                            cmd.Parameters.AddWithValue("@dow", (byte)day.DayOfWeek);
                            cmd.Parameters.AddWithValue("@start", day.StartTime);
                            cmd.Parameters.AddWithValue("@end", day.EndTime);
                            cmd.Parameters.AddWithValue("@bcu", (byte)day.CountBCU);
                            cmd.Parameters.AddWithValue("@ani", (byte)day.CountANI);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            using var cmd = new SqlCommand(
                                "INSERT INTO td_RenewalStrategyPointSummonTime (DayOfWeek, StartTime, EndTime, CountBCU, CountANI) " +
                                "VALUES (@dow, @start, @end, @bcu, @ani)", conn, tx);
                            cmd.Parameters.AddWithValue("@dow", (byte)day.DayOfWeek);
                            cmd.Parameters.AddWithValue("@start", day.StartTime);
                            cmd.Parameters.AddWithValue("@end", day.EndTime);
                            cmd.Parameters.AddWithValue("@bcu", (byte)day.CountBCU);
                            cmd.Parameters.AddWithValue("@ani", (byte)day.CountANI);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Step 2: Update summon info for each map (mirrors DBUpdateStrategyPointSummonInfo)
                    foreach (var map in plan)
                    {
                        int summonCount = (map.Creation == "Scheduled") ? 1 : 0;
                        int summonAttr = summonCount;
                        string summonTimeStr = map.SummonTime.HasValue
                            ? map.SummonTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")
                            : "1900-01-01 00:00:00.000";

                        int exists;
                        using (var cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM td_StrategyPointSummonInfo WHERE MapIndex = @mi", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@mi", map.MapIndex);
                            exists = (int)await cmd.ExecuteScalarAsync()!;
                        }

                        if (exists > 0)
                        {
                            using var cmd = new SqlCommand(
                                "UPDATE td_StrategyPointSummonInfo SET SummonCount = @sc, SummonTime = @st, " +
                                "SummonAttribute = @sa WHERE MapIndex = @mi", conn, tx);
                            cmd.Parameters.AddWithValue("@mi", map.MapIndex);
                            cmd.Parameters.AddWithValue("@sc", (byte)summonCount);
                            cmd.Parameters.AddWithValue("@st", DateTime.Parse(summonTimeStr));
                            cmd.Parameters.AddWithValue("@sa", (byte)summonAttr);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            using var cmd = new SqlCommand(
                                "INSERT INTO td_StrategyPointSummonInfo (MapIndex, SummonCount, SummonTime, SummonAttribute) " +
                                "VALUES (@mi, @sc, @st, @sa)", conn, tx);
                            cmd.Parameters.AddWithValue("@mi", map.MapIndex);
                            cmd.Parameters.AddWithValue("@sc", (byte)summonCount);
                            cmd.Parameters.AddWithValue("@st", DateTime.Parse(summonTimeStr));
                            cmd.Parameters.AddWithValue("@sa", (byte)summonAttr);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    tx.Commit();
                    return (true, $"Saved plan: {plan.Count(p => p.Creation == "Scheduled")} maps scheduled.");
                }
                catch { tx.Rollback(); throw; }
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════
        // LOOKUP: MONSTER NAMES & MAP NAMES
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Load monster lookup (MonsterUnitKind → MonsterName) from ti_Monster (Account DB).
        /// </summary>
        public async Task<Dictionary<int, string>> LoadMonsterNamesAsync()
        {
            var result = new Dictionary<int, string>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT UniqueNumber, MonsterName FROM ti_Monster WITH (NOLOCK) ORDER BY UniqueNumber", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = Convert.ToInt32(r.GetValue(0));
                    var name = r.IsDBNull(1) ? $"Monster_{id}" : r.GetString(1).Trim();
                    result[id] = name;
                }
            }
            catch { /* lookup is optional */ }
            return result;
        }

        /// <summary>
        /// Load map lookup (MapIndex → MapName) from ti_MapInfo (Account DB).
        /// </summary>
        public async Task<Dictionary<int, string>> LoadMapNamesAsync()
        {
            var result = new Dictionary<int, string>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT MapIndex, MapName FROM ti_MapInfo WITH (NOLOCK) ORDER BY MapIndex", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var idx = Convert.ToInt32(r.GetValue(0));
                    var name = r.IsDBNull(1) ? $"Map_{idx}" : r.GetString(1).Trim();
                    result[idx] = name;
                }
            }
            catch { /* silently fail - lookup is optional */ }
            return result;
        }

        // ═══════════════════════════════════════════════
        // EVENT MONSTER MANAGEMENT
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Load all event monsters from ti_EventMonster (Account DB).
        /// Mirrors CAtumDBHelper::LoadEventMonster using PROCEDURE_080827_0073.
        /// </summary>
        public async Task<(List<EventMonster> list, string info)> LoadEventMonstersAsync()
        {
            var result = new List<EventMonster>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT EventMonsterUID, ServerGroupID, StartDateTime, EndDateTime, " +
                    "SummonerMapIndex, SummonerReqMinLevel, SummonerReqMaxLevel, SummonerExceptMonster, " +
                    "SummonMonsterNum, SummonMonsterCount, SummonDelayTime, SummonProbability " +
                    "FROM ti_EventMonster WITH (NOLOCK) ORDER BY EventMonsterUID DESC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.Add(new EventMonster
                    {
                        EventMonsterUID = r.GetInt32(0),
                        ServerGroupID = Convert.ToInt32(r.GetValue(1)),
                        StartDateTime = r.GetDateTime(2),
                        EndDateTime = r.GetDateTime(3),
                        SummonerMapIndex = Convert.ToInt32(r.GetValue(4)),
                        SummonerReqMinLevel = Convert.ToInt32(r.GetValue(5)),
                        SummonerReqMaxLevel = Convert.ToInt32(r.GetValue(6)),
                        SummonerExceptMonster = Convert.ToInt32(r.GetValue(7)),
                        SummonMonsterNum = Convert.ToInt32(r.GetValue(8)),
                        SummonMonsterCount = Convert.ToInt32(r.GetValue(9)),
                        SummonDelayTime = Convert.ToInt32(r.GetValue(10)),
                        SummonProbability = Convert.ToInt32(r.GetValue(11))
                    });
                }
                return (result, $"Loaded {result.Count} event monsters");
            }
            catch (Exception ex) { return (result, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Insert a new event monster into ti_EventMonster.
        /// Mirrors PROCEDURE_080827_0051.
        /// </summary>
        public async Task<(bool ok, string info)> InsertEventMonsterAsync(EventMonster em)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "INSERT INTO ti_EventMonster (ServerGroupID, StartDateTime, EndDateTime, " +
                    "SummonerMapIndex, SummonerReqMinLevel, SummonerReqMaxLevel, SummonerExceptMonster, " +
                    "SummonMonsterNum, SummonMonsterCount, SummonDelayTime, SummonProbability) " +
                    "VALUES (@sgid, @start, @end, @mapIdx, @minLv, @maxLv, @except, @monNum, @monCnt, @delay, @prob)", conn);
                cmd.Parameters.AddWithValue("@sgid", em.ServerGroupID);
                cmd.Parameters.AddWithValue("@start", em.StartDateTime);
                cmd.Parameters.AddWithValue("@end", em.EndDateTime);
                cmd.Parameters.AddWithValue("@mapIdx", (short)em.SummonerMapIndex);
                cmd.Parameters.AddWithValue("@minLv", (byte)em.SummonerReqMinLevel);
                cmd.Parameters.AddWithValue("@maxLv", (byte)em.SummonerReqMaxLevel);
                cmd.Parameters.AddWithValue("@except", em.SummonerExceptMonster);
                cmd.Parameters.AddWithValue("@monNum", em.SummonMonsterNum);
                cmd.Parameters.AddWithValue("@monCnt", em.SummonMonsterCount);
                cmd.Parameters.AddWithValue("@delay", em.SummonDelayTime);
                cmd.Parameters.AddWithValue("@prob", em.SummonProbability);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Event monster created successfully.");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Update an existing event monster in ti_EventMonster.
        /// Mirrors PROCEDURE_080827_0052.
        /// </summary>
        public async Task<(bool ok, string info)> UpdateEventMonsterAsync(EventMonster em)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "UPDATE ti_EventMonster SET ServerGroupID=@sgid, StartDateTime=@start, EndDateTime=@end, " +
                    "SummonerMapIndex=@mapIdx, SummonerReqMinLevel=@minLv, SummonerReqMaxLevel=@maxLv, " +
                    "SummonerExceptMonster=@except, SummonMonsterNum=@monNum, SummonMonsterCount=@monCnt, " +
                    "SummonDelayTime=@delay, SummonProbability=@prob " +
                    "WHERE EventMonsterUID=@uid", conn);
                cmd.Parameters.AddWithValue("@sgid", em.ServerGroupID);
                cmd.Parameters.AddWithValue("@start", em.StartDateTime);
                cmd.Parameters.AddWithValue("@end", em.EndDateTime);
                cmd.Parameters.AddWithValue("@mapIdx", (short)em.SummonerMapIndex);
                cmd.Parameters.AddWithValue("@minLv", (byte)em.SummonerReqMinLevel);
                cmd.Parameters.AddWithValue("@maxLv", (byte)em.SummonerReqMaxLevel);
                cmd.Parameters.AddWithValue("@except", em.SummonerExceptMonster);
                cmd.Parameters.AddWithValue("@monNum", em.SummonMonsterNum);
                cmd.Parameters.AddWithValue("@monCnt", em.SummonMonsterCount);
                cmd.Parameters.AddWithValue("@delay", em.SummonDelayTime);
                cmd.Parameters.AddWithValue("@prob", em.SummonProbability);
                cmd.Parameters.AddWithValue("@uid", em.EventMonsterUID);
                int affected = await cmd.ExecuteNonQueryAsync();
                return (affected > 0, affected > 0 ? "Event monster updated." : "No record found.");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Delete an event monster from ti_EventMonster.
        /// Mirrors PROCEDURE_080827_0053.
        /// </summary>
        public async Task<(bool ok, string info)> DeleteEventMonsterAsync(int eventMonsterUID)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM ti_EventMonster WHERE EventMonsterUID=@uid", conn);
                cmd.Parameters.AddWithValue("@uid", eventMonsterUID);
                int affected = await cmd.ExecuteNonQueryAsync();
                return (affected > 0, affected > 0 ? "Event monster deleted." : "No record found.");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }
    }

    public class DashboardStats
    {
        public int TotalAccounts { get; set; }
        public int TotalCharacters { get; set; }
        public int TotalGuilds { get; set; }
        public int BannedAccounts { get; set; }
        public int TodayRegistrations { get; set; }
        public long TotalMoney { get; set; }
        public string? Error { get; set; }
    }

    // ─── Declaration of War (Mothership War) DB Methods ─────
    public partial class DatabaseService
    {
        /// <summary>Load all Declaration of War rows from td_DeclarationOfWar (Game DB).</summary>
        public async Task<(List<DeclarationOfWarInfo> list, string info)> LoadDeclarationOfWarAsync()
        {
            var list = new List<DeclarationOfWarInfo>();
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_Load_DeclarationOfWarInfo", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new DeclarationOfWarInfo
                    {
                        Influence = Convert.ToInt32(r["Influence"]),
                        MSWarStep = Convert.ToInt32(r["MSWarStep"]),
                        NCP = r["NCP"] == DBNull.Value ? 0 : Convert.ToInt32(r["NCP"]),
                        MSNum = r["MSNum"] == DBNull.Value ? 0 : Convert.ToInt32(r["MSNum"]),
                        MSAppearanceMap = r["MSAppearanceMap"] == DBNull.Value ? 0 : Convert.ToInt32(r["MSAppearanceMap"]),
                        MSWarStepStartTime = r["MSWarStepStartTime"] == DBNull.Value ? null : Convert.ToDateTime(r["MSWarStepStartTime"]),
                        MSWarStepEndTime = r["MSWarStepEndTime"] == DBNull.Value ? null : Convert.ToDateTime(r["MSWarStepEndTime"]),
                        MSWarStartTime = r["MSWarStartTime"] == DBNull.Value ? null : Convert.ToDateTime(r["MSWarStartTime"]),
                        MSWarEndTime = r["MSWarEndTime"] == DBNull.Value ? null : Convert.ToDateTime(r["MSWarEndTime"]),
                        SelectCount = r["SelectCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["SelectCount"]),
                        GiveUp = r["GiveUp"] != DBNull.Value && Convert.ToBoolean(r["GiveUp"]),
                        MSWarEndState = r["MSWarEndState"] == DBNull.Value ? 0 : Convert.ToInt32(r["MSWarEndState"])
                    });
                }
                return (list, $"Loaded {list.Count} war entries.");
            }
            catch (Exception ex) { return (list, $"Error: {ex.Message}"); }
        }

        /// <summary>Load forbid time from td_DeclarationOfWarForbidTime (Account DB).</summary>
        public async Task<(DeclarationOfWarForbidTime? data, string info)> LoadDeclarationOfWarForbidTimeAsync()
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_Load_DeclarationOfWarForbidTimeInfo", conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    var ft = new DeclarationOfWarForbidTime
                    {
                        DayOfWeek = Convert.ToInt32(r["DayOfWeek"]),
                        ForbidStartTime = r["ForbidStartTime"] == DBNull.Value ? null : Convert.ToDateTime(r["ForbidStartTime"]),
                        ForbidEndTime = r["ForbidEndTime"] == DBNull.Value ? null : Convert.ToDateTime(r["ForbidEndTime"])
                    };
                    return (ft, "Forbid time loaded.");
                }
                return (null, "No forbid time data found.");
            }
            catch (Exception ex) { return (null, $"Error: {ex.Message}"); }
        }

        /// <summary>Cascade-update step start times (calls atum_UpdateStepDeclarationOfWarByAdminTool).</summary>
        public async Task<(bool ok, string info)> UpdateWarStepStartAsync(int step, DateTime startTime)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_UpdateStepDeclarationOfWarByAdminTool @Step, @StartTime", conn);
                cmd.Parameters.AddWithValue("@Step", step);
                cmd.Parameters.AddWithValue("@StartTime", startTime);
                await cmd.ExecuteNonQueryAsync();
                return (true, $"Step {step} start updated. Syncing to server...");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>Update remaining select count for a specific influence+step.</summary>
        public async Task<(bool ok, string info)> UpdateWarSelectCountAsync(int influence, int step, int selectCount)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_UpdateSelectCountDeclarationOfWarByAdminTool @Infl, @Step, @Select", conn);
                cmd.Parameters.AddWithValue("@Infl", influence);
                cmd.Parameters.AddWithValue("@Step", step);
                cmd.Parameters.AddWithValue("@Select", selectCount);
                await cmd.ExecuteNonQueryAsync();
                return (true, $"Select count updated to {selectCount}.");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>Update forbid time (Account DB).</summary>
        public async Task<(bool ok, string info)> UpdateWarForbidTimeAsync(int dayOfWeek, DateTime startTime, DateTime endTime)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_UpdateForbidTimeDeclarationOfWarByAdminTool @Day, @Start, @End", conn);
                cmd.Parameters.AddWithValue("@Day", dayOfWeek);
                cmd.Parameters.AddWithValue("@Start", startTime);
                cmd.Parameters.AddWithValue("@End", endTime);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Forbid time updated. Syncing to server...");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>Manually set MS War start time for a specific influence+step.</summary>
        public async Task<(bool ok, string info)> UpdateMSWarStartTimeAsync(int influence, int step, DateTime warStartTime)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_UpdateMSWarStartDeclarationOfWarByAdminTool @Infl, @Step, @StartTime", conn);
                cmd.Parameters.AddWithValue("@Infl", influence);
                cmd.Parameters.AddWithValue("@Step", step);
                cmd.Parameters.AddWithValue("@StartTime", warStartTime);
                await cmd.ExecuteNonQueryAsync();
                return (true, $"MS War start time set. Syncing to server...");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>Full reset of Declaration of War data.</summary>
        public async Task<(bool ok, string info)> ResetDeclarationOfWarAsync()
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("EXEC dbo.atum_ResetDeclarationOfWarByAdminTool", conn);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Declaration of War reset complete. Syncing to server...");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        /// <summary>Start an instant war by expiring prior steps, resetting the target step, and setting MSWarStartTime to now+2min.</summary>
        public async Task<(bool ok, string info)> StartInstantWarAsync(int influence, int step)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetGameConnectionString());
                await conn.OpenAsync();

                var now = DateTime.Now;
                var pastEnd = now.AddMinutes(-1);          // Expired (in the past)
                var stepStart = now.AddMinutes(-5);        // Step already active (in the past)
                var stepEnd = now.AddDays(7);              // Step valid for 7 days
                var warTime = now.AddMinutes(2);           // War starts in 2 min

                // Step 1: Expire ALL steps BEFORE the target step
                // FieldServer's FindMSWarStepByCurrentTime finds the FIRST step where
                // MSWarStepEndTime > now. If earlier steps still have future end times,
                // the target step will never be reached!
                if (step > 1)
                {
                    using var expireCmd = new SqlCommand(@"
                        UPDATE td_DeclarationOfWar
                        SET MSWarStepEndTime = @PastEnd
                        WHERE MSWarStep < @Step AND MSWarStep != 99", conn);
                    expireCmd.Parameters.Add("@PastEnd", System.Data.SqlDbType.DateTime).Value = pastEnd;
                    expireCmd.Parameters.Add("@Step", System.Data.SqlDbType.TinyInt).Value = (byte)step;
                    int expiredRows = await expireCmd.ExecuteNonQueryAsync();
                }

                // Step 2: Full reset of the target step (both BCU and ANI rows)
                using var cmd = new SqlCommand(@"
                    UPDATE td_DeclarationOfWar
                    SET MSWarStepStartTime = @StepStart,
                        MSWarStepEndTime = @StepEnd,
                        MSWarStartTime = @WarTime,
                        MSWarEndTime = NULL,
                        MSWarEndState = 0,
                        GiveUp = 0,
                        SelectCount = 0,
                        MSNum = 0,
                        NCP = 0,
                        MSAppearanceMap = 0
                    WHERE MSWarStep = @Step", conn);
                cmd.Parameters.Add("@StepStart", System.Data.SqlDbType.DateTime).Value = stepStart;
                cmd.Parameters.Add("@StepEnd", System.Data.SqlDbType.DateTime).Value = stepEnd;
                cmd.Parameters.Add("@WarTime", System.Data.SqlDbType.DateTime).Value = warTime;
                cmd.Parameters.Add("@Step", System.Data.SqlDbType.TinyInt).Value = (byte)step;
                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return (false, $"No rows updated! Step={step} not found in td_DeclarationOfWar.");

                // Verify the update actually applied
                using var verify = new SqlCommand(
                    "SELECT MSWarStepStartTime, MSWarStartTime, MSNum, MSWarEndState FROM td_DeclarationOfWar WHERE Influence = @Infl AND MSWarStep = @Step2", conn);
                verify.Parameters.Add("@Infl", System.Data.SqlDbType.TinyInt).Value = (byte)influence;
                verify.Parameters.Add("@Step2", System.Data.SqlDbType.TinyInt).Value = (byte)step;
                using var r = await verify.ExecuteReaderAsync();
                string dbCheck = "";
                if (await r.ReadAsync())
                {
                    var dbStepStart = r["MSWarStepStartTime"] == DBNull.Value ? "NULL" : Convert.ToDateTime(r["MSWarStepStartTime"]).ToString("HH:mm:ss");
                    var dbWarStart = r["MSWarStartTime"] == DBNull.Value ? "NULL" : Convert.ToDateTime(r["MSWarStartTime"]).ToString("HH:mm:ss");
                    var dbMSNum = Convert.ToInt32(r["MSNum"]);
                    var dbEndState = Convert.ToInt32(r["MSWarEndState"]);
                    dbCheck = $" [DB: StepStart={dbStepStart}, WarStart={dbWarStart}, MSNum={dbMSNum}, EndState={dbEndState}]";
                }

                return (true, $"Instant war: {rows} rows updated, war at {warTime:HH:mm:ss} Step {step}.{dbCheck} Syncing...");
            }
            catch (Exception ex) { return (false, $"InstantWar Error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════
        //  BLOCKED ACCOUNTS / ANTI-CHEAT
        // ═══════════════════════════════════════════════════════════

        public async Task<List<BlockedAccount>> LoadBlockedAccountsAsync()
        {
            var list = new List<BlockedAccount>();
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT AccountName, BlockedType, StartDate, EndDate, AdminAccountName, BlockedReason, BlockedReasonForOnlyAdmin FROM td_blockedaccounts WITH (NOLOCK) ORDER BY StartDate DESC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BlockedAccount
                    {
                        AccountName = r["AccountName"]?.ToString()?.Trim() ?? "",
                        BlockedType = Convert.ToInt32(r["BlockedType"]),
                        StartDate = Convert.ToDateTime(r["StartDate"]),
                        EndDate = Convert.ToDateTime(r["EndDate"]),
                        AdminAccountName = r["AdminAccountName"]?.ToString()?.Trim() ?? "",
                        BlockedReason = r["BlockedReason"]?.ToString()?.Trim() ?? "",
                        BlockedReasonForOnlyAdmin = r["BlockedReasonForOnlyAdmin"]?.ToString()?.Trim() ?? ""
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadBlockedAccounts Error: {ex.Message}"); }
            return list;
        }

        public async Task<(bool ok, string msg)> BlockAccountAsync(string accountName, int blockType,
            DateTime startDate, DateTime endDate, string adminName, string reasonUser, string reasonAdmin)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("atum_Insert_BlockedAccounts", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@i_AccountName", accountName);
                cmd.Parameters.AddWithValue("@i_BlockedType", blockType);
                cmd.Parameters.AddWithValue("@i_StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@i_EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@i_AdminAccountName", adminName);
                cmd.Parameters.AddWithValue("@i_BlockedReason", reasonUser);
                cmd.Parameters.AddWithValue("@i_BlockedReasonForOnlyAdmin", reasonAdmin);
                await cmd.ExecuteNonQueryAsync();
                return (true, $"Account '{accountName}' blocked successfully.");
            }
            catch (Exception ex) { return (false, $"Block Error: {ex.Message}"); }
        }

        public async Task<(bool ok, string msg)> UnblockAccountAsync(string accountName)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand("atum_Delete_BlockedAccounts", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@i_AccountName", accountName);
                await cmd.ExecuteNonQueryAsync();
                return (true, $"Account '{accountName}' unblocked successfully.");
            }
            catch (Exception ex) { return (false, $"Unblock Error: {ex.Message}"); }
        }

        public async Task<(int total, int autoBlock, int manual, int speedHack, int memHack)> GetBlockedAccountStatsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
                    SELECT
                        COUNT(*) AS Total,
                        SUM(CASE WHEN BlockedType IN (7,8) THEN 1 ELSE 0 END) AS AutoBlock,
                        SUM(CASE WHEN BlockedType NOT IN (7,8) THEN 1 ELSE 0 END) AS Manual,
                        SUM(CASE WHEN BlockedType = 8 THEN 1 ELSE 0 END) AS SpeedHack,
                        SUM(CASE WHEN BlockedType = 7 THEN 1 ELSE 0 END) AS MemHack
                    FROM td_blockedaccounts WITH (NOLOCK)", conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return (Convert.ToInt32(r["Total"]), Convert.ToInt32(r["AutoBlock"]),
                            Convert.ToInt32(r["Manual"]), Convert.ToInt32(r["SpeedHack"]),
                            Convert.ToInt32(r["MemHack"]));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"BlockStats Error: {ex.Message}"); }
            return (0, 0, 0, 0, 0);
        }

        public async Task<bool> IsAccountExistsAsync(string accountName)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetAccountConnectionString());
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM td_Account WITH (NOLOCK) WHERE AccountName = @name", conn);
                cmd.Parameters.AddWithValue("@name", accountName);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
            }
            catch { return false; }
        }
    }
}
