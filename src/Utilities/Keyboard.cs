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

namespace AimBot.Utilities
{
    public static class Keyboard
    {
        private const uint KeyeventfExtendedkey = 0x0001;
        private const uint KeyeventfKeyup       = 0x0002;

        private const int ActionDelay = 1;

        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void Keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


        public static void KeyDown(Keys key) { Keybd_event((byte) key, 0, KeyeventfExtendedkey, UIntPtr.Zero); }

        public static void KeyUp(Keys key)
        {
            Keybd_event((byte) key, 0, KeyeventfExtendedkey | KeyeventfKeyup, UIntPtr.Zero);
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
    }
}