using AtumControlPanel.Forms;

namespace AtumControlPanel
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            while (true)
            {
                using var loginForm = new Login();
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;

                using var mainForm = new MainForm(loginForm.Config, loginForm.LoggedInAccount!);
                var result = mainForm.ShowDialog();

                if (result != DialogResult.Retry) // Retry = Logout, show login again
                    return;
            }
        }
    }
}
