using System.Runtime.CompilerServices;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace Zelda_3_Launcher
{
    // Lingua del launcher: inglese, oppure italiano quando in zelda3.ini e' scelta
    // la lingua italiana per i dialoghi. I testi restano scritti in inglese nel
    // codice e vengono tradotti qui.
    internal static class Lang
    {
        public static bool Italian;

        // Windows in italiano: senza impostazioni (primo avvio, ini nuovo) launcher
        // e dialoghi partono in italiano; per tutti gli altri in inglese
        public static bool WindowsItalian =>
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it";

        // Legge la lingua da zelda3.ini
        public static void Load()
        {
            Italian = false;
            try
            {
                var iniFile = IniFile.UserIni;
                if (!File.Exists(iniFile))
                {
                    Italian = WindowsItalian;
                    return;
                }
                var ini = IniFile.Load(iniFile);
                Italian = string.Equals(ini["General"]["Language"]?.Trim(), "it", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
        }

        // Testo intero (pulsanti, etichette, suggerimenti)
        public static string T(string en)
        {
            if (!Italian || en == null) return en!;
            return kItalian.TryGetValue(en, out var it) ? it : en;
        }

        // Messaggio composto da pezzi: si traducono le frasi che contiene
        public static string M(string en)
        {
            if (!Italian || en == null) return en!;
            if (kItalian.TryGetValue(en, out var whole)) return whole;
            foreach (var (a, b) in kSentences)
                en = en.Replace(a, b);
            return en;
        }

        public static DialogResult Show(string text) => MessageBox.Show(M(text));
        public static DialogResult Show(string text, string caption) => MessageBox.Show(M(text), T(caption));
        public static DialogResult Show(string text, string caption, MessageBoxButtons b) => MessageBox.Show(M(text), T(caption), b);
        public static DialogResult Show(string text, string caption, MessageBoxButtons b, MessageBoxIcon i) => MessageBox.Show(M(text), T(caption), b, i);
        public static DialogResult Show(string text, string caption, MessageBoxButtons b, MessageBoxIcon i, MessageBoxDefaultButton d) => MessageBox.Show(M(text), T(caption), b, i, d);

        // Testo originale (inglese) dei controlli, per poter tornare all'inglese
        // e per il codice che riconosce le scelte dal testo dei pulsanti
        private class Original { public string Text = ""; public string? Tip; }
        private static readonly ConditionalWeakTable<Control, Original> originals = new();

        public static string OrigText(this Control c) => originals.TryGetValue(c, out var o) ? o.Text : c.Text;

        // Traduce (o riporta in inglese) un form e tutti i suoi controlli
        public static void Apply(Control root, ToolTip? tip)
        {
            Walk(root, tip);
        }

        private static void Walk(Control c, ToolTip? tip)
        {
            if (!originals.TryGetValue(c, out var o))
            {
                o = new Original { Text = c.Text, Tip = tip?.GetToolTip(c) };
                originals.Add(c, o);
            }
            // Nei campi di testo e negli elenchi il testo e' un valore, non un'etichetta
            if (c is not TextBox && c is not ComboBox && c is not NumericUpDown)
                c.Text = o.Text.Contains('\n') ? M(o.Text) : T(o.Text);  // testi su piu' righe: frase per frase
            if (tip != null && !string.IsNullOrEmpty(o.Tip))
                tip.SetToolTip(c, T(o.Tip));
            foreach (Control child in c.Controls)
                Walk(child, tip);
        }

        private static readonly Dictionary<string, string> kItalian = new()
        {
            // Finestra principale
            ["Download"] = "Scarica",
            ["Update"] = "Aggiorna",
            ["Repair"] = "Ripara",
            ["Restore"] = "Ripristina",
            ["Re-build"] = "Ricompila",
            ["Launch Zelda 3"] = "Avvia Zelda 3",
            ["Launching..."] = "Avvio...",
            ["Running..."] = "In esecuzione...",
            ["Settings"] = "Impostazioni",
            ["Restore INI"] = "Ripristina INI",
            ["Downloading..."] = "Download...",
            ["Building..."] = "Compilazione...",
            ["Modifying python311._pth..."] = "Modifica di python311._pth...",
            ["Downloading pip..."] = "Download di pip...",
            ["Installing dependencies..."] = "Installazione dipendenze...",
            ["Extracting assets..."] = "Estrazione risorse...",
            ["Modifying installation bat..."] = "Modifica del file bat...",
            ["Building zelda3.exe..."] = "Compilazione di zelda3.exe...",
            ["Downloading zelda3.exe..."] = "Download di zelda3.exe...",
            ["Backing up zelda3.ini..."] = "Copia di sicurezza di zelda3.ini...",
            ["Modifying zelda3.ini..."] = "Modifica di zelda3.ini...",
            ["Done"] = "Fatto",

            // Finestre di avanzamento
            ["Update Status"] = "Stato",
            ["Repository Download"] = "Download del repository",
            ["Downloading a fresh copy of the zelda3 repository..."] = "Download di una copia aggiornata del repository zelda3...",
            ["Copying ROM File"] = "Copia della ROM",
            ["Copying ROM file to proper directory..."] = "Copia della ROM nella cartella corretta...",
            ["Downloading TCC"] = "Download di TCC",
            ["Downloading TCC build tools..."] = "Download degli strumenti di compilazione TCC...",
            ["Downloading SDL2"] = "Download di SDL2",
            ["Downloading SDL2..."] = "Download di SDL2...",
            ["Downloading Python"] = "Download di Python",
            ["Downloading Python..."] = "Download di Python...",
            ["Downloading pip"] = "Download di pip",

            // Impostazioni
            ["NOTE: You can hover over an option to get a more detailed description of what each setting does."] =
                "NOTA: passa con il mouse su un'opzione per leggerne una descrizione più dettagliata.",
            ["Gameplay"] = "Gioco",
            ["General"] = "Generale",
            ["Graphics"] = "Grafica",
            ["Sound"] = "Audio",
            ["Language:"] = "Lingua:",
            ["Keymapping"] = "Tasti",
            ["HD Improvements..."] = "Migliorie HD...",
            ["HD Improvements"] = "Migliorie HD",
            ["Reset"] = "Ripristina",
            ["Resetting..."] = "Ripristino...",
            ["Save"] = "Salva",
            ["Saving..."] = "Salvataggio...",
            ["Cancel"] = "Annulla",
            ["X button for items"] = "Tasto X per gli oggetti",
            ["Switch items with L and R"] = "Cambio oggetti con L e R",
            ["L and R buttons for items"] = "Tasti L e R per gli oggetti",
            ["Limit L/R Switch to First 4 Items"] = "L/R solo sui primi 4 oggetti",
            ["Allow Turning While Dashing"] = "Sterzata durante la corsa",
            ["Mirror Teleports to Dark World"] = "Specchio verso il Mondo delle Tenebre",
            ["Collect Items w/ Sword"] = "Raccogli oggetti con la spada",
            ["Break Pots w/ Sword"] = "Rompi vasi con la spada",
            ["Disable Low Heart Beep"] = "Niente bip con pochi cuori",
            ["Save hearts and magic state"] = "Salva stato cuori e magia",
            ["Skip Intro w/ Keypress"] = "Salta intro con un tasto",
            ["Indicate Max Resources"] = "Evidenzia risorse al massimo",
            ["More Active Bombs"] = "Più bombe attive",
            ["Larger Wallet"] = "Borsa più grande",
            ["Miscellaneous Minor Fixes"] = "Correzioni minori varie",
            ["Game Changing Bug Fixes"] = "Correzioni maggiori",
            ["Various GBA fixes"] = "Correzioni varie GBA",
            ["Various bug fixes from the GBA version"] = "Correzioni errori vari derivati dalla versione GBA",
            ["Enchanted rocks"] = "Rocce incantate",
            ["As on GBA, when Link enters an area, sometimes one small rock sparkles. Hit it with the sword for a few seconds: it drops rupees and then bombs."] =
                "Come su GBA, entrando in un'area a volte una roccia piccola brilla. Colpiscila con la spada per qualche secondo: lascia cadere rupie e poi bombe.",
            ["Map like A Link Between Worlds"] = "Mappa come A Link Between Worlds",
            ["The map now has the shortcuts of Zelda: A Link Between Worlds."] =
                "La mappa ora ha le scorciatoie di Zelda: A Link Between Worlds.",
            ["Title \"Triforce of the Gods\""] = "Titolo \"La Triforza degli Dei\"",
            ["The title screen shows \"Triforce of the Gods\" instead of \"A Link to the Past\", like the Japanese title. With the dialogue in Italian it shows \"La Triforza degli Dei\"."] =
                "Nella schermata del titolo compare \"La Triforza degli Dei\" al posto di \"A Link to the Past\", come nel titolo giapponese. Con i dialoghi in un'altra lingua compare \"Triforce of the Gods\".",
            ["Cancel Bird"] = "Annulla viaggio uccello",
            ["Bird travel point 9"] = "Punto di volo 9",
            ["Adds the ninth bird travel point of the GBA version, on the eastern summit of Death Mountain. It appears once Turtle Rock is open."] =
                "Aggiunge il nono punto di volo della versione GBA, in cima al Monte della Morte a est. Compare quando la Rocca della Tartaruga è aperta.",
            ["Adds a ninth point to the bird travel map: the eastern summit of Death Mountain, on the plateau with the pegs above Turtle Rock."] =
                "Aggiunge un nono punto alla mappa del volo con l'uccello: la cima a est del Monte della Morte, sulla piattaforma dei pioli sopra la Roccia della Tartaruga.",
            ["Autosave"] = "Autosalva",
            ["Performance in Title"] = "Prestazioni nel titolo",
            ["Unchanged Sprites"] = "Sprite invariati",
            ["Extend Y"] = "Estendi Y",
            ["Disable Frame Delay"] = "No ritardo frame",
            ["No Visual Fixes"] = "Senza correzioni",
            ["Aspect Ratio"] = "Proporzioni",
            ["Normal (4:3)"] = "Normale (4:3)",
            ["Widescreen (16:9)"] = "Panoramico (16:9)",
            ["Steam Deck (16:10)"] = "Steam Deck (16:10)",
            ["Laptop / tablet (3:2)"] = "Portatile / tablet (3:2)",
            ["Ultrawide (18:9)"] = "Ultrapanoramico (18:9)",
            ["Window Size"] = "Dimensione finestra",
            ["Auto"] = "Automatica",
            ["Custom"] = "Personalizzata",
            ["Fullscreen Mode"] = "Schermo intero",
            ["Windowed"] = "Finestra",
            ["Borderless Fullscreen"] = "Schermo intero senza bordi",
            ["Fullscreen"] = "Schermo intero esclusivo",
            ["Optimized SNES PPU"] = "PPU SNES ottimizzata",
            ["No Sprite Limit"] = "No limite sprite",
            ["Linear Filtering"] = "Filtro lineare",
            ["Enhanced Mode7"] = "Mode7 migliorato",
            ["Stretch"] = "Allarga",
            ["Fewer flashes (VC)"] = "Meno flash VC",
            ["Fewer flashes (GBA)"] = "Meno flash GBA",
            ["Render Method:"] = "Rendering:",
            ["Window Scaling:"] = "Scala finestra:",
            ["Custom Link Sprites"] = "Sprite di Link personalizzati",
            ["Custom Sprite Downloads"] = "Scarica sprite",
            ["Use GLSL Shader"] = "Usa shader GLSL",
            ["Suggested Shaders"] = "Shader consigliati",
            ["Enable Audio"] = "Attiva audio",
            ["Audio Channels"] = "Canali audio",
            ["Stereo"] = "Stereo",
            ["Mono"] = "Mono",
            ["Audio Frequency:"] = "Frequenza audio:",
            ["Audio Samples:"] = "Campioni audio:",
            ["Enable MSU"] = "Attiva MSU",
            ["MSU Settings"] = "Impostazioni MSU",
            ["Resume MSU"] = "Riprendi MSU",
            ["MSU Volume:"] = "Volume MSU:",
            ["MSU Version"] = "Versione MSU",
            ["MSU Directory:"] = "Cartella MSU:",
            ["Copying MSU Files..."] = "Copia dei file MSU...",

            // Suggerimenti delle impostazioni
            ["Limit the items you will cycle through to just the first four items"] = "Con L e R scorri solo i primi quattro oggetti",
            ["Allows you to turn when dashing with the Pegasus Boots, as in BS Zelda"] = "Puoi cambiare direzione mentre corri con i Calzari di Pegaso come su BS Zelda",
            ["Magic Mirror works in both directions"] = "Lo Specchio Magico funziona in entrambe le direzioni",
            ["You can collect items with your sword from a distance"] = "Puoi raccogliere oggetti a distanza con la spada",
            ["You can break pots if you have a L2 or higher sword"] = "Puoi rompere i vasi se hai una spada di livello 2 o superiore",
            ["Disables the beeping when you are low health"] = "Disattiva il bip quando hai pochi cuori",
            ["Save and quit remembers the hearts and magic you have instead of giving back the starting ones; on the file select, hearts not saved yet are black."] =
                "Salva ed esci ricorda i cuori e la magia che hai invece di ridare quelli di partenza; nella scelta del salvataggio i cuori non ancora salvati sono neri.",
            ["Allows you to skip the intro by pressing a key"] = "Puoi saltare l'introduzione premendo un tasto",
            ["Values will appear as yellow/orange when you have max resources (rupees, arrows, bombs)"] =
                "I valori diventano gialli/arancioni quando rupie, frecce o bombe sono al massimo",
            ["Can have 10 active bombs instead of 2"] = "Puoi avere fino a 10 bombe attive anziché 2",
            ["Can carry 9999 rupees"] = "Puoi portare 9999 rupie",
            ["Miscellaneous bug fixes"] = "Correzioni di errori varie",
            ["Some bug fixes that are significant enough to impact game behavior"] = "Correzioni di errori abbastanza importanti da cambiare il comportamento del gioco",
            ["Allows you to cancel bird travel with the sword button (B), like on GBA"] = "Puoi annullare il viaggio con l'uccello col tasto della spada (B), come su GBA",
            ["Displays 240 vertical lines rather than the default 224"] = "Mostra 240 righe verticali invece delle 224 originali",
            ["Avoid fixing some graphics glitches. If enabled, memory compare will not work."] = "Non corregge alcuni difetti grafici. Se attivo, il confronto della memoria non funziona.",
            ["Avoid changing sprite spawn/die behavior. (Required for replay compatibility)"] = "Non cambia la comparsa e scomparsa degli sprite (serve per la compatibilità dei replay)",
            ["Steam Deck\'s untraditional aspect ratio"] = "Le proporzioni particolari dello Steam Deck",
            ["Traditional widescreen screens"] = "Schermi panoramici tradizionali",
            ["Original output of the SNES"] = "L'immagine originale dello SNES",
            ["Show current performance in window title"] = "Mostra le prestazioni nel titolo della finestra",
            ["Automatically save state on quit and reload on start"] = "Salva lo stato all'uscita e lo ricarica all'avvio",
            ["Disable the SDL_Delay that happens on each frame (Gives slightly better performance if your display is set to exactly 60Hz)"] =
                "Disattiva l'SDL_Delay a ogni frame (prestazioni un po' migliori se lo schermo è esattamente a 60 Hz)",
            ["This is the aspect ratio which the game will render. All aspect ratios other than Normal will result in black bars on the left and right in various scenarios."] =
                "Le proporzioni con cui il gioco viene disegnato. Con proporzioni diverse da Normale in alcune scene compaiono bande nere ai lati.",
            ["The game will play within a window on the desktop at your selected aspect ratio and window scaling"] =
                "Il gioco si apre in una finestra sul desktop, con le proporzioni e la scala scelte",
            ["Game will launch in exclusive fullscreen mode"] = "Il gioco si avvia a schermo intero esclusivo",
            ["Game will launch in borderless fullscreen"] = "Il gioco si avvia a schermo intero senza bordi",
            ["Height"] = "Altezza",
            ["Width"] = "Larghezza",
            ["Set a custom aspect ratio"] = "Imposta una dimensione personalizzata",
            ["Automatically resize window to match selected aspect ratio"] = "Ridimensiona la finestra in base alle proporzioni scelte",
            ["Use an optimized SNES PPU implementation (potentially buggy)"] = "Usa una versione ottimizzata della PPU dello SNES (può avere difetti)",
            ["This sets the scaling of the game window when not fullscreen"] = "La scala della finestra di gioco quando non è a schermo intero",
            ["Display the world map with higher resolution"] = "Mostra la mappa del mondo ad alta risoluzione",
            ["Remove the sprite limit per scan line"] = "Rimuove il limite di sprite per riga",
            ["Use linear filter. Softens image and results in less crisp pixels."] = "Usa il filtro lineare: l'immagine è più morbida e i pixel meno netti.",
            ["Stretches the graphics to take up the entire window/screen regardless of aspect ratio."] = "Allarga l'immagine fino a riempire finestra o schermo, ignorando le proporzioni.",
            ["This will enable the use of GLSL shaders.\r\n\r\nThis is only supported by the OpenGL renderer."] =
                "Attiva l'uso degli shader GLSL.\r\n\r\nFunziona solo con il rendering OpenGL.",
            ["Opens a dialog for selecting your GLSL shader file."] = "Apre una finestra per scegliere il file dello shader GLSL.",
            ["The path where your .glsl or .slslp file is located"] = "Il percorso del file .glsl o .glslp",
            ["Opens a dialog for selecting your ZSPR custom sprite file."] = "Apre una finestra per scegliere il file ZSPR dello sprite.",
            ["The path where your .zspr file is located"] = "Il percorso del file .zspr",
            ["Enables custom ZSPR Link sprites. Not available with Blond Link (Graphics)."] =
                "Attiva gli sprite ZSPR personalizzati di Link. Non disponibile con Link biondo (Grafica).",
            ["Click here for some custom sprites you can download"] = "Clicca qui per scaricare sprite personalizzati",
            ["Rendering software used. SDL-software rendering may give better performance on Raspberry Pi"] =
                "Il sistema di rendering usato. SDL-Software può andare meglio su Raspberry Pi",
            ["Enable audio output by the game"] = "Attiva l'audio del gioco",
            ["DSP frequency in samples per second"] = "Frequenza del DSP in campioni al secondo",
            ["Audio buffer size in samples"] = "Dimensione del buffer audio in campioni",
            ["Outputs audio to the left or right speakers based where the sound is generated on screen"] =
                "Manda il suono a sinistra o a destra in base a dove nasce sullo schermo",
            ["Uniform audio output across all speakers"] = "Stesso suono su tutti gli altoparlanti",
            ["Settings specifically for MSU"] = "Impostazioni dedicate alla musica MSU",
            ["Select the version of MSU you wish to use"] = "Scegli la versione MSU da usare",
            ["MSU directory path"] = "Percorso della cartella MSU",
            ["Directory where MSU files are located"] = "La cartella dei file MSU",
            ["Volume of MSU output (0-100)"] = "Volume della musica MSU (0-100)",
            ["Resume MSU music from the same point in the track when returning to an area"] =
                "Tornando in un'area, la musica MSU riprende dal punto in cui era",
            ["Open dialog to select MSU directory"] = "Apre una finestra per scegliere la cartella MSU",
            ["Reset all settings to their default values"] = "Riporta tutte le impostazioni ai valori predefiniti",
            ["Save current settings and close window"] = "Salva le impostazioni e chiude la finestra",
            ["Close window without saving any settings"] = "Chiude la finestra senza salvare",
            ["Open a new settings menu exclusively for configuring keymappings for the game"] = "Apre la finestra per assegnare i tasti del gioco",
            ["HD improvements: camera, lighting, effects, transitions, automatic saves."] =
                "Migliorie HD: telecamera, luci, effetti, transizioni, salvataggi automatici.",
            ["This webpage will list the specific fixes"] = "Questa pagina elenca le correzioni",
            ["Select the language you would like to use."] = "Scegli la lingua dei dialoghi (e del launcher, se è l'italiano).",

            // Tasti
            ["Keymapper Utility"] = "Assegnazione tasti",
            ["Input Type"] = "Input",
            ["Keyboard"] = "Tastiera",
            ["Controller"] = "Controller",
            ["Assign All Input"] = "Assegna tutti i tasti",
            ["Assign All"] = "Assegna tutti",
            ["Cheats"] = "Trucchi",
            ["Give One Key:"] = "Dai una chiave:",
            ["Turbo:"] = "Turbo:",
            ["No Clip:"] = "No clip:",
            ["Restore Health:"] = "Ripristina salute:",
            ["Game Functions"] = "Funzioni di gioco",
            ["Volume Down:"] = "Volume giù:",
            ["Volume Up:"] = "Volume su:",
            ["Decrease Window Size:"] = "Riduci finestra:",
            ["Increase Window Size:"] = "Ingrandisci finestra:",
            ["Dim When Paused"] = "Scurisci in pausa",
            ["Controller rumble"] = "Vibrazione del controller",
            ["Vibrates the controller for explosions, damage, crashes with the Pegasus Boots, the screen shaking and other strong moments."] = "Fa vibrare il controller per esplosioni, danni, schianti con gli Stivali di Pegaso, lo schermo che trema e altri momenti forti.",
            ["Pause:"] = "Pausa:",
            ["Reset:"] = "Reset:",
            ["Replays"] = "Replay",
            ["Replay Controls"] = "Comandi dei replay",
            ["The game has an integrated replay/savestate system. Here are the controls:"] =
                "Il gioco ha un sistema integrato di replay e salvataggi rapidi. Ecco i comandi:",
            ["Toggle Replay Speed:"] = "Velocità replay:",
            ["Stop Replay:"] = "Ferma replay:",
            ["Performance"] = "Prestazioni",
            ["Toggle Renderer:"] = "Cambia renderer:",
            ["ToggleFPS:"] = "Mostra FPS:",
            ["Set each input sequentially (hit ESC to skip to the next input)"] = "Assegna i tasti uno dopo l'altro (ESC passa al successivo)",
            ["Select the input type you want to configure"] = "Scegli il tipo di comando da configurare",
            ["Allows you to walk through walls"] = "Permette di attraversare i muri",
            ["If you have no keys, give yourself a single key."] = "Se non hai chiavi, te ne dà una.",
            ["The game plays at an extremely fast speed."] = "Il gioco va a velocità molto alta.",
            ["Decreases the volume output of the game"] = "Abbassa il volume del gioco",
            ["Increases the volume output of the game"] = "Alza il volume del gioco",
            ["Decrease window scale by a factor of 1"] = "Riduce la scala della finestra di 1",
            ["Increase window scale by a factor of 1"] = "Aumenta la scala della finestra di 1",
            ["If enabled, the game will dim when paused via the special pause key"] = "Se attivo, il gioco si scurisce quando lo metti in pausa con il tasto apposito",
            ["Pause the game without opening the menu"] = "Mette in pausa senza aprire il menu",
            ["Reset the game"] = "Riavvia il gioco",
            ["Toggle between Turbo and regular speed of the replays."] = "Alterna velocità turbo e normale dei replay.",
            ["Stop the current replay in progress."] = "Ferma il replay in corso.",
            ["Save all controller settings and close menu"] = "Salva i tasti e chiude la finestra",
            ["Reset all controller related settings"] = "Riporta i tasti ai valori predefiniti",
            ["Close keymapping utility without saving any changes"] = "Chiude senza salvare le modifiche",
            ["Restores all health. Holding \"Shift\" when pushing the button will refill your other resources."] =
                "Ripristina tutti i cuori. Tenendo premuto \"Shift\" ricarica anche le altre risorse.",
            ["Up"] = "Su",
            ["Down"] = "Giù",
            ["Left"] = "Sinistra",
            ["Right"] = "Destra",
            ["Pause"] = "Pausa",
            ["Increase Window Size"] = "Ingrandisci finestra",
            ["Decrease Window Size"] = "Riduci finestra",
            ["Volume Up"] = "Volume su",
            ["Volume Down"] = "Volume giù",
            ["Restore Health"] = "Ripristina salute",
            ["One Key"] = "Una chiave",
            ["No Clip"] = "Attraversa muri",
            ["Toggle Replay Speed"] = "Velocità replay",
            ["Stop Replay"] = "Ferma replay",
            ["Toggle Renderer"] = "Cambia renderer",
            ["Toggle FPS"] = "Mostra FPS",
            ["Reset Game"] = "Riavvia il gioco",
            ["Default keys"] = "Tasti predefiniti",
            ["Give One Key"] = "Dai una chiave",
            ["Stop"] = "Ferma",
            ["Press..."] = "Premi...",
            ["Press a key"] = "Premi un tasto",
            ["Press the controller button. Esc cancels, after 5 seconds the previous one stays."] =
                "Premi il pulsante del controller. Esc annulla; dopo 5 secondi resta quello di prima.",
            ["Press the key. Esc cancels, after 5 seconds the previous one stays."] =
                "Premi il tasto. Esc annulla; dopo 5 secondi resta quello di prima.",
            ["Press the key (Ctrl, Shift and Alt can go with it) or a controller button. Esc cancels, after 5 seconds the previous one stays."] =
                "Premi il tasto (anche insieme a Ctrl, Maiusc o Alt) oppure un pulsante del controller. Esc annulla; dopo 5 secondi resta quello di prima.",
            ["Select the input type you want to configure. Controller can be chosen when one is connected."] =
                "Scegli se configurare la tastiera o il controller. Controller si può scegliere quando ne è collegato uno.",
            ["Puts back the default keys of both keyboard and controller. They are saved with Save."] =
                "Rimette i tasti predefiniti della tastiera e del controller. Si salvano con Salva.",
            ["Click the box and press the key, or the controller button if Controller is chosen above. Esc cancels; after 5 seconds the previous one stays."] =
                "Fai clic sulla casella e premi il tasto, o il pulsante del controller se sopra è scelto Controller. Esc annulla; dopo 5 secondi resta quello di prima.",
            ["Switches between the renderers while playing."] = "Cambia il sistema di rendering durante il gioco.",
            ["Shows or hides the frames per second while playing."] = "Mostra o nasconde i fotogrammi al secondo durante il gioco.",

            // Migliorie HD
            ["You can also change these while playing, from the options menu (Esc or right stick click)."] =
                "Si possono cambiare anche durante il gioco dal menu opzioni (Esc oppure clic della levetta destra).",
            ["Effects"] = "Effetti",
            ["Off"] = "Spenta",
            ["Light"] = "Leggera",
            ["Medium"] = "Media",
            ["Strong"] = "Forte",
            ["Sharp dialogue text"] = "Testo nitido nei dialoghi",
            ["Modern text"] = "Testo moderno",
            ["Dialogues with light upright letters on a translucent dark band instead of the original box. It replaces the sharp text."] =
                "Dialoghi con lettere chiare e dritte su una fascia scura trasparente al posto del riquadro originale. Sostituisce il testo nitido.",
            ["Redraws the dialogue box on top of the shader so the letters stay sharp."] = "Ridisegna il riquadro dei dialoghi sopra lo shader, così le lettere restano nitide.",
            ["Smoothness above 60 Hz"] = "Fluidità oltre i 60 Hz",
            ["On displays above 60 Hz, scrolling is interpolated in the extra frames. Nothing changes at 60 Hz."] =
                "Sugli schermi a più di 60 Hz interpola lo scorrimento nei frame in più. A 60 Hz non cambia niente.",
            ["Dynamic lighting"] = "Illuminazione dinamica",
            ["Glow around torches, fire and lava; dark rooms are lit only by burning flames."] =
                "Aloni attorno a torce, fuoco e lava; le stanze buie sono illuminate solo dalle fiamme accese.",
            ["Heat and flames effect"] = "Effetto calore e fiamme",
            ["The heat makes the air shimmer and sparks rise from torches, braziers, fire and lava."] =
                "Il calore fa tremolare l'aria e scintille salgono da torce, bracieri, fuoco e lava.",
            ["Light rays from windows"] = "Raggi dalle finestre",
            ["Light shafts with floating dust in rooms with windows."] = "Fasci di luce con pulviscolo nelle stanze con le finestre.",
            ["Glow on received items"] = "Bagliore degli oggetti ricevuti",
            ["Light rays behind Link when he holds up a newly found item."] = "Raggi luminosi dietro Link quando solleva un oggetto appena trovato.",
            ["Improved rain and lightning"] = "Pioggia e lampi migliorati",
            ["Denser rain with splashes; a single soft flash instead of the quick flickers."] =
                "Pioggia più fitta con spruzzi; un solo lampo morbido al posto dei lampeggi rapidi.",
            ["Improved fog"] = "Nebbia migliorata",
            ["The fog of the Lost Woods (before the Master Sword) and of the Skull Woods becomes soft drifting veils with depth, in the original shades: thicker in some places, thinner around Link."] =
                "La nebbia del Bosco Perduto (prima della Spada Suprema) e del Bosco d'Ossa diventa fatta di veli morbidi che scorrono con profondità, con le tonalità dell'originale: più fitta in certi punti, più rada attorno a Link.",
            ["Realistic water and waterfalls"] = "Acqua e cascate realistiche",
            ["Water as in Four Swords Adventures: depth, foam along the shores and around rocks, moving surface; flowing waterfalls with mist."] =
                "Acqua come in Four Swords Adventures: profondità, schiuma lungo le rive e attorno ai sassi, superficie che si muove; cascate che scorrono con nebbia.",
            ["Tell whirlpools from Zoras"] = "Distingui vortici da Zora",
            ["Teleport whirlpools give off magic sparkles."] = "I vortici di teletrasporto emettono bagliori magici",
            ["Room below in pits"] = "Stanza sotto le buche",
            ["Dungeon pits show the room you fall into, darker and with depth, instead of the plain pattern."] = "Nelle buche dei labirinti si vede la stanza in cui si cade, più scura e in profondità, invece della trama originale",
            ["Blur behind menus and dialogues"] = "Sfocatura dietro menu e dialoghi",
            ["The game is blurred and darkened behind the inventory and menus, and blurred inside dialogue boxes."] =
                "Il gioco si sfoca e si scurisce dietro l'inventario e i menu, e si sfoca dentro i riquadri dei dialoghi.",
            ["Shadows of characters and enemies"] = "Ombre di personaggi e nemici",
            ["Soft shadows under Link and enemies walking on the ground."] = "Ombre morbide sotto Link e i nemici che camminano a terra.",
            ["Official artwork look"] = "Aspetto dei disegni ufficiali",
            ["Colors closer to the official artwork: Agahnim in red with blue skin, King of Hyrule in red in the ending; the palaces' floor medallion shows the six-pointed star of the Japanese version."] =
                "Colori più vicini ai disegni ufficiali: Agahnim in rosso con la pelle blu, re di Hyrule vestito di rosso nel finale; il medaglione sul pavimento dei palazzi mostra la stella a sei punte della versione giapponese.",
            ["Soft transition circle"] = "Cerchio di transizione morbido",
            ["Soft edge on the circle transition when entering and leaving places."] =
                "Bordo sfumato nel cerchio di transizione quando si entra e si esce dai luoghi.",
            ["Button hints"] = "Suggerimenti dei tasti",
            ["On-screen hints for the buttons in the inventory and on the maps. Only with a wide aspect ratio: they sit in the side bands, in 4:3 there is no room."] =
                "Suggerimenti a schermo dei tasti nell'inventario e sulle mappe. Solo con lo schermo largo: stanno nelle fasce laterali, in 4:3 non c'è spazio.",
            ["On-screen hints for the buttons in the inventory and on the maps (wide aspect ratios only)"] =
                "Suggerimenti a schermo dei tasti nell'inventario e sulle mappe (solo con lo schermo largo)",
            ["Dialogue speed"] = "Velocità dei dialoghi",
            ["Original"] = "Originale",
            ["Fast holding A or B"] = "Veloce tenendo A o B",
            ["Instant"] = "Istantanea",
            ["How fast the dialogue text appears."] = "Quanto velocemente compare il testo dei dialoghi.",
            ["Fast transitions"] = "Transizioni veloci",
            ["Moving between areas and rooms is quick and smooth: no loading pauses, Link keeps walking and the camera starts and stops gently."] = "Il passaggio tra aree e stanze è rapido e fluido: niente pause di caricamento, Link non si ferma e la telecamera parte e frena dolcemente.",
            ["Continuous world"] = "Mondo continuo",
            ["Outdoors you see the neighbouring areas beyond the edge, the camera follows Link across it and the world remembers what you changed while you stay outdoors."] =
                "All'aperto si vedono le aree vicine oltre il bordo, la telecamera segue Link oltre il bordo e il mondo ricorda quello che hai cambiato finché resti all'aperto.",
            ["Modern HUD"] = "HUD moderno",
            ["Hearts and magic top left, Y/A/B buttons top right with the item, the sword and what the A button does (Talk, Read, Open...), rupees and keys bottom left."] =
                "Cuori e magia in alto a sinistra, tasti Y/A/B in alto a destra con oggetto, spada e azione del tasto A (Parla, Leggi, Apri...), rupie e chiavi in basso a sinistra.",
            ["Extended inventory"] = "Inventario esteso",
            ["The inventory has 6 columns like on GBA: four separate bottles, shovel and flute in separate slots."] =
                "L'inventario è su 6 colonne come su GBA: quattro bottiglie separate, pala e ocarina in posti separati.",
            ["Improved map"] = "Mappa migliorata",
            ["Place up to 24 stamps on the map so you don't forget anything. Press B to scroll faster and Y to go back to Link."] =
                "Inserisci fino a 24 timbri sulla mappa per non dimenticare niente. Premi B per scorrere più veloce e Y per tornare su Link.",
            ["Improved dungeon map"] = "Mappa dei labirinti migliorata",
            ["Changing floor on the dungeon map is faster, and the dungeon's name is shown at the top."] = "Sulla mappa del labirinto il cambio di piano è più rapido e in alto compare il nome del labirinto.",
            ["Progressive map"] = "Mappa progressiva",
            ["The map starts covered by clouds and is uncovered area by area as Link visits it."] =
                "La mappa all'inizio è coperta dalle nuvole e si scopre un'area alla volta, man mano che Link la visita.",
            ["Dungeon names"] = "Nomi dei labirinti",
            ["When Link enters a dungeon from outside, its name appears in the middle of the screen like on GBA and disappears as soon as he moves."] =
                "Quando Link entra in un labirinto dall'esterno, il nome compare al centro dello schermo come su GBA e sparisce appena si muove.",
            ["Automatic save"] = "Salvataggio automatico",
            ["Enable all"] = "Attiva tutto",
            ["Four Swords Adventures shader"] = "Shader Four Swords Adventures",
            ["Painted look in the style of Four Swords Adventures (uses OpenGL). The HUD, text and characters stay sharp."] =
                "Aspetto dipinto in stile Four Swords Adventures (usa OpenGL). HUD, testi e personaggi restano nitidi.",
            ["Disable all"] = "Disattiva tutto",
            ["Only the HD improvements: graphics, effects and gameplay."] = "Solo le migliorie HD: grafica, effetti e gioco.",
            ["Convenience"] = "Comodità",
            ["Sword and items world interaction"] = "Spada e oggetti interazione ambiente",
            ["Items refill resources"] = "Oggetti riempiono le risorse",
            ["Gold Medicine"] = "Pozione d'oro",
            ["From BS Zelda: a fourth cauldron in the witch's shop, 50 rupees, one per day (computer clock, back at midnight). Like the blue medicine it refills life and magic, and for 10 minutes the Spin Attack charges twice as fast."] =
                "Da BS Zelda: un quarto calderone nella bottega della strega, 50 rupie, una al giorno (orologio del computer, torna a mezzanotte). Come la pozione blu ridà energia e Magia, e per 10 minuti l'attacco rotante si carica al doppio della velocità.",
            ["Palace 6 as on GBA"] = "Palazzo 6 come GBA",
            ["Changes the Palace 6 puzzle to match the GBA version"] = "Modifica l'enigma del Palazzo 6 perché rispecchi la versione GBA",
            ["Getting a magic item refills the magic meter, the Master Sword also the hearts; the Bow comes with 10 arrows"] =
                "Ottenere un oggetto magico riempie la barra della magia, la Spada Suprema anche i cuori; l'Arco arriva con 10 frecce",
            ["Burn and freeze interaction"] = "Interazione bruciatura e congelamento",
            ["The world physically reacts to fire and ice: the lamp, the rods and the medallions burn or freeze bushes and grass."] =
                "Il mondo reagisce fisicamente a fuoco e ghiaccio: Lanterna, bacchette e medaglioni bruciano o congelano cespugli ed erba.",
            ["Fewer flashes during gameplay, like on Virtual Console"] =
                "Meno lampeggiamenti durante il gioco come su Virtual Console",
            ["Fewer flashes during gameplay, like on GBA"] =
                "Meno lampeggiamenti durante il gioco come su GBA",
            ["Swords and items behave like on GBA: every sword collects hearts, rupees and other items, the Master Sword breaks signs, the Gold Sword breaks pots; arrows break pots, the hammer breaks bushes and small rocks, the lamp and the fire rod cut grass and bushes and the lamp flame hurts enemies."] =
                "Spade e oggetti si comportano come su GBA: ogni spada raccoglie cuori, rupie e altri oggetti, la Spada Suprema rompe i cartelli, la Spada d'Oro i vasi; le frecce rompono i vasi, il martello cespugli e rocce piccole, Lanterna e Bacchetta del Fuoco tagliano erba e cespugli e la fiamma della Lanterna ferisce i nemici.",
            ["Enemy damage as on GBA"] = "Danni dei nemici come su GBA",
            ["Enemies take the damage of the GBA version: some change with Ether, Bombos, Quake, the hookshot and the magic powder. Takes effect when the game starts."] =
                "I nemici subiscono i danni della versione GBA: alcuni cambiano con i Medaglioni dell'Etere, della Fiamma e Sisma, il Rampino e la Polvere Magica. Vale dall'avvio del gioco.",
            ["Hurricane Spin"] = "Giravolta Tornado",
            ["As on GBA, once you have the Bug-Catching Net: hold the sword charge longer and the spin attack becomes a Hurricane Spin. Press B while spinning to spin again and move with the D-pad; each spin costs magic."] =
                "Come su GBA, una volta ottenuto il Retino per Farfalle: tenendo la carica della spada più a lungo, il colpo rotante diventa la Giravolta Tornado. Premi B mentre giri per fare un altro giro e muoverti con la croce; ogni giro costa magia.",
            ["Dive with the flippers"] = "Immersione con le pinne",
            ["While swimming, B makes Link dive underwater for a moment, as on GBA."] =
                "Mentre nuota, con B Link si immerge per un momento, come su GBA.",
            ["Arrows stuck in walls"] = "Frecce piantate nei muri",
            ["The arrows of Link and of the enemies stay stuck in walls instead of vanishing."] =
                "Le frecce di Link e dei nemici restano piantate nei muri invece di sparire.",
            ["Blond Link"] = "Link biondo",
            ["Link with blond hair, as in the official artwork. Custom Link sprites can't be used while it is on."] =
                "Link con i capelli biondi, come nei disegni ufficiali. Finché è acceso non si possono usare Link personalizzati.",
            ["World interaction"] = "Interazione con l'ambiente",
            ["Items and combat"] = "Oggetti e combattimento",
            ["Dungeons"] = "Labirinti",
            ["Saves and start"] = "Salvataggi e avvio",
            ["Items and controls"] = "Oggetti e comandi",
            ["Bug fixes"] = "Correzioni",
            ["In the item menu, hold X and pick an item: X will use it in the game.\n\nWith an item on X, Select opens the map; to save and quit, press Select in the item menu. An item can be on one button only among Y, X, L and R."] =
                "Nel menu degli oggetti tieni premuto X e scegli un oggetto: X lo userà in gioco.\n\nCon un oggetto su X la mappa si apre con Select; per salvare e uscire premi Select nel menu degli oggetti. Un oggetto può stare su un solo tasto fra Y, X, L e R.",
            ["L and R change the item of the Y button without opening the menu.\n\nNot together with \"L and R buttons for items\": turning one on turns the other off."] =
                "L e R cambiano l'oggetto del tasto Y senza aprire il menu.\n\nNon si può usare insieme a \"Tasti L e R per gli oggetti\": accendendone una, l'altra si spegne.",
            ["In the item menu, hold L or R and pick an item: that button will use it in the game. An item can be on one button only among Y, X, L and R.\n\nNot together with \"Switch items with L and R\": turning one on turns the other off."] =
                "Nel menu degli oggetti tieni premuto L o R e scegli un oggetto: quel tasto lo userà in gioco. Un oggetto può stare su un solo tasto fra Y, X, L e R.\n\nNon si può usare insieme a \"Cambio oggetti con L e R\": accendendone una, l'altra si spegne.",
            ["Unable to save zelda3.ini:\n\n"] = "Impossibile salvare zelda3.ini:\n\n",
            ["Error"] = "Errore",

            // Finestra principale nuova
            ["Play"] = "Gioca",
            ["Checking for updates..."] = "Controllo aggiornamenti...",
            ["{0} new"] = "{0} novità",
            ["no news"] = "nessuna novità",
            ["offline"] = "senza connessione",
            ["Game"] = "Gioco",
            ["not downloaded"] = "non scaricato",
            ["Modified locally (1 file)"] = "Modificato in locale (1 file)",
            ["Modified locally ({0} files)"] = "Modificato in locale ({0} file)",
            ["Not built yet"] = "Non ancora compilato",
            ["No local changes"] = "Nessuna modifica locale",
            ["Press Download to get the game"] = "Premi Scarica per avere il gioco",

            // Finestra unica delle impostazioni
            ["You can also change these in game (Esc)"] = "Si cambiano anche in gioco (Esc)",

            // Trucchi
            ["All off at the start; the ones with a key only work when on"] = "Tutti spenti all'inizio; quelli con un tasto funzionano solo se accesi",
            ["Cheats with a key"] = "Trucchi con un tasto",
            ["Always active"] = "Sempre attivi",
            ["Walk through walls (No Clip)"] = "Attraversi i muri (No Clip)",
            ["The key makes Link walk through walls; press it again to stop."] =
                "Il tasto fa attraversare i muri a Link; premilo di nuovo per smettere.",
            ["Instant death (Kill Link)"] = "Morte istantanea (Uccidi Link)",
            ["The key kills Link at once, even with infinite life."] = "Il tasto uccide subito Link, anche con la vita infinita.",
            ["Fill hearts and magic"] = "Riempi al massimo cuori e magia",
            ["The key fills hearts and magic to the maximum. Holding Shift with it also gives bombs, arrows and rupees."] =
                "Il tasto carica cuori e magia fino al massimo. Tenendo premuto anche Shift dà pure bombe, frecce e rupie.",
            ["The key gives a small key if you have none."] = "Il tasto dà una chiave piccola se non ne hai.",
            ["While the key is held the game plays at an extremely fast speed. Always active."] = "Finché tieni premuto il tasto il gioco va velocissimo. Sempre attivo.",
            ["Instant kill (any enemy in one hit)"] = "Uccisione istantanea (qualsiasi nemico con un colpo)",
            ["Any hit that hurts an enemy kills it at once. Enemies that a weapon can't hurt stay unhurt."] =
                "Ogni colpo che ferisce un nemico lo uccide subito. I nemici che un'arma non può ferire restano illesi.",
            ["Infinite life"] = "Vita infinita",
            ["Hearts don't go down when Link is hit or falls into a pit."] = "I cuori non calano quando Link viene colpito o cade in una buca.",
            ["Infinite magic"] = "Magia infinita",
            ["Magic doesn't go down when used."] = "La magia non cala quando la usi.",
            ["Infinite rupees"] = "Rupie infinite",
            ["Rupees don't go down when spent."] = "Le rupie non calano quando le spendi.",
            ["Infinite bombs"] = "Bombe infinite",
            ["Bombs don't go down when used."] = "Le bombe non calano quando le usi.",
            ["Infinite arrows"] = "Frecce infinite",
            ["Arrows don't go down when shot."] = "Le frecce non calano quando le scocchi.",

            // Salvataggio
            ["Save file"] = "Salvataggio",
            ["Import a SNES save and edit the game files"] = "Importa un salvataggio SNES e modifica le partite",
            ["Import save"] = "Importa salvataggio",
            ["SNES save (.srm)"] = "Salvataggio SNES (.srm)",
            ["Choose the save of a SNES emulator (.srm, A Link to the Past in English or Italian): it becomes the game's save (saves\\sram.dat) with its three game files. The previous one is copied to saves\\backup_import."] =
                "Scegli il salvataggio di un emulatore SNES (.srm, A Link to the Past in inglese o italiano): diventa il salvataggio del gioco (saves\\sram.dat) con le sue tre partite. Quello di prima viene copiato in saves\\backup_import.",
            ["Edit save"] = "Modifica salvataggio",
            ["Player name"] = "Nome del giocatore",
            ["Makes a copy of the current save, then opens the editor. With \"Restore save\" you go back to that copy."] =
                "Fa una copia del salvataggio attuale, poi apre l'editor. Con \"Ripristina salvataggio\" torni a quella copia.",
            ["Restore save"] = "Ripristina salvataggio",
            ["Copy made by \"Edit save\""] = "Copia fatta da \"Modifica salvataggio\"",
            ["Puts back the save as it was when you last pressed \"Edit save\" (game files and quick saves)."] =
                "Rimette il salvataggio com'era l'ultima volta che hai premuto \"Modifica salvataggio\" (partite e salvataggi rapidi).",
            ["Saved games"] = "Partite",
            ["The three game files of the title screen, read from saves\\sram.dat."] = "Le tre partite della schermata iniziale, lette da saves\\sram.dat.",
            ["The quick saves (menu slots, F1 - F10) keep their own copy of the game files: importing and editing update them too, so loading one doesn't bring back the old save. Close the game first."] =
                "I salvataggi rapidi (slot del menu, F1 - F10) hanno una loro copia delle partite: importando e modificando si aggiornano anche quelli, così caricarne uno non riporta il salvataggio di prima. Chiudi prima il gioco.",
            ["No save yet: start a game first, or import one."] = "Ancora nessun salvataggio: inizia una partita o importane uno.",
            ["{0} hearts"] = "{0} cuori",
            ["(empty)"] = "(vuota)",
            ["Copy of {0}"] = "Copia del {0}",
            ["No copy yet"] = "Ancora nessuna copia",
            ["Close the game before changing the save."] = "Chiudi il gioco prima di cambiare il salvataggio.",
            ["SNES save"] = "Salvataggio SNES",
            ["All files"] = "Tutti i file",
            ["This file is not an A Link to the Past save: it is smaller than 8 KB."] =
                "Questo file non è un salvataggio di A Link to the Past: è più piccolo di 8 KB.",
            ["No game file was found in this save (it may be empty or from another game).\n\nDo you want to import it anyway?"] =
                "In questo salvataggio non c'è nessuna partita (potrebbe essere vuoto o di un altro gioco).\n\nVuoi importarlo lo stesso?",
            ["The current save will be replaced (a copy goes to saves\\backup_import).\n\nDo you want to continue?"] =
                "Il salvataggio attuale verrà sostituito (una copia va in saves\\backup_import).\n\nVuoi continuare?",
            ["Save imported. Choose the game file on the title screen."] = "Salvataggio importato. Scegli la partita nella schermata iniziale.",
            ["Unable to import the save:"] = "Impossibile importare il salvataggio:",
            ["There is no game file to edit: start a game first, or import a save."] =
                "Non c'è nessuna partita da modificare: inizia una partita o importa un salvataggio.",
            ["Unable to make a copy of the save:"] = "Impossibile fare una copia del salvataggio:",
            ["The save will go back to the copy of {0}.\n\nDo you want to continue?"] =
                "Il salvataggio tornerà alla copia del {0}.\n\nVuoi continuare?",
            ["Unable to restore the save:"] = "Impossibile ripristinare il salvataggio:",
            ["The game files of the title screen (saves\\sram.dat)"] = "Le partite della schermata iniziale (saves\\sram.dat)",
            ["Game file"] = "Partita",
            ["Space"] = "Spazio",
            ["Type letters (A-Z, a-z), numbers and space on the keyboard, or use the buttons. The Link icon takes two boxes, as in the game. As in the game, a lowercase l looks like an uppercase I."] =
                "Scrivi lettere (A-Z, a-z), numeri e spazio con la tastiera, oppure usa i pulsanti. L'icona di Link occupa due caselle, come nel gioco. Come nel gioco, la l minuscola è uguale alla I maiuscola.",
            ["The name can't be empty."] = "Il nome non può essere vuoto.",
            ["Unable to write the save:"] = "Impossibile scrivere il salvataggio:",
            // Editor del salvataggio
            ["Inventory"] = "Inventario",
            ["Pieces of heart"] = "Frammenti di cuore",
            ["Player"] = "Giocatore",
            ["Link icon"] = "Icona di Link",
            ["Puts the Link icon in the name: it takes two boxes, as in the game."] = "Mette l'icona di Link nel nome: occupa due caselle, come nel gioco.",
            ["Progression"] = "Avanzamento",
            ["Part of the story the game file is in: it decides, among other things, where Link can start."] =
                "Punto della storia in cui si trova la partita: decide, tra l'altro, da dove può ripartire Link.",
            ["Part 1: Equipment"] = "Parte 1: l'equipaggiamento",
            ["Part 2: Rescuing Zelda"] = "Parte 2: salvare Zelda",
            ["Part 3: The 3 Pendants of Virtue"] = "Parte 3: i tre Amuleti",
            ["Part 4: The 7 Crystals"] = "Parte 4: i sette Cristalli",
            ["Save count"] = "Conteggio",
            ["Saves and deaths of the game file so far."] = "Salvataggi e morti della partita fino a ora.",
            ["Total count"] = "Conteggio totale",
            ["Total of saves and deaths shown at the end of the game."] = "Totale di salvataggi e morti mostrato alla fine del gioco.",
            ["Life and magic"] = "Vita e magia",
            ["Heart containers"] = "Contenitori di cuore",
            ["Maximum hearts (3 to 20)."] = "Cuori massimi (da 3 a 20).",
            ["Hearts"] = "Cuori",
            ["Hearts when the game file starts."] = "Cuori alla ripresa della partita.",
            ["Pieces of heart collected towards the next heart container (4 make a container)."] =
                "Frammenti raccolti verso il prossimo contenitore (4 fanno un contenitore).",
            ["Magic"] = "Magia",
            ["Magic meter: 128 is full."] = "Barra della magia: 128 è piena.",
            ["Magic upgrade"] = "Consumo di magia",
            ["Normal"] = "Normale",
            ["Half magic"] = "Metà magia",
            ["Magic consumption: normal, or halved by the fairy of the Pyramid's fountain."] =
                "Consumo di magia: normale, o dimezzato dalla fata della fonte della Piramide.",
            ["Rupees"] = "Rupie",
            ["Rupees of the game file (999, or 9999 with the Larger Wallet option)."] = "Rupie della partita: fino a 999, o fino a 9999 se nel launcher è accesa l'opzione Borsa più grande (Oggetti).",
            ["Rupees in the fountain"] = "Rupie nella fonte",
            ["Rupees already thrown into the fountain of the Lake Hylia fairy."] = "Rupie già gettate nella fonte della fata del Lago Hylia.",
            ["Hookshot"] = "Arpione",
            ["Fire Rod"] = "Bacchetta del Fuoco",
            ["Ice Rod"] = "Bacchetta del Ghiaccio",
            ["Bombos Medallion"] = "Medaglione della Fiamma",
            ["Ether Medallion"] = "Medaglione dell'Etere",
            ["Quake Medallion"] = "Medaglione Sisma",
            ["Lamp"] = "Lanterna",
            ["Magic Hammer"] = "Martello Magico",
            ["Bug-Catching Net"] = "Retino per Farfalle",
            ["Book of Mudora"] = "Libro di Mudora",
            ["Cane of Somaria"] = "Bastone di Somaria",
            ["Cane of Byrna"] = "Bastone di Byrna",
            ["Magic Cape"] = "Mantello Magico",
            ["Magic Mirror"] = "Specchio Magico",
            ["Pegasus Boots"] = "Stivali di Pegaso",
            ["Zora's Flippers"] = "Pinne di Zora",
            ["Moon Pearl"] = "Perla Lunare",
            ["Pendants and crystals"] = "Amuleti e cristalli",
            ["Pendant of Courage"] = "Amuleto del Coraggio",
            ["Pendant of Power"] = "Amuleto della Forza",
            ["Pendant of Wisdom"] = "Amuleto della Saggezza",
            ["Crystal"] = "Cristallo",
            ["Equipment"] = "Equipaggiamento",
            ["Sword"] = "Spada",
            ["Fighter's Sword"] = "Spada del Guerriero",
            ["Master Sword"] = "Spada Suprema",
            ["Tempered Sword"] = "Spada Temprata",
            ["Golden Sword"] = "Spada d'Oro",
            ["Shield"] = "Scudo",
            ["Fighter's Shield"] = "Scudo del Guerriero",
            ["Red Shield"] = "Scudo Rosso",
            ["Mirror Shield"] = "Scudo Specchio",
            ["Tunic"] = "Corazza",
            ["Green Tunic"] = "Tunica verde",
            ["Blue Mail"] = "Corazza Blu",
            ["Red Mail"] = "Corazza Rossa",
            ["Glove"] = "Guanti",
            ["Power Glove"] = "Guanti della Forza",
            ["Titan's Mitt"] = "Bracciali del Titano",
            ["Bow"] = "Arco",
            ["Bow & Arrows"] = "Arco e frecce",
            ["Bow & Silver Arrows"] = "Arco e frecce d'argento",
            ["Boomerang"] = "Boomerang",
            ["Magical Boomerang"] = "Boomerang Magico",
            ["Mushroom / Magic Powder"] = "Fungo / Polvere Magica",
            ["Mushroom"] = "Fungo",
            ["Magic Powder"] = "Polvere Magica",
            ["Shovel / Flute"] = "Pala / Ocarina",
            ["Shovel"] = "Pala",
            ["Flute"] = "Ocarina",
            ["Flute with Bird"] = "Ocarina con l'uccello",
            ["With the extended inventory the flute also keeps the shovel."] = "Con l'inventario esteso l'ocarina tiene anche la pala.",
            ["Bombs and arrows"] = "Bombe e frecce",
            ["Up to"] = "Fino a",
            ["Bombs"] = "Bombe",
            ["Arrows"] = "Frecce",
            ["Bombs held and the most the bag can hold (upgrades from the fairy of the Pyramid's fountain)."] =
                "Bombe che hai e quante ne può tenere la borsa (aumenti della fata della fonte della Piramide).",
            ["Arrows held and the most the quiver can hold (upgrades from the fairy of the Pyramid's fountain)."] =
                "Frecce che hai e quante ne può tenere la faretra (aumenti della fata della fonte della Piramide).",
            ["Bottles"] = "Ampolle",
            ["Content of the bottles: \"-\" means no bottle."] = "Contenuto delle ampolle: \"-\" vuol dire nessuna ampolla.",
            ["Empty bottle"] = "Ampolla vuota",
            ["Gold Medicine (option off)"] = "Pozione d'oro (opzione spenta)",
            ["Content of the bottles: \"-\" means no bottle. The Gold Medicine can be chosen only with the Gold Medicine option on (Items)."] =
                "Contenuto delle ampolle: \"-\" vuol dire nessuna ampolla. La Pozione d'oro si può scegliere solo con l'opzione Pozione d'oro accesa (Oggetti).",
            ["Life Medicine"] = "Pozione Vita",
            ["Magic Medicine"] = "Pozione Magica",
            ["Cure-All Medicine"] = "Pozione Curatutto",
            ["Faerie"] = "Fata",
            ["Bee"] = "Ape",
            ["Good Bee"] = "Ape d'oro",
            ["Map"] = "Mappa",
            ["Compass"] = "Bussola",
            ["Big Key"] = "Chiave grande",
            ["Boss"] = "Boss",
            ["Small keys"] = "Chiavi",
            ["Heart Container"] = "Contenitore di cuore",
            ["Agahnim defeated"] = "Agahnim sconfitto",
            ["The dungeon map has been found."] = "La mappa del labirinto è stata trovata.",
            ["The dungeon compass has been found."] = "La bussola del labirinto è stata trovata.",
            ["The big key has been found."] = "La chiave grande è stata trovata.",
            ["The boss's heart container has been taken."] = "Il contenitore di cuore del boss è stato preso.",
            ["Agahnim has been defeated here."] = "Agahnim è stato sconfitto qui.",
            ["Small keys held for this dungeon."] = "Chiavi piccole che hai per questo labirinto.",
            ["Saves and deaths in this dungeon, counted for the end of the game."] = "Salvataggi e morti in questo labirinto, contati per la fine del gioco.",
            ["Hyrule Castle"] = "Castello di Hyrule",
            ["Eastern Palace"] = "Palazzo dell'Est",
            ["Desert Palace"] = "Castello di Sabbia",
            ["Tower of Hera"] = "Torre sul Monte",
            ["Agahnim's Tower"] = "Sala superiore del castello",
            ["Palace of Darkness (Level 1)"] = "Palazzo Cupo (livello 1)",
            ["Swamp Palace (Level 2)"] = "Dedalo d'Acqua (livello 2)",
            ["Skull Woods (Level 3)"] = "Bosco d'Ossa (livello 3)",
            ["Thieves' Town (Level 4)"] = "Tana dei Ladroni (livello 4)",
            ["Ice Palace (Level 5)"] = "Forte Ghiaccio (livello 5)",
            ["Misery Mire (Level 6)"] = "Malapalude (livello 6)",
            ["Turtle Rock (Level 7)"] = "Rocca Tartaruga (livello 7)",
            ["Ganon's Tower (Level 8)"] = "Torre di Ganon (livello 8)",
            ["Light World"] = "Mondo della Luce",
            ["Death Mountain and Lake Hylia"] = "Monte Morte e Lago Hylia",
            ["Dark World"] = "Mondo delle Tenebre",
            ["On: the piece of heart has already been taken. This doesn't change the pieces counted in General."] =
                "Acceso: il frammento di cuore è già stato preso. Non cambia i frammenti contati in Generale.",
            ["Lost Woods: in the thieves' hideout"] = "Bosco Perduto: nel covo dei ladri",
            ["Kakariko Village: in a well"] = "Villaggio Calbarico: in un pozzo",
            ["Kakariko Village: in the house with the grass roof"] = "Villaggio Calbarico: nella casa col tetto d'erba",
            ["South of Kakariko Village: race mini-game won"] = "A sud del Villaggio Calbarico: gara vinta",
            ["Sanctuary: in a cave above a pile of rocks"] = "Santuario: in una grotta sopra un mucchio di massi",
            ["Graveyard: in a cave"] = "Cimitero: in una grotta",
            ["Lumberjacks' house: in a cave under a tree"] = "Casa dei taglialegna: in una grotta sotto un albero",
            ["Great Swamp: in the water near the ruins"] = "Grande Palude: nell'acqua vicino alle rovine",
            ["Desert of Mystery: in a secret room near Aginah"] = "Deserto Mistero: in una stanza segreta vicino ad Aginah",
            ["Desert of Mystery: on a ledge left of the Desert Palace"] = "Deserto Mistero: su una sporgenza a sinistra del Castello di Sabbia",
            ["Desert of Mystery: in a cave in the north-east corner"] = "Deserto Mistero: in una grotta nell'angolo a nord-est",
            ["Zora's Waterfall: at the end of a path near a fence"] = "Cascata di Zora: in fondo a un sentiero vicino a uno steccato",
            ["Foot of Death Mountain: on a ledge near a signpost"] = "Ai piedi del Monte Morte: su una sporgenza vicino a un cartello",
            ["Death Mountain: in a cave near a telepathy tile"] = "Monte Morte: in una grotta vicino a una piastrella della telepatia",
            ["Death Mountain: at the top of the mountain"] = "Monte Morte: in cima alla montagna",
            ["Death Mountain: in a cave guarded by 4 green Goriyas"] = "Monte Morte: in una grotta sorvegliata da 4 Goriya verdi",
            ["Spectacle Rock: at the top"] = "Roccia Belvedere: in cima",
            ["Pyramid of Power: on a ledge right of the pyramid"] = "Piramide: su una sporgenza a destra della piramide",
            ["Lake Hylia: on an island"] = "Lago Hylia: su un'isola",
            ["South of the Haunted Grove: in a cave on the plateau"] = "A sud del Boschetto Infestato: in una grotta sull'altopiano",
            ["Treasure Field: dug up in the mini-game"] = "Campo Tesori: dissotterrato nel minigioco",
            ["Village of Outcasts: treasure chest mini-game"] = "Villaggio dei Reietti: minigioco dei forzieri",
            ["Stake Garden: in a secret cave"] = "Giardino dei paletti: in una grotta segreta",
            ["Swamp of Evil: inside the left head"] = "Palude del Male: dentro la testa di sinistra",
            ["Items"] = "Oggetti",
            ["Audio"] = "Audio",
            ["Controls"] = "Controlli",
            ["Advanced"] = "Avanzate",
            ["Window, rendering and performance"] = "Finestra, rendering e prestazioni",
            ["Image, HD look and effects"] = "Immagine, aspetto HD ed effetti",
            ["How the world moves, what the screen shows, dialogue and saves"] =
                "Come si muove il mondo, cosa mostra lo schermo, dialoghi e salvataggi",
            ["Items, controls and fixes of the original game"] = "Oggetti, comandi e correzioni del gioco originale",
            ["Sound output and MSU music"] = "Uscita audio e musica MSU",
            ["Keyboard and controller buttons"] = "Tasti della tastiera e del controller",
            ["Compatibility, custom files and maintenance"] = "Compatibilità, file personalizzati e manutenzione",
            ["Move the mouse over an option to read what it does."] = "Passa il mouse su un'opzione per leggere cosa fa.",
            ["Click a box and press the key or button to assign. Hover over an item for details."] =
                "Fai clic su una casella e premi il tasto o il pulsante da assegnare. Passa il mouse su una voce per i dettagli.",
            ["Window"] = "Finestra",
            ["Image"] = "Immagine",
            ["HD look"] = "Aspetto HD",
            ["World and transitions"] = "Mondo e transizioni",
            ["Dialogue"] = "Dialoghi",
            ["Interface"] = "Interfaccia",
            ["Maps"] = "Mappe",
            ["Saves and convenience"] = "Salvataggi e comodità",
            ["Item selection"] = "Selezione degli oggetti",
            ["Movement and travel"] = "Movimento e viaggi",
            ["World and items"] = "Mondo e oggetti",
            ["Output"] = "Uscita",
            ["MSU music"] = "Musica MSU",
            ["Compatibility"] = "Compatibilità",
            ["Maintenance"] = "Manutenzione",
            ["Custom files"] = "File personalizzati",
            ["Display mode"] = "Modalità schermo",
            ["Width × height"] = "Larghezza × altezza",
            ["Window scaling"] = "Scala della finestra",
            ["Render method"] = "Metodo di rendering",
            ["SDL (Default)"] = "SDL (predefinito)",
            ["Aspect ratio"] = "Proporzioni",
            ["Speed"] = "Velocità",
            ["Language"] = "Lingua",
            ["Resume where you left off"] = "Riprendi da dove avevi lasciato",
            ["Saves the game state when you quit and reloads it at the next start."] =
                "Salva lo stato del gioco quando esci e lo ricarica al prossimo avvio.",
            ["Frequency"] = "Frequenza",
            ["Buffer"] = "Buffer",
            ["Channels"] = "Canali",
            ["Folder"] = "Cartella",
            ["MSU volume"] = "Volume MSU",
            ["Default settings"] = "Impostazioni predefinite",
            ["Game files"] = "File del gioco",
            ["Rebuild"] = "Ricompila",
            ["List on the wiki"] = "Elenco su wiki",
            ["What the fixes change, on the Zelda3 HD wiki."] = "Cosa cambiano le correzioni, sul wiki di Zelda3 HD.",
            ["Shaders you can download"] = "Shader che puoi scaricare",
            ["Dutch"] = "Olandese",
            ["English (EU)"] = "Inglese (EU)",
            ["English (Redux)"] = "Inglese (Redux)",
            ["English (US)"] = "Inglese (US)",
            ["French (CA)"] = "Francese (CA)",
            ["French (EU)"] = "Francese (EU)",
            ["German"] = "Tedesco",
            ["Polish"] = "Polacco",
            ["Portuguese"] = "Portoghese",
            ["Spanish"] = "Spagnolo",
            ["Swedish"] = "Svedese",
            ["Windowed: the game plays within a window at your aspect ratio and window scaling. Borderless: fullscreen without changing the screen mode. Fullscreen: exclusive fullscreen mode."] =
                "Finestra: il gioco sta in una finestra con le proporzioni e la scala scelte. Senza bordi: schermo intero senza cambiare la modalità dello schermo. Esclusivo: schermo intero esclusivo.",
            ["Auto resizes the window to match the selected aspect ratio; Custom uses the width and height below."] =
                "Automatica adatta la finestra alle proporzioni scelte; Personalizzata usa larghezza e altezza qui sotto.",
            ["Window size in pixels when Window Size is Custom."] = "Dimensione della finestra in pixel quando è Personalizzata.",
            ["Size of the game window when not fullscreen: 1 = 100%, 2 = 200%, 3 = 300%..."] =
                "Grandezza della finestra del gioco quando non è a schermo intero: 1 = 100%, 2 = 200%, 3 = 300%...",
            ["Rendering software used. SDL-software rendering may give better performance on Raspberry Pi. Shaders need OpenGL: choosing SDL turns them off."] =
                "Il sistema di rendering usato. SDL-Software può andare meglio su Raspberry Pi. Gli shader richiedono OpenGL: scegliendo SDL si spengono.",
            ["Language of the dialogue. Other languages need a ROM of that version; the launcher is in Italian when the dialogue is."] =
                "Lingua dei dialoghi. Le altre lingue richiedono una ROM di quella versione; il launcher è in italiano quando lo sono i dialoghi.",
            ["Mono: uniform audio output across all speakers. Stereo: outputs audio to the left or right speakers based where the sound is generated on screen."] =
                "Mono: lo stesso suono da tutti gli altoparlanti. Stereo: il suono esce a sinistra o a destra secondo dove nasce sullo schermo.",
            ["Enable MSU support for audio.\n\nSupports MSU or MSU Deluxe in PCM or OPUZ format."] =
                "Attiva la musica MSU.\n\nSupporta MSU e MSU Deluxe in formato PCM o OPUZ.",
            ["Reset all settings to their default values. The keys are reset in Controls."] =
                "Riporta tutte le impostazioni ai valori predefiniti. I tasti si ripristinano in Controlli.",
            ["Downloads the latest version, puts back the game files changed by hand (it asks first) and rebuilds zelda3.exe. Settings and saves are kept."] =
                "Scarica l'ultima versione, rimette i file del gioco modificati a mano (prima chiede) e ricompila zelda3.exe. Impostazioni e salvataggi restano.",
            ["The game will be downloaded again if there is a newer version, the game files changed by hand will be put back and zelda3.exe will be rebuilt.\n\nSettings and saves are kept; the changes made here are saved first.\n\nDo you want to continue?"] =
                "Se c'è una versione più recente il gioco viene scaricato di nuovo, i file del gioco modificati a mano vengono rimessi com'erano e zelda3.exe viene ricompilato.\n\nImpostazioni e salvataggi restano; le modifiche fatte qui vengono salvate prima.\n\nVuoi continuare?",

            // Titoli dei messaggi
            ["No ROM"] = "Nessuna ROM",
            ["Launcher update"] = "Aggiornamento del launcher",
            ["Local changes"] = "Modifiche locali",
            ["Invalid Input"] = "Tasto non valido",
            ["Save Changes"] = "Salva modifiche",
            ["Confirmation"] = "Conferma",
            ["Invalid ROM Hash"] = "Hash della ROM non valido",
            ["No Connection"] = "Nessuna connessione",
            ["Corrupted Settings"] = "Impostazioni danneggiate",
            ["Italian"] = "Italiano",
            ["Symlink Creation Failed"] = "Collegamento simbolico non creato",
            ["Invalid File Type"] = "Tipo di file non valido",
            ["No MSU Files Found"] = "Nessun file MSU trovato",
        };

        // Frasi dei messaggi composti (le piu' lunghe per prime)
        private static readonly (string, string)[] kSentences = new (string, string)[]
        {
            ("Error occurred while launching Zelda 3.", "Errore durante l'avvio di Zelda 3."),
            ("Error occurred while downloding pip.", "Errore durante il download di pip."),
            ("Error occurred while installing/updating dependencies.", "Errore durante l'installazione delle dipendenze."),
            ("Error occurred while extracting resources.", "Errore durante l'estrazione delle risorse."),
            ("Error occurred while building executable.", "Errore durante la compilazione dell'eseguibile."),
            ("Error occurred while creating the Italian font.", "Errore durante la creazione del font italiano."),
            ("Error occurred while generating language files.", "Errore durante la creazione dei file della lingua."),
            ("Error occurred while extracting dialog.", "Errore durante l'estrazione dei dialoghi."),
            ("Please refer to ", "Consulta "),
            (" for further details.", " per i dettagli."),
            ("No ROM provided. Process cancelled.", "Nessuna ROM indicata. Operazione annullata."),
            ("The zelda3 folder contains local changes to the source files.", "La cartella zelda3 contiene modifiche locali ai sorgenti."),
            (" will reset the source files to the original repository and these changes will be lost.",
                " riporterà i sorgenti allo stato del repository originale e queste modifiche andranno perse."),
            ("Do you want to continue?", "Vuoi continuare?"),
            ("The Shift keys are not recognized by zelda3.exe and thus cannot be used.", "I tasti Shift non sono riconosciuti da zelda3.exe e non si possono usare."),
            ("The Control keys are not recognized by zelda3.exe and thus cannot be used.", "I tasti Ctrl non sono riconosciuti da zelda3.exe e non si possono usare."),
            ("The Alt keys are not recognized by zelda3.exe and thus cannot be used.", "I tasti Alt non sono riconosciuti da zelda3.exe e non si possono usare."),
            ("The function keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.",
                "I tasti funzione servono ai replay e ai salvataggi rapidi di zelda3.exe e non si possono usare per altro."),
            ("The number keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.",
                "I tasti numerici servono ai replay e ai salvataggi rapidi di zelda3.exe e non si possono usare per altro."),
            (" key is utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.",
                " serve ai replay e ai salvataggi rapidi di zelda3.exe e non si può usare per altro."),
            ("Do you want to save your changes to ", "Vuoi salvare le modifiche alle impostazioni "),
            (" settings?", "?"),
            ("The changes that you have made will not be saved.", "Le modifiche fatte non verranno salvate."),
            ("Do you wish to continue?", "Vuoi continuare?"),
            ("ROM hash is not valid for the ", "L'hash della ROM non è valido per la versione "),
            (" version.", "."),
            ("The hash of the file selected is ", "L'hash del file scelto è "),
            ("The correct hash is ", "L'hash corretto è "),
            ("Would you like to select another?", "Vuoi sceglierne un'altra?"),
            ("Unable to connect to the internet.", "Impossibile collegarsi a internet."),
            ("Please ensure you have a stable internet connection before updating your repository.", "Controlla la connessione prima di aggiornare il repository."),
            ("Please ensure you have a stable internet connection before downloading TCC files.", "Controlla la connessione prima di scaricare TCC."),
            ("INI file is corrupted and preventing the settings menu from loading.", "Il file INI è danneggiato e impedisce di aprire le impostazioni."),
            ("Do you want to restore to default settings?", "Vuoi ripristinare le impostazioni predefinite?"),
            ("INI backup file is corrupted and preventing the settings menu from loading.", "La copia di sicurezza del file INI è danneggiata e impedisce di aprire le impostazioni."),
            ("Do you want to download a clean copy from the zelda3 repository?", "Vuoi scaricare una copia pulita dal repository zelda3?"),
            ("The Italian translation (assets\\dialogue_it.txt) was not found.", "La traduzione italiana (assets\\dialogue_it.txt) non è stata trovata."),
            ("A ROM of the ", "Serve una ROM della versione "),
            (" version is required to extract the dialogue and unique font.", " per estrarre i dialoghi e il font."),
            ("Would you like to select one now?", "Vuoi sceglierla ora?"),
            ("This will reset the INI file immediately and cannot be reversed.", "Il file INI verrà ripristinato subito e non si potrà tornare indietro."),
            ("Your INI backup file is missing and you are unable to connect to the internet to download a fresh copy.",
                "Manca la copia di sicurezza del file INI e non è possibile scaricarne una nuova da internet."),
            ("Please ensure you have a stable internet connection before attempting to reset settings.", "Controlla la connessione prima di ripristinare le impostazioni."),
            ("Unable to find the default zelda3.ini and unable to connect to the internet to download it.",
                "Il file zelda3.ini predefinito non si trova e non è possibile scaricarlo da internet."),
            ("Unable to update the game:", "Impossibile aggiornare il gioco:"),
            ("Unable to download the game:", "Impossibile scaricare il gioco:"),
            ("A symlink could not be created.", "Non è stato possibile creare il collegamento simbolico."),
            ("It could be due to a lack of privileges, so running as administrator could allow for symlink creation. ",
                "Potrebbero mancare i permessi: avviando il launcher come amministratore il collegamento potrebbe riuscire. "),
            ("If you are already running the launcher as an administrator, your problem is likely filesystem related and cannot be avoided.",
                "Se il launcher è già avviato come amministratore, il problema dipende probabilmente dal file system e non si può evitare."),
            ("At this point, you can copy the files instead of creating a symlink, but this will take up more space on your drive. ",
                "Puoi copiare i file invece di creare il collegamento, ma occuperanno più spazio sul disco. "),
            ("Alternatively, you can choose to not copy the files over and the MSU directory setting will not be saved at which point you can restart the launcher as administrator and attempt again.",
                "Oppure puoi non copiarli: la cartella MSU non verrà salvata e potrai riavviare il launcher come amministratore e riprovare."),
            ("Would you like to copy the files instead?", "Vuoi copiare i file?"),
            ("Selected file must be of the .zspr format.", "Il file scelto deve essere in formato .zspr."),
            ("Selected file must be of the .glsl or .glslp formats.", "Il file scelto deve essere in formato .glsl o .glslp."),
            ("No MSU files were found in the selected directory. Please verify the correct folder was selected and that the proper .pcm or .opuz files are available.",
                "Nella cartella scelta non ci sono file MSU. Controlla di aver scelto la cartella giusta e che contenga i file .pcm o .opuz."),
            ("Shift+F1 - F10: Saves savestate/replay", "Shift+F1 - F10: salva salvataggio rapido/replay"),
            ("F1 - F10: Load savestates/replays", "F1 - F10: carica salvataggi rapidi/replay"),
            ("Ctrl: When held while loading a savestate, the game replays itself up to point of the savestate rather than immediately load the savestate.",
                "Ctrl: tenuto premuto mentre carichi un salvataggio rapido, il gioco ripercorre il replay fino a quel punto invece di caricarlo subito."),
            ("A new version of the launcher is available (", "È disponibile una nuova versione del launcher ("),
            (", you have ", ", tu hai la "),
            ("Do you want to open the download page?", "Vuoi aprire la pagina per scaricarla?"),
            ("Extracting ", "Estrazione di "),
            (" to ", " in "),
        }.OrderByDescending(p => p.Item1.Length).ToArray();
    }
}
