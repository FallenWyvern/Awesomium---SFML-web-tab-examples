using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SFML.Graphics;
using SFML.Window;
using Awesomium.Core;
using System.Threading.Tasks;
using System.Threading;

namespace sfml_web
{
    public static class BrowserManager
    {
        public static List<BrowserTab> Tabs = new List<BrowserTab>();   // List of all tabs.
        public static int CurrentTab = 0;                               // Current tabe we're on.
        public static bool Running = false;                             // Check to determine if we're in use.
        public static SynchronizationContext awesomiumContext = null;   // Used to synchronize multiple tabs.

        /// <summary>
        /// Starts the Browser Manager on another thread. Without this, no web services are available.
        /// </summary>
        public static void StartBrowserManagerService()
        {
            if (!BrowserManager.Running)
            {
                new System.Threading.Thread(() =>
                {
                    Awesomium.Core.WebCore.Started += (s, e) =>
                    {
                        BrowserManager.awesomiumContext = System.Threading.SynchronizationContext.Current;
                        Console.WriteLine("Starting Synchronization Context for Browser");
                    };
                    BrowserManager.Start();
                }).Start();
            }
        }

        /// <summary>
        /// Ends the Browser Manager. Use this when closing your application.
        /// </summary>
        public static void EndBrowserManagerService()
        {
            if (BrowserManager.Running)                         // Stops the browser manager.
            {
                BrowserManager.Close();
            }

            BrowserManager.awesomiumContext = null;             // Release the sync context.            
        }

        /// <summary>
        /// Used internally to start the service.
        /// </summary>
        private static void Start()
        {
            Console.WriteLine("Starting WebCore Engine");
            Running = true;
            WebCore.Run();
        }

        /// <summary>
        /// Creates a tab at X/Y location, a w/h size and with an INT ID and a string URL
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="url"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void NewTab(int ID, string url, int w, int h, int x, int y, bool clickable = true, bool keyevent = false)
        {
            while (awesomiumContext == null)
            {
                Console.WriteLine("Context sleeping, waiting for context");
                System.Threading.Thread.Sleep(100);
            }

            foreach (BrowserTab b in Tabs)
            {
                if (b.ID == ID)
                {
                    Console.WriteLine("Tab already exists on this ID:" + ID);
                    return;
                }
            }

            awesomiumContext.Post(state =>
            {
                Console.WriteLine("Creating tab for " + url);
                BrowserTab t = new BrowserTab(ID, url, w, h, x, y);
                t.Clickable = clickable;
                t.KeyEvents = keyevent;
                Tabs.Add(t);
            }, null);
        }

        /// <summary>
        /// Cleans all tabs. Use this if you want to destroy every existing tab.
        /// </summary>
        public static void Clean()
        {
            Console.WriteLine("Cleaning tabs... Count: " + Tabs.Count);

            while (Tabs.Count > 0)
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    awesomiumContext.Send(state =>
                    {
                        Tabs[i].Dispose();
                    }, null);

                    Tabs.Remove(Tabs[i]);
                }
            }

