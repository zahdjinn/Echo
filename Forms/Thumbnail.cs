﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Echo
{
    internal partial class Thumbnail : UserControl
    {
        private MainForm MainForm;
        private Client Client;
        private IntPtr ThumbnailHandle;

        internal Thumbnail(MainForm mainForm, Client client)
        {
            MainForm = mainForm;
            Client = client;
            InitializeComponent();
        }
        internal bool Create()
        {
            //attempt to register this darkages process to a thumbnail, 0 for success
            if (NativeMethods.DwmRegisterThumbnail(MainForm.Handle, Client.MainWindowHandle, out ThumbnailHandle) == 0)
            {
                //create a new thumbnail properties struct and set properties/location/size/etc
                ThumbnailProperties tProperties = new ThumbnailProperties
                {
                    Visible = true,
                    Flags = ThumbnailFlags.Visible | ThumbnailFlags.RectDestination | ThumbnailFlags.Opacity | ThumbnailFlags.SourceClientAreaOnly,
                    Opacity = 255,
                    OnlyClientRect = true
                };

                //now determine the location of the thumbnail
                //first we convert this usercontrol's rect to a screen rect
                Rectangle screenRect = RectangleToScreen(DisplayRectangle);
                //clientrect will represent the mainform co-ordinates of this form
                Rectangle clientRect = MainForm.RectangleToClient(screenRect);

                tProperties.DestinationRect = new Rect(clientRect.Left, clientRect.Top + 24, clientRect.Left + clientRect.Width, clientRect.Top + clientRect.Height);

                //update the thumbnail
                NativeMethods.DwmUpdateThumbnailProperties(ThumbnailHandle, ref tProperties);
                return true;
            }
            else
                return false;
        }
        internal void Renew()
        {
            NativeMethods.DwmUnregisterThumbnail(ThumbnailHandle);
            Create();
        }
        internal void Destroy(bool refresh = true)
        {
            //set thumbnail to invisible incase its not dead yet
            ThumbnailProperties tProps = new ThumbnailProperties();
            tProps.Visible = false;
            tProps.Flags = ThumbnailFlags.Visible;

            //unregister the thumbnail
            NativeMethods.DwmUnregisterThumbnail(ThumbnailHandle);
            NativeMethods.DwmUpdateThumbnailProperties(ThumbnailHandle, ref tProps);

            Hide();
        }

        #region Handlers
        private void toggleHide_Click(object sender, EventArgs e)
        {
            Client.Resize(0, 0, true);
        }
        private void small_Click(object sender, EventArgs e)
        {
            Client.Resize(640, 480);
        }
        private void large_Click(object sender, EventArgs e)
        {
            Client.Resize(1280, 960);
        }
        private void large4k_Click(object sender, EventArgs e)
        {
            Client.Resize(2560, 1920);
        }
        private void fullscreen_Click(object sender, EventArgs e)
        {
            Client.Resize(0, 0, false, true);
        }
        private void exitBtn_Click(object sender, EventArgs e)
        {
            Client.Process.EnableRaisingEvents = false;
            Client.Destroy();
        }
        private void Thumbnail_Click(object sender, EventArgs e)
        {
            //if it's hidden, unhide it
            if (!NativeMethods.IsWindowVisible(Client.MainWindowHandle))
                Client.Resize(0, 0, true);
            //if it's fullscreen, restore it to it's current position and size
            else if (Client.State.HasFlag(ClientState.Fullscreen))
                NativeMethods.ShowWindowAsync(Client.MainWindowHandle, ShowWindowFlags.ActiveShow);
            //otherwise, restore it to it's last known position and size
            else
                NativeMethods.ShowWindowAsync(Client.MainWindowHandle, ShowWindowFlags.ActiveNormal);

            //wait for window to appear
            while (Client.MainWindowHandle == IntPtr.Zero)
                Thread.Sleep(10);

            //set the window as the foreground window
            NativeMethods.SetForegroundWindow((int)Client.MainWindowHandle);
        }
        #endregion
    }
}
