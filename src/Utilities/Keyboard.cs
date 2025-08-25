#region Header

//-----------------------------------------------------------------
//   Class:          VirtualKeyboard
//   Description:    Keyboard control utils.
//   Author:         Stridemann, nymann        Date: 08.26.2017
//-----------------------------------------------------------------

#endregion

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace AimBot.Utilities
{
    public static class Keyboard
    {
        // SendInput constants
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MAPVK_VK_TO_VSC = 0x0;

        private const int ActionDelay = 30;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // Legacy API as fallback for certain games
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


        public static void KeyDown(Keys key) { SendKey(key, false); }

        public static void KeyUp(Keys key)
        {
            SendKey(key, true);
        }

        public static void KeyPress(Keys key)
        {

            KeyDown(key);
            Thread.Sleep(ActionDelay);
            KeyUp(key);
        }

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static bool IsKeyDown(int nVirtKey) => GetKeyState(nVirtKey) < 0;

        private static void SendKey(Keys key, bool keyUp)
        {
            // Allow runtime override via environment toggle for quick experimentation
            var useLegacy = Environment.GetEnvironmentVariable("AIMBOT_USE_LEGACY_KEY") == "1";
            if (useLegacy)
            {
                uint legacyFlags = keyUp ? KEYEVENTF_KEYUP : 0;
                keybd_event((byte)key, 0, legacyFlags, UIntPtr.Zero);
                return;
            }

            // Use scancodes; some games/overlays ignore VK-based input
            ushort scan = (ushort)MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
            uint flags = KEYEVENTF_SCANCODE | (keyUp ? KEYEVENTF_KEYUP : 0);

            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0, // use scancode
                        wScan = scan,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var inputs = new[] { input };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}