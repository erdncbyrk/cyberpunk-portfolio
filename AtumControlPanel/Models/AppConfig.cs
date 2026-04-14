namespace AtumControlPanel.Models
{
    public class AppConfig
    {
        public string AccountDbServer { get; set; } = "localhost";
        public int AccountDbPort { get; set; } = 1433;
        public string AccountDbName { get; set; } = "atum2_db_account";
        public string AccountDbUser { get; set; } = "atum";
        public string AccountDbPassword { get; set; } = "callweb";

        public string GameDbServer { get; set; } = "localhost";
        public int GameDbPort { get; set; } = 1433;
        public string GameDbName { get; set; } = "atum2_db_1";
        public string GameDbUser { get; set; } = "atum";
        public string GameDbPassword { get; set; } = "callweb";

        // PreServer connection for admin commands
        public string PreServerIP { get; set; } = "127.0.0.1";
        public int PreServerPort { get; set; } = 40100;

        // Auto-Update paths
        public string UpdateClientPath { get; set; } = @"C:\inetpub\wwwroot\ftp_autoupdate\atumrivals";
        public string UpdateToolExe { get; set; } = @"C:\inetpub\wwwroot\ftp_autoupdate\atumrivals\DarksideUpdateTool.exe";
        public string VersionsCfgPath { get; set; } = @"C:\Server\config\versions.cfg";

        public string GetAccountConnectionString()
        {
            return $"Server={AccountDbServer},{AccountDbPort};Database={AccountDbName};User Id={AccountDbUser};Password={AccountDbPassword};Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=10;";
        }

        public string GetGameConnectionString()
        {
            return $"Server={GameDbServer},{GameDbPort};Database={GameDbName};User Id={GameDbUser};Password={GameDbPassword};Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=10;";
        }
    }
}
