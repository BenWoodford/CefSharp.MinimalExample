﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CefSharp.OffScreen;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.OffScreen
{
    public class Program
    {
        private static ChromiumWebBrowser browser;

        public static void Main(string[] args)
        {
            const string testUrl = "https://www.wufoo.com/html5/attributes/02-autofocus.html";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            // Create the offscreen Chromium browser.
            browser = new ChromiumWebBrowser(testUrl);

            // An event that is fired when the first page is finished loading.
            // This returns to us from another thread.
            browser.LoadingStateChanged += BrowserLoadingStateChanged;

            // We have to wait for something, otherwise the process will exit too soon.
            Console.ReadKey();

            // Clean up Chromium objects.  You need to call this in your application otherwise
            // you will get a crash when closing.
            Cef.Shutdown();
        }

        static void SendKeys()
        {
            KeyEvent[] events = new KeyEvent[] {
                new KeyEvent() { FocusOnEditableField = true, WindowsKeyCode = 82, Modifiers = CefEventFlags.None, Type = KeyEventType.Char, IsSystemKey = false }, // Just the letter R, no shift (so no caps...?)
                new KeyEvent() { FocusOnEditableField = true, WindowsKeyCode = 82, Modifiers = CefEventFlags.ShiftDown, Type = KeyEventType.Char, IsSystemKey = false }, // Capital R?
                new KeyEvent() { FocusOnEditableField = true, WindowsKeyCode = 52, Modifiers = CefEventFlags.None, Type = KeyEventType.Char, IsSystemKey = false }, // Just the number 4
                new KeyEvent() { FocusOnEditableField = true, WindowsKeyCode = 52, Modifiers = CefEventFlags.ShiftDown, Type = KeyEventType.Char, IsSystemKey = false }, // Shift 4 (should be $)
            };

            foreach (KeyEvent ev in events)
            {
                Thread.Sleep(100);
                KeyEvent newEv = ev;
                //newEv.Type = KeyEventType.KeyDown;
                browser.GetBrowser().GetHost().SendKeyEvent(newEv);

                //Thread.Sleep(100);

                //newEv.Type = KeyEventType.KeyUp;
                //browser.GetBrowser().GetHost().SendKeyEvent(newEv);

            }
        }

        private static void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            // Check to see if loading is complete - this event is called twice, one when loading starts
            // second time when it's finished
            // (rather than an iframe within the main frame).
            if (!e.IsLoading)
            {
                // Remove the load event handler, because we only want one snapshot of the initial page.
                browser.LoadingStateChanged -= BrowserLoadingStateChanged;
                browser.GetBrowser().GetHost().ShowDevTools();

                    //Give the browser a little time to render
                    Thread.Sleep(500);

                    // Send keys and wait.
                    SendKeys();

                    // Wait for the screenshot to be taken.
                    var task = browser.ScreenshotAsync(true);
                    task.ContinueWith(x =>
                    {
                        // Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
                        var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                        Console.WriteLine();
                        Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                        // Save the Bitmap to the path.
                        // The image type is auto-detected via the ".png" extension.
                        task.Result.Save(screenshotPath);

                        // We no longer need the Bitmap.
                        // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
                        task.Result.Dispose();

                        Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

                        // Tell Windows to launch the saved image.
                        Process.Start(screenshotPath);

                        Console.WriteLine("Image viewer launched.  Press any key to exit.");
                    }, TaskScheduler.Default);
            }
        }
    }
}