            Console.WriteLine("Tabs should be clean. Count: " + Tabs.Count);
        }

        /// <summary>
        /// Returns a specific tab.
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static BrowserTab FindTabByID(int ID)
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.ID == ID)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a tab that's been clicked
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BrowserTab FindTabByClick(int x, int y)
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.MouseOver(x, y))
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Clicks at a specific tab at a specific location.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void MouseMove(int x, int y)
        {
            if (Mouse.IsButtonPressed(Mouse.Button.Right)) { return; }
            foreach (BrowserTab t in Tabs)
            {
                if (t.MouseOver(x, y) && t.Clickable)
                {
                    awesomiumContext.Send(state =>
                    {
                        t.MyTab.InjectMouseMove((int)(x - t.View.Position.X), (int)(y - t.View.Position.Y));
                    }, null);
                }
            }
        }

        /// <summary>
        /// Release mouse on tabs.
        /// </summary>
        public static void MouseUp()
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.Clickable)
                {
                    awesomiumContext.Send(state =>
                    {
                        t.MyTab.InjectMouseUp(MouseButton.Left);
                    }, null);
                }
                if (t.mouseDrag)
                {
                    t.mouseDrag = false;                    
                }
            }
        }

        /// <summary>
        /// Inject MouseDown at x/y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void MouseDown(int x, int y, SFML.Window.Mouse.Button b)
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.MouseOver(x, y) && t.Clickable && b == Mouse.Button.Left)
                {
                    awesomiumContext.Send(state =>
                    {
                        t.MyTab.InjectMouseDown(MouseButton.Left);
                    }, null);
                }                
            }
            
            Tabs.Reverse();
            foreach (BrowserTab t in Tabs)
            {
                if (t.MouseOver(x, y) && Mouse.IsButtonPressed(Mouse.Button.Right))
                {
                    Tabs.Remove(t);
                    Tabs.Add(t);
                    t.mouseDrag = true;
                    t.dragOffsetX = (uint)(x - t.View.Position.X);
                    t.dragOffsetY = (uint)(y - t.View.Position.Y);
                    return;
                }
            }
            Tabs.Reverse();
        }

        /// <summary>
        /// Allows keys to be passed to the view that the mouse is over.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void InjectKey(Keyboard.Key k, int x, int y)
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.MouseOver(x, y) && t.KeyEvents)
                {
                    awesomiumContext.Send(state =>
                    {
                        WebKeyboardEvent keyboardEvent = new WebKeyboardEvent();
                        keyboardEvent.Text = k.ToString();
                        keyboardEvent.Type = WebKeyboardEventType.Char;
                        t.MyTab.InjectKeyboardEvent(keyboardEvent);                        
                    }, null);
                }
            }
        }

        /// <summary>
        /// Used to destroy a tab.
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static bool DestroyTab(int ID)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].ID == ID)
                {
                    awesomiumContext.Send(state =>
                    {
                        Tabs[i].Dispose();
                    }, null);

                    Tabs.Remove(Tabs[i]);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shuts down the WebManager.
        /// </summary>
        private static void Close()
        {
            Console.WriteLine("Shutting Down WebCore Engine");
            Running = false;
            Clean();
            WebCore.Shutdown();
        }

        /// <summary>
        /// Draws all open tabs.
        /// </summary>
        /// <param name="target"></param>
        public static void Draw(RenderWindow target)
        {
            try
            {

                foreach (BrowserTab tab in Tabs)
                {
                    target.Draw(tab.Draw());
                }
            }
            catch { }
        }
    }

    public class BrowserTab : IDisposable
    {
        public int ID = 0;              // ID of this tab.
        public string url;              // Current URL
        public bool closing = false;    // Used to close tab.
        public WebView MyTab;           // The Awesomeium WebView
        public Sprite View;             // The SFML Sprite that is returned for drawing.
        public bool Clickable = true;   // Is this view clickable.
        public bool KeyEvents = false;  // Is this view typeable.
        public bool mouseDrag = false;  // Is this view being dragged.
        public uint dragOffsetX = 0;    // Where was the mouse when dragging began.
        public uint dragOffsetY = 0;    // ^

        private Texture BrowserTex;     // Texture that copies the Bitmap Surface
        private BitmapSurface s;        // Bitmap surface for the Webview.
        private byte[] webBytes;        // byte array of image data buffer.

        private int browserwidth;       // Width of browesr.
        private int browserheight;      // Height of browser.        

        /// <summary>
        /// Instantiates a new tab.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="URL"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public BrowserTab(int id, string URL, int w, int h, int x, int y)
        {
            ID = id;
            url = URL;

            browserwidth = w;
            browserheight = h;

            s = new BitmapSurface(w, h);
            webBytes = new byte[w * h * 4];

            BrowserTex = new Texture((uint)browserwidth, (uint)browserheight);
            View = new Sprite(BrowserTex);
            View.Position = new Vector2f((uint)x, (uint)y);

            WebSession session = WebCore.CreateWebSession(new WebPreferences()
            {
                WebSecurity = false,
                FileAccessFromFileURL = true,
                UniversalAccessFromFileURL = true,
                LocalStorage = true,
            });

            MyTab = WebCore.CreateWebView(browserwidth, browserheight, session, WebViewType.Offscreen);
            
            //MyTab.ShowJavascriptDialog += (sender, e) =>
            //{
            //    Console.WriteLine(e.Message);
            //    BrowserManager.DestroyTab(ID);
            //    BrowserManager.NewTab(ID, url, w, h, x, y);
            //};

            MyTab.Source = new Uri(URL);

            MyTab.Surface = s;

            s.Updated += (sender, e) =>
            {
                unsafe
                {
                    fixed (Byte* byteptr = webBytes)
                    {
                        s.CopyTo((IntPtr)byteptr, s.RowSpan, 4, true, false);
                        BrowserTex.Update(webBytes);
                    }
                }
            };
        }

        /// <summary>
        /// Returns the surface for drawing.
        /// </summary>
        /// <returns></returns>
        public Sprite Draw()
        {
            if (!closing)
            {
                if (mouseDrag) Move((uint)SFML.Window.Mouse.GetPosition().X - dragOffsetX, (uint)SFML.Window.Mouse.GetPosition().Y - dragOffsetY);
                return View;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Check if point is over this tab.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool MouseOver(int x, int y)
        {
            if (x > View.Position.X && x < View.Position.X + BrowserTex.Size.X)
            {
                if (y > View.Position.Y && y < View.Position.Y + BrowserTex.Size.Y)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Move the Sprite around the screen.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Move(uint x, uint y)
        {
            if (x < 0 || x > 1366) x = 0;
            if (y < 0 || y > 768) y = 0;
            View.Position = new Vector2f(x, y);
        }

        /// <summary>
        /// Used to dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Console.Write("Destroying Browser");
            s.Updated += (sender, e) => { Console.WriteLine("Destroying browser: " + url); };
            MyTab.Stop();
            MyTab.Dispose();
            Console.Write(".");
            closing = true;
            Console.Write(".");
            BrowserTex.Dispose();
            s.Dispose();
            View.Dispose();
            Console.Write(". ");
            Console.WriteLine("Browser Destroyed");
        }
    }
}
