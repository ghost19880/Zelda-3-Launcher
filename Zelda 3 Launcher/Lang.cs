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

        // Legge la lingua da zelda3.ini
        public static void Load()
        {
            Italian = false;
            try
            {
                var iniFile = IniFile.UserIni;
                if (!File.Exists(iniFile)) return;
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
            ["Advanced Item Selection"] = "Selezione oggetti avanzata",
            ["Limit L/R Switch to First 4 Items"] = "L/R solo sui primi 4 oggetti",
            ["Allow Turning While Dashing"] = "Svolta durante la corsa",
            ["Mirror Teleports to Dark World"] = "Specchio verso il Mondo delle Tenebre",
            ["Collect Items w/ Sword"] = "Raccogli oggetti con la spada",
            ["Break Pots w/ Sword"] = "Rompi vasi con la spada",
            ["Disable Low Heart Beep"] = "Niente bip a pochi cuori",
            ["Skip Intro w/ Keypress"] = "Salta intro con un tasto",
            ["Indicate Max Resources"] = "Evidenzia risorse al massimo",
            ["More Active Bombs"] = "Più bombe attive",
            ["Larger Wallet"] = "Borsa più grande",
            ["Miscellaneous Minor Fixes"] = "Correzioni minori varie",
            ["Game Changing Bug Fixes"] = "Correzioni maggiori",
            ["Cancel Bird"] = "Annulla viaggio uccello",
            ["Bird travel point 9"] = "Punto di volo 9",
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
            ["Dim Flashing"] = "Meno lampi",
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
            ["Allows you to turn when dashing"] = "Puoi cambiare direzione mentre corri con i Calzari di Pegaso",
            ["Magic Mirror works in both directions"] = "Lo Specchio Magico funziona in entrambe le direzioni",
            ["You can collect items with your sword from a distance"] = "Puoi raccogliere oggetti a distanza con la spada",
            ["You can break pots if you have a L2 or higher sword"] = "Puoi rompere i vasi se hai una spada di livello 2 o superiore",
            ["Disables the beeping when you are low health"] = "Disattiva il bip quando hai pochi cuori",
            ["Allows you to skip the intro by pressing a key"] = "Puoi saltare l'introduzione premendo un tasto",
            ["Values will appear as yellow/orange when you have max resources (rupees, arrows, bombs)"] =
                "I valori diventano gialli/arancioni quando rupie, frecce o bombe sono al massimo",
            ["Can have 10 active bombs instead of 2"] = "Puoi avere 10 bombe attive invece di 2",
            ["Can carry 9999 rupees"] = "Puoi portare 9999 rupie",
            ["Miscellaneous bug fixes"] = "Correzioni di errori varie",
            ["Some bug fixes that are significant enough to impact game behavior"] = "Correzioni di errori abbastanza importanti da cambiare il comportamento del gioco",
            ["Allows you to cancel bird travel by hitting the X button"] = "Puoi annullare il viaggio con l'uccello premendo X",
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
            ["Enables custom ZSPR Link sprites"] = "Attiva gli sprite ZSPR personalizzati di Link",
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

            // Migliorie HD
            ["You can also change these while playing, from the options menu (Esc or right stick click)."] =
                "Si possono cambiare anche durante il gioco dal menu opzioni (Esc oppure clic della levetta destra).",
            ["Effects"] = "Effetti",
            ["Smooth camera"] = "Telecamera fluida",
            ["Off"] = "Spenta",
            ["Light"] = "Leggera",
            ["Medium"] = "Media",
            ["Strong"] = "Forte",
            ["Follows Link with a slight delay instead of staying locked to him."] = "Segue Link con un leggero ritardo invece di restare agganciata.",
            ["Sharp dialogue text"] = "Testo nitido nei dialoghi",
            ["Redraws the dialogue box on top of the shader so the letters stay sharp."] = "Ridisegna il riquadro dei dialoghi sopra lo shader, così le lettere restano nitide.",
            ["Smoothness above 60 Hz"] = "Fluidità oltre i 60 Hz",
            ["On displays above 60 Hz, scrolling is interpolated in the extra frames. Nothing changes at 60 Hz."] =
                "Sugli schermi a più di 60 Hz interpola lo scorrimento nei frame in più. A 60 Hz non cambia niente.",
            ["Dynamic lighting"] = "Illuminazione dinamica",
            ["Glow around torches, fire and lava; dark rooms are lit only by burning flames."] =
                "Aloni attorno a torce, fuoco e lava; le stanze buie sono illuminate solo dalle fiamme accese.",
            ["Embers from flames"] = "Braci dalle fiamme",
            ["Sparks rising from torches, braziers, fire and lava."] = "Scintille che salgono da torce, bracieri, fuoco e lava.",
            ["Light rays from windows"] = "Raggi dalle finestre",
            ["Light shafts with floating dust in rooms with windows."] = "Fasci di luce con pulviscolo nelle stanze con le finestre.",
            ["Glow on received items"] = "Bagliore degli oggetti ricevuti",
            ["Light rays behind Link when he holds up a newly found item."] = "Raggi luminosi dietro Link quando solleva un oggetto appena trovato.",
            ["Heat haze over lava"] = "Calore sopra la lava",
            ["The air shimmers slightly above lava."] = "L'aria tremola leggermente sopra la lava.",
            ["Improved rain and lightning"] = "Pioggia e lampi migliorati",
            ["Denser rain with splashes; a single soft flash instead of the quick flickers."] =
                "Pioggia più fitta con spruzzi; un solo lampo morbido al posto dei lampeggi rapidi.",
            ["Blur behind menus"] = "Sfocatura dietro menu",
            ["The game is blurred and darkened behind the inventory and menus."] =
                "Il gioco si sfoca e si scurisce dietro l'inventario e i menu.",
            ["Shadows of characters and enemies"] = "Ombre di personaggi e nemici",
            ["Soft shadows under Link and enemies walking on the ground."] = "Ombre morbide sotto Link e i nemici che camminano a terra.",
            ["Soft transition circle"] = "Cerchio di transizione morbido",
            ["Soft edge on the circle transition when entering and leaving places."] =
                "Bordo sfumato nel cerchio di transizione quando si entra e si esce dai luoghi.",
            ["Button hints"] = "Suggerimenti dei tasti",
            ["On-screen hints for the buttons in the inventory and on the maps."] =
                "Suggerimenti a schermo dei tasti nell'inventario e sulle mappe.",
            ["Dialogue speed"] = "Velocità dei dialoghi",
            ["Original"] = "Originale",
            ["Fast holding A or B"] = "Veloce tenendo A o B",
            ["Instant"] = "Istantanea",
            ["How fast the dialogue text appears."] = "Quanto velocemente compare il testo dei dialoghi.",
            ["Fast transitions"] = "Transizioni veloci",
            ["Moving between areas and rooms happens at double speed."] = "Il passaggio tra aree e stanze avviene a velocità doppia.",
            ["Continuous area transitions"] = "Transizione continua tra aree",
            ["Between outdoor areas Link keeps walking and the camera eases in and out, without stops."] =
                "Fra le aree esterne Link continua a camminare e la telecamera accelera e frena, senza blocchi.",
            ["Continuous world"] = "Mondo continuo",
            ["Outdoors you see the neighbouring areas beyond the edge, the camera follows Link across it and the world remembers what you changed while you stay outdoors."] =
                "All'aperto si vedono le aree vicine oltre il bordo, la telecamera segue Link oltre il bordo e il mondo ricorda quello che hai cambiato finché resti all'aperto.",
            ["Modern HUD"] = "HUD moderno",
            ["Hearts and magic top left, Y/A/B buttons top right with the item, the sword and what the A button does (Talk, Read, Open...), rupees and keys bottom left."] =
                "Cuori e magia in alto a sinistra, tasti Y/A/B in alto a destra con oggetto, spada e azione del tasto A (Parla, Leggi, Apri...), rupie e chiavi in basso a sinistra.",
            ["X, L, R items in the HUD"] = "Oggetti X, L, R nell'HUD",
            ["Shows the items assigned to X, L and R in the HUD (requires Advanced Item Selection)."] =
                "Mostra nell'HUD gli oggetti assegnati a X, L e R (richiede la selezione oggetti avanzata).",
            ["Dungeon names"] = "Nomi dei dungeon",
            ["When Link enters a dungeon from outside, its name appears in the middle of the screen like on GBA and disappears as soon as he moves."] =
                "Quando Link entra in un dungeon dall'esterno, il nome compare al centro dello schermo come su GBA e sparisce appena si muove.",
            ["Automatic save"] = "Salvataggio automatico",
            ["Silently saves at every area change and when entering or leaving dungeons and houses. Load it from the in-game menu."] =
                "Salva in silenzio a ogni cambio di area e a ogni ingresso o uscita da labirinti e case. Si carica dal menu del gioco.",
            ["Rotating automatic saves"] = "Salvataggi automatici a rotazione",
            ["How many automatic saves to keep (auto0.sav, auto1.sav, ...)."] = "Quanti salvataggi automatici tenere (auto0.sav, auto1.sav, ...).",
            ["Enable all"] = "Attiva tutto",
            ["Four Swords shader"] = "Shader Four Swords",
            ["Painted look in the style of Four Swords Adventures (uses OpenGL). The HUD, text and characters stay sharp."] =
                "Aspetto dipinto in stile Four Swords Adventures (usa OpenGL). HUD, testi e personaggi restano nitidi.",
            ["Disable all"] = "Disattiva tutto",
            ["Only the HD improvements: graphics, effects and gameplay."] = "Solo le migliorie HD: grafica, effetti e gioco.",
            ["Convenience"] = "Comodità",
            ["Sword and items world interaction"] = "Spada e oggetti interazione ambiente",
            ["Burn and freeze interaction"] = "Interazione bruciatura e congelamento",
            ["The world physically reacts to fire and ice."] = "Il mondo reagisce fisicamente a fuoco e ghiaccio.",
            ["Softens the intensity of the flashes, not how many there are (like the Virtual Console releases): the screen turns only slightly white during lightning, magic and boss flashes, the intro sword flash is dimmer and the Ether Medallion flickers more slowly."] =
                "Attenua l'intensità dei lampi, non il loro numero (come nelle versioni Virtual Console): lo schermo diventa solo un po' bianco durante fulmini, magie e lampi dei boss, il lampo della spada nell'introduzione è più tenue e il Medaglione Etere lampeggia più lentamente.",
            ["Swords and items behave like on GBA and interact with the world"] =
                "Spade e oggetti si comportano come su GBA e interagiscono con l'ambiente",
            ["Items and controls"] = "Oggetti e comandi",
            ["Bug fixes"] = "Correzioni",
            ["Allows you to switch items with L/R but also allows you to assign items to X, Y, L, and R.\n\nIf X is assigned an item, select will now open the map. To save and quit, you will need to hit Select while in the item menu.\n\nIf L and/or R are assigned an item, quick item switching will be disabled."] =
                "Puoi cambiare oggetto con L/R e assegnare oggetti a X, Y, L e R.\n\nSe a X è assegnato un oggetto, Select apre la mappa. Per salvare e uscire premi Select nel menu degli oggetti.\n\nSe a L e/o R è assegnato un oggetto, il cambio rapido con L/R si disattiva.",
            ["Unable to save zelda3.ini:\n\n"] = "Impossibile salvare zelda3.ini:\n\n",
            ["Error"] = "Errore",

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
