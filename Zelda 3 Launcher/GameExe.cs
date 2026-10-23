using System.Net.Http;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Zelda_3_Launcher
{
    // Il gioco compilato con TCC e' molto piu' lento (TCC non ottimizza) e con le
    // migliorie HD va a scatti. Per ogni commit del gioco la release "exe" del
    // repository contiene zelda3-<sha>.exe compilato con MSVC: se c'e' quello del
    // commit scaricato si usa al posto della compilazione, altrimenti si compila
    // con TCC come prima.
    internal static class GameExe
    {
        public const string ReleaseApi = "https://api.github.com/repos/ghost19880/zelda3/releases/tags/exe";

        // true se zelda3.exe (e SDL2.dll) sono pronti nella cartella del gioco
        public static bool TryDownload()
        {
            try
            {
                string sha;
                using (var repo = new Repository(Program.repoDir))
                    sha = repo.Head.Tip.Sha;

                var task = Task.Run(() => Download(sha));
                while (!task.IsCompleted)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                var exe = task.Result;
                if (exe == null)
                    return false;

                var sdl = Path.Combine(Program.third_partyDir, "SDL2-2.28.2", "lib", "x64", "SDL2.dll");
                if (!File.Exists(sdl))
                    return false;
                File.WriteAllBytes(Path.Combine(Program.repoDir, "zelda3.exe"), exe);
                File.Copy(sdl, Path.Combine(Program.repoDir, "SDL2.dll"), true);
                File.AppendAllText(Program.currentDirectory + "\\zelda3.log", "\nDownloaded zelda3-" + sha + ".exe\n");
                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText(Program.currentDirectory + "\\zelda3.log", "\nzelda3.exe download failed: " + e.Message + "\n");
                return false;
            }
        }

        private static async Task<byte[]?> Download(string sha)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Zelda-3-Launcher");
            var json = await client.GetStringAsync(ReleaseApi);
            var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]*/zelda3-" + sha + "\\.exe)\"");
            if (!m.Success)
                return null;
            var exe = await client.GetByteArrayAsync(m.Groups[1].Value);
            // Un exe di Windows comincia con "MZ"
            return exe.Length > 100000 && exe[0] == 'M' && exe[1] == 'Z' ? exe : null;
        }
    }
}
