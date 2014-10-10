using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SFML.Graphics;
using SFML.Window;
using Awesomium.Core;

namespace sfml_web
{
    class Program
    {
        static public RenderWindow win;

        static void Main(string[] args)
        {
            win = new RenderWindow(new VideoMode(1366, 768), "");
            win.SetFramerateLimit(60);
            BrowserManager.StartBrowserManagerService();

            win.Resized += (sender, e) =>
            {            
            };

            win.Closed += (sender, e) =>
            {
                win.Close();
                BrowserManager.EndBrowserManagerService();
            };

            win.MouseButtonPressed += new EventHandler<MouseButtonEventArgs>(win_MouseButtonPressed);
            win.MouseButtonReleased += new EventHandler<MouseButtonEventArgs>(win_MouseButtonReleased);
            win.MouseMoved += new EventHandler<MouseMoveEventArgs>(win_MouseMoved);
            win.KeyPressed += new EventHandler<KeyEventArgs>(win_KeyPressed);

            BrowserManager.NewTab(0, @"http://www.reddit.com", 300, 400, 0, 0);
            BrowserManager.NewTab(1, @"http://www.youtube.com", 300, 400, 400, 0);    

            while (win.IsOpen())
            {
                win.DispatchEvents();
                win.Clear();
                BrowserManager.Draw(win);
                win.Display();
            }
        }

        static void win_KeyPressed(object sender, KeyEventArgs e)
        {
            BrowserManager.InjectKey(e.Code, win.InternalGetMousePosition().X, win.InternalGetMousePosition().Y);
        }

        static void win_MouseMoved(object sender, MouseMoveEventArgs e)
        {
            try
            {
                BrowserManager.MouseMove(e.X, e.Y);
            }
            catch { }
        }

        static void win_MouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            try
            {
                BrowserManager.MouseUp();
            }
            catch { }
        }

        static void win_MouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            try {
                BrowserManager.MouseDown(e.X, e.Y, e.Button);
                }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }   
}
