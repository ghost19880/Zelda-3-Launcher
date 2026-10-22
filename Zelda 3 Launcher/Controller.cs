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

        public void Close()
        {
            // Close the joystick and quit SDL
            SDL.SDL_GameControllerClose(controller);
            SDL.SDL_Quit();
        }
    }

}
