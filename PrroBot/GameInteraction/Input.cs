using GameOverlay.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PrroBot.GameInteraction
{
    public static class Input
    {
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private const int KEY_DOWN_EVENT = 0x0001;
        private const int KEY_UP_EVENT = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        // import external functions
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);



        // wrap the external functions
        public static Point GetCursorPosition()
        {
            Lifeguard.CheckInteractionPossible();
            POINT lpPoint;
            GetCursorPos(out lpPoint);

            return new Point(lpPoint.X, lpPoint.Y);
        }

        private static void keybd_event(Keys key, int cButtons)
        {
            Lifeguard.CheckInteractionPossible();
            keybd_event((byte)key, 0, cButtons, 0);
        }

        private static void SetCursorPos(float x, float y)
        {
            Lifeguard.CheckInteractionPossible();
            SetCursorPos((int)x, (int)y);
        }

        private static void mouse_event(int dwFlags, Point p)
        {
            Lifeguard.CheckInteractionPossible();
            mouse_event(dwFlags, (int)p.X, (int)p.Y, 0, 0);
        }



        // provide more convenient interface functions for other classes
        public static void SetCursorPos(Point p)
        {
            SetCursorPos(p.X, p.Y);
        }

        public static void RightMouseClick(Point p)
        {
            SetCursorPos(p);
            Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, p);
            Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_RIGHTUP, p);
        }

        public static void LeftMouseClick(Point p)
        {
            SetCursorPos(p);
            Thread.Sleep(200);
            mouse_event(MOUSEEVENTF_LEFTDOWN, p);
            Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_LEFTUP, p);
        }

        public static void LeftMouseClick(float x, float y)
        {
            LeftMouseClick(new Point(x, y));
        }

        public static void LeftMouseDown(Point p)
        {
            SetCursorPos(p);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, p);
            Thread.Sleep(10);
        }

        public static void LeftMouseUp(Point p)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, p);
            Thread.Sleep(10);
        }


        public static void KeyDown(Keys key)
        {
            keybd_event(key, KEY_DOWN_EVENT);
        }

        public static void KeyUp(Keys key)
        {
            keybd_event(key, KEY_UP_EVENT);
        }

        public static void KeyPress(Keys key, int delay = 50)
        {
            KeyDown(key);
            Thread.Sleep(delay);
            KeyUp(key);
        }
    }
}
