namespace Zelda_3_Launcher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Lang.Load();
            Application.Run(new MainForm());
        }

        // Da dove si scaricano il gioco con le migliorie HD e la traduzione italiana
        public const string GameRepository = "https://github.com/ghost19880/zelda3.git";
        public const string ItalianTranslation = "https://raw.githubusercontent.com/ghost19880/zelda3-italiano/main/dialogue_it.txt";

        public static string currentDirectory = Directory.GetCurrentDirectory();
        public static string repoDir = Path.Combine(currentDirectory, "zelda3");
        public static string third_partyDir = Path.Combine(repoDir, "third_party");
    }
}