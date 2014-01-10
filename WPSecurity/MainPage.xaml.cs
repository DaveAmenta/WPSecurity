using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace WPSecurity
{
    public partial class MainPage : PhoneApplicationPage
    {
        Microsoft.Devices.PhotoCamera _Camera;

        public MainPage()
        {
            InitializeComponent();
        }

        public void DoFrame()
        {
            Debug.WriteLine("DoFrame");

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    int height = (int)_Camera.PreviewResolution.Height;
                    int width = (int)_Camera.PreviewResolution.Width;

                    var buf = new int[width * height];
                    _Camera.GetPreviewBufferArgb32(buf);

                    var wb = new WriteableBitmap(width, height);
                    Array.Copy(buf, wb.Pixels, buf.Length);
                    buf = null;

                    byte[] data = null;
                    using (var ms = new MemoryStream())
                    {
                        wb.SaveJpeg(ms, width, height, 0, 50);
                        data = ms.ToArray();
                    }

                    //using (
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); 
                    //)
                    {
                        socket.SendBufferSize = 65507;
                        
                        SocketAsyncEventArgs a = new SocketAsyncEventArgs();
                        a.RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 11000);
                        a.SetBuffer(data, 0, data.Length);
                        a.Completed += (s, e) =>
                            {
                                try
                                {
                                    Debug.WriteLine(e.SocketError);
                                    ((Socket)s).Dispose();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex); // can't do anything
                                }
                                DoFrame();
                            };

                        bool cleanup = true;
                        if (data.Length > socket.SendBufferSize)
                        {
                            Debug.WriteLine("Aborting frame due to size");
                        }
                        else
                        {
                            Debug.WriteLine("Pack Send: " + data.Length);
                            cleanup = !socket.ConnectAsync(a);
                        }

                        if (cleanup)
                        {
                            socket.Dispose();
                            DoFrame();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(1000);
                    DoFrame();
                }
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Disable sleep
            PhoneApplicationService.Current.UserIdleDetectionMode = 
                IdleDetectionMode.Disabled;
            
            _Camera = new PhotoCamera(CameraType.Primary);
            _Camera.Initialized += (_, __) => DoFrame();
            vb.SetSource(_Camera);
        }

        private void LayoutRoot_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.DeltaManipulation.Scale.X > 0.0 && e.DeltaManipulation.Scale.Y > 0.0)
            {
                Func<double, double> EnforceLimits = (s) =>
                {
                    if (s < 1.0)
                        s = 1.0;
                    else if (s > 4.0)
                        s = 4.0;
                    return s;
                };

                vbImageTransform.ScaleX = EnforceLimits(vbImageTransform.ScaleX * e.DeltaManipulation.Scale.X);
                vbImageTransform.ScaleY = EnforceLimits(vbImageTransform.ScaleY * e.DeltaManipulation.Scale.Y);
            }
        }
    }
}