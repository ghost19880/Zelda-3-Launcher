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

        // L'exe compilato con MSVC ha l'intestazione "Rich" fra lo stub DOS e
        // l'intestazione PE, quello di TCC no. Serve a chi ha aggiornato prima che
        // l'exe veloce del suo commit fosse nella release: e' rimasto con TCC
        public static bool IsSlowBuild(string path)
        {
            try
            {
                var b = new byte[1024];
                int n;
                using (var f = File.OpenRead(path))
                    n = f.Read(b, 0, b.Length);
                if (n < 0x40 || b[0] != 'M' || b[1] != 'Z')
                    return false;
                int pe = Math.Min(BitConverter.ToInt32(b, 0x3c), n);
                for (int i = 0x40; i + 4 <= pe; i++)
                    if (b[i] == 'R' && b[i + 1] == 'i' && b[i + 2] == 'c' && b[i + 3] == 'h')
                        return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // true se nella release c'e' l'exe veloce del commit (senza scaricarlo)
        public static bool Available(string sha)
        {
            try
            {
                using var client = NewClient();
                return FindUrl(client.GetStringAsync(ReleaseApi).GetAwaiter().GetResult(), sha) != null;
            }
            catch
            {
                return false;
            }
        }

        private static HttpClient NewClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Zelda-3-Launcher");
            return client;
        }

        private static string? FindUrl(string json, string sha)
        {
            var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]*/zelda3-" + sha + "\\.exe)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static async Task<byte[]?> Download(string sha)
        {
            using var client = NewClient();
            var url = FindUrl(await client.GetStringAsync(ReleaseApi), sha);
            if (url == null)
                return null;
            var exe = await client.GetByteArrayAsync(url);
            // Un exe di Windows comincia con "MZ"
            return exe.Length > 100000 && exe[0] == 'M' && exe[1] == 'Z' ? exe : null;
        }
    }
}
