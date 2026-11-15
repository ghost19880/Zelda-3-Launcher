using System;
using SDL2;
using Timer = System.Timers.Timer;

namespace Zelda_3_Launcher
{
    public class MyController
    {
        private IntPtr controller;
        public bool connected = false;

        public MyController()
        {
            // Initialize SDL (i pulsanti arrivano anche senza una finestra di SDL)
            SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
            SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER);

            // Check if any joysticks are connected
            if (SDL.SDL_NumJoysticks() > 0)
            {
                // Open the first joystick
                controller = SDL.SDL_GameControllerOpen(0);
                connected = true;
            }
        }

        internal static string? ConvertButtonID(byte button)
        {
            switch (button)
            {
                case 0:
                    return "A";
                case 1:
                    return "B";
                case 2:
                    return "X";
                case 3:
                    return "Y";
                case 4:
                    return "Back";
                case 5:
                    return "Guide";
                case 6:
                    return "Start";
                case 7:
                    return "L3";
                case 8:
                    return "R3";
                case 9:
                    return "Lb";
                case 10:
                    return "Rb";
                case 11:
                    return "DPadUp";
                case 12:
                    return "DPadDown";
                case 13:
                    return "DPadLeft";
                case 14:
                    return "DPadRight";
                default:
                    return null;
            }
        }

        // Vibrazione di prova con la forza scelta, come quella del menu del gioco
        // (Host_Rumble: solo il motore pesante, leggera 40%, media 70%, forte 100%)
        private static System.Windows.Forms.Timer? rumbleStop;
        public static void TestRumble(int level)
        {
            float[] scale = { 0, 0.4f, 0.7f, 1.0f };
            if (level <= 0 || level > 3) return;
            try
            {
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
                if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 0)
                    SDL.SDL_InitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
                SDL.SDL_GameControllerUpdate();
                var strength = (ushort)(scale[level] * 0xffff);
                for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
                {
                    if (SDL.SDL_IsGameController(i) != SDL.SDL_bool.SDL_TRUE) continue;
                    // restano aperti: chiuderli fermerebbe la vibrazione
                    var gc = SDL.SDL_GameControllerFromInstanceID(SDL.SDL_JoystickGetDeviceInstanceID(i));
                    if (gc == IntPtr.Zero) gc = SDL.SDL_GameControllerOpen(i);
                    if (gc != IntPtr.Zero) SDL.SDL_GameControllerRumble(gc, strength, 0, 500);
                }
                // nel launcher nessuno aggiorna SDL: la fermiamo noi dopo 500 ms
                rumbleStop?.Stop();
                rumbleStop = new System.Windows.Forms.Timer { Interval = 500 };
                rumbleStop.Tick += (s, e) =>
                {
                    ((System.Windows.Forms.Timer)s!).Stop();
                    if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 0) return;
                    for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
                    {
                        var gc = SDL.SDL_GameControllerFromInstanceID(SDL.SDL_JoystickGetDeviceInstanceID(i));
                        if (gc != IntPtr.Zero) SDL.SDL_GameControllerRumble(gc, 0, 0, 0);
                    }
                };
                rumbleStop.Start();
            }
            catch (Exception) { }  // SDL2.dll mancante o controller staccato
        }

        public void Close()
        {
            // Close the joystick and quit SDL
            SDL.SDL_GameControllerClose(controller);
            SDL.SDL_Quit();
        }
    }

}
