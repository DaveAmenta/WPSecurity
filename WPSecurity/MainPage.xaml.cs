using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Microsoft.Devices;
using System.Diagnostics;
using System.Threading;
using Microsoft.Phone.Shell;

namespace WPSecurity
{
    public partial class MainPage : PhoneApplicationPage
    {
        Microsoft.Devices.PhotoCamera c;
        public MainPage()
        {
            InitializeComponent();
        }

        public void DoFrame()
        {
            Debug.WriteLine("DoFrame");

            Dispatcher.BeginInvoke(() =>
            {
                int[] buf = new int[(int)c.PreviewResolution.Width * (int)c.PreviewResolution.Height];
                c.GetPreviewBufferArgb32(buf);

                WriteableBitmap wb = new WriteableBitmap((int)c.PreviewResolution.Width, (int)c.PreviewResolution.Height);
                Array.Copy(buf, wb.Pixels, buf.Length);

                MemoryStream ms = new MemoryStream();
                wb.SaveJpeg(ms, (int)c.PreviewResolution.Width, (int)c.PreviewResolution.Height, 0, 50);
                byte[] data = ms.ToArray();

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SendBufferSize = 65507;
                SocketAsyncEventArgs a = new SocketAsyncEventArgs();
                a.RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 11000);
                a.SetBuffer(data, 0, data.Length);
                a.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs ee)
                {
                    Debug.WriteLine(ee.SocketError);
                    ((Socket)s).Dispose();
                    DoFrame();
                });

                if (data.Length > 65507)
                {
                    Debug.WriteLine("Aborting frame due to size");
                }
                Debug.WriteLine("Pack Send: " + data.Length);
                var f = socket.ConnectAsync(a);
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Disable sleep
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

            c = new Microsoft.Devices.PhotoCamera(Microsoft.Devices.CameraType.Primary);
            c.Initialized += (_, __) =>
                {
                    DoFrame();
                };
            vb.SetSource(c);
        }
    }
}