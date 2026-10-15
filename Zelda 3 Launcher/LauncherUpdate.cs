using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Zelda_3_Launcher
{
    // All'avvio chiede a GitHub qual e' l'ultima release del launcher e, se e'
    // piu' recente di quella in uso, propone di aprire la pagina per scaricarla.
    // Senza connessione (o se GitHub non risponde in pochi secondi) non dice nulla.
    internal static class LauncherUpdate
    {
        public const string LatestApi = "https://api.github.com/repos/ghost19880/Zelda-3-Launcher/releases/latest";
        public const string LatestPage = "https://github.com/ghost19880/Zelda-3-Launcher/releases/latest";

        public static async void CheckAsync(Form owner)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Zelda-3-Launcher");
                var json = await client.GetStringAsync(LatestApi);
                var m = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?([0-9.]+)\"");
                if (!m.Success)
                    return;
                var latest = Version.Parse(Normalize(m.Groups[1].Value));
                var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
                if (latest <= Version.Parse(Normalize(current.ToString())))
                    return;
                var answer = Lang.Show("A new version of the launcher is available (" + latest.ToString(3) + ", you have " +
                    current.ToString(3) + ").\n\nDo you want to open the download page?",
                    "Launcher update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(LatestPage) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        // "1.4" -> "1.4.0.0": Version confronta bene solo con lo stesso numero di parti
        private static string Normalize(string v)
        {
            var parts = v.Split('.').ToList();
            while (parts.Count < 4)
                parts.Add("0");
            return string.Join(".", parts.Take(4));
        }
    }
}
