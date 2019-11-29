using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Digitalpersona.Uareu;
using Com.Digitalpersona.Uareu.Dpfpddusbhost;
using Java.Nio;

namespace DigitalPersona
{
    public class FingerprintTools
    {
        private static String ACTION_USB_PERMISSION = "com.digitalpersona.test.USB_PERMISSION";
        UsbDevice device;
        public static Activity _activity;
        UsbReciever usbReceiver;

        // Events
        public event EventHandler DevicesDetected;
        public event EventHandler<byte[]> FingerprintDetected;

        public void Init(Activity activity)
        {
            try 
            {
                _activity = activity;

                Java.Lang.JavaSystem.LoadLibrary("dpuareu_jni");
                Java.Lang.JavaSystem.LoadLibrary("dpfr6");
                Java.Lang.JavaSystem.LoadLibrary("dpfr7");
                Java.Lang.JavaSystem.LoadLibrary("dpfj");
                Java.Lang.JavaSystem.LoadLibrary("dpfpdd");
                Java.Lang.JavaSystem.LoadLibrary("dpfpdd_4k");
                Java.Lang.JavaSystem.LoadLibrary("dpfpdd_ptapi");
                Java.Lang.JavaSystem.LoadLibrary("dpfpdd5000");
                Java.Lang.JavaSystem.LoadLibrary("dpfpdd7k");
                //Java.Lang.JavaSystem.LoadLibrary("dpuvc");
                Java.Lang.JavaSystem.LoadLibrary("nex_sdk");
                Java.Lang.JavaSystem.LoadLibrary("tfm");

                UsbManager manager = (UsbManager)activity.GetSystemService(Context.UsbService);

                usbReceiver = new UsbReciever(this);
                PendingIntent mPermissionIntent = PendingIntent.GetBroadcast(activity, 0, new Intent(
                    ACTION_USB_PERMISSION), 0);
                IntentFilter filter = new IntentFilter(ACTION_USB_PERMISSION);
                activity.RegisterReceiver(usbReceiver, filter);

                //if (DPFPDDUsbHost.DPFPDDUsbCheckAndRequestPermissions(activity, mPermissionIntent, "Fingerprint reader"))
                //{
                //    CheckDevices();
                //}
            }
            catch (Exception ex)
            { 
            }
        }

        public List<string> CheckDevices()
        {
            var data = new List<string>();

            try
            {
                var readers = UareUGlobal.GetReaderCollection(Android.App.Application.Context);
                readers.GetReaders();

                if (readers.Size() > 1)
                {
                    DevicesDetected?.Invoke(this, null);
                }
                else
                {
                    var reader = readers.Get(0).JavaCast<IReader>();

                    if (reader != null)
                    {
                        InitDevice(reader);
                    }                    
                }
            }
            catch (Exception ex)
            {
            }

            return data;
        }

        public List<string> GetReaders() {
            var data = new List<string>();

            try
            {
                var readers = UareUGlobal.GetReaderCollection(Android.App.Application.Context);
                readers.GetReaders();

                if (readers.Size() > 0)
                {
                    var reader = readers.Get(0).JavaCast<IReader>();

                    if (reader != null)
                    {
                        data.Add(reader.Description.Name);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return data;
        }

        private bool wasInit = false;

        public void InitDevice(string name)
        {
            PendingIntent mPermissionIntent = PendingIntent.GetBroadcast(_activity, 0, new Intent(
                    ACTION_USB_PERMISSION), 0);

            if (DPFPDDUsbHost.DPFPDDUsbCheckAndRequestPermissions(_activity, mPermissionIntent, name))
            {
                CheckDevices();
            }
        }

        public void InitDevice(IReader reader)
        {
            if (reader != null)
            {
                if (wasInit)
                {
                    if (reader.Status.Status == ReaderReaderStatus.Busy)
                    {
                        return;
                    }
                }

                _activity.RunOnUiThread(() =>
                {
                    var readerName = "";

                    if (reader.Description != null)
                        readerName = reader.Description.Name ?? "";

                    var toast = Toast.MakeText(_activity, readerName, ToastLength.Long);
                    toast.Show();
                });

                reader.Open(ReaderPriority.Exclusive);
                wasInit = true;

                var dpi = GetFirstDPI(reader);

                ReadFingerprint(reader, dpi);
            }
        }

        private void ReadFingerprint(IReader reader, int dpi)
        {
            var t = Task.Run(async () =>
            {
                try
                {
                    var m_reset = false;

                    while (!m_reset)
                    {
                        try
                        {
                            var cap_result = reader.Capture(FidFormat.Ansi3812004, ReaderImageProcessing.ImgProcDefault, dpi, -1);

                            if (cap_result == null) continue;

                            if (cap_result.Image != null)
                            {
                                var view = cap_result.Image.GetViews()[0];
                                var result = cap_result.Quality;
                               
                                if (view != null)
                                {
                                    var data = view.GetImageData();

                                    var bitmap = GetBitmapFromRaw(data, view.Width, view.Height);

                                    var stream = new MemoryStream();
                                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 0, stream);
                                    byte[] bitmapData = stream.ToArray();

                                    var img = Convert.ToBase64String(bitmapData);

                                    FingerprintDetected?.Invoke(this, bitmapData);
                                    //var worker = await FindWorker(cap_result.Image);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            });
        }

        private async static Task<string> FindWorker(IFid currentFingerprint)
        {
            //var users = await ksec.access.App.Database.GetItemsAsync<UserInfo>();

            //foreach (var user in users)
            //{
            //    if ((user.Rut ?? "") != "" && (user.Fingerprint1 ?? "") != "")
            //    {
            //        var result = CompareFingers(user.Fingerprint1, currentFingerprint);

            //        if (result)
            //            return user.Rut;
            //    }
            //}

            return "";
        }

        private bool CompareFingers(string baseFingerprint, IFid currentFingerprint)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var engine = UareUGlobal.Engine;

            String[] arr = baseFingerprint.Split('-');
            byte[] bytes = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++) bytes[i] = Convert.ToByte(arr[i], 16);

            // Huella del trabajador
            var frm1 = engine.CreateFmd(bytes,
                currentFingerprint.GetViews()[0].Width,
                currentFingerprint.GetViews()[0].Height,
                508, 1, currentFingerprint.CbeffId, FmdFormat.Ansi3782004);

            // Huella actual
            var frm2 = engine.CreateFmd(currentFingerprint, FmdFormat.Ansi3782004);
           
            var score = engine.Compare(frm1, 0, frm2, 0);

            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);

            _activity.RunOnUiThread(() =>
            {
                var toast = Toast.MakeText(_activity, "Result: " + stopwatch.ElapsedMilliseconds.ToString(), ToastLength.Long);
                toast.Show();
            });

            if ((score) < (0x7FFFFFFF / 100000))
            {
                return true;
            }

            return false;
        }

        private int GetFirstDPI(IReader reader)
        {
            if (reader != null)
            {
                var capabilities = reader.Capabilities;

                if (capabilities != null)
                {
                    if (capabilities.Resolutions != null)
                    {
                        if (capabilities.Resolutions.Count > 0)
                        {
                            return capabilities.Resolutions[0];
                        }
                    }
                }
            }

            return 0;
        }

        private static List<Bitmap> m_cachedBitmaps = new List<Bitmap>();
        private static int m_cacheIndex = 0;
        private static int m_cacheSize = 2;
        private static Bitmap m_lastBitmap;
        private static byte[] previousTest;

        private static Android.Graphics.Bitmap GetBitmapFromRaw(byte[] src, int width, int height)
        {
            try
            {


                byte[] bits = new byte[src.Length * 4];
                int i = 0;
                for (i = 0; i < src.Length; i++)
                {
                    bits[i * 4] = bits[i * 4 + 1] = bits[i * 4 + 2] = (byte)src[i];
                    bits[i * 4 + 3] = 1;
                }

                Bitmap bitmap = null;
                if (m_cachedBitmaps.Count == m_cacheSize)
                {
                    bitmap = m_cachedBitmaps[m_cacheIndex];
                }

                if (bitmap == null)
                {
                    bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
                    m_cachedBitmaps.Add(bitmap);
                }
                else if (bitmap.Width != width || bitmap.Height != height)
                {
                    bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
                    m_cachedBitmaps[m_cacheIndex] = bitmap;
                }
                m_cacheIndex = (m_cacheIndex + 1) % m_cacheSize;

                bitmap.CopyPixelsFromBuffer(ByteBuffer.Wrap(bits));

                // save bitmap to history to be restored when screen orientation changes
                m_lastBitmap = bitmap;
                return bitmap;

            }
            catch (Exception ex)
            { }

            return null;
        }

        public static Bitmap toGrayscale(Bitmap bmpOriginal)
        {
            int width, height;
            height = bmpOriginal.Height;
            width = bmpOriginal.Width;

            Bitmap bmpGrayscale = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            Canvas c = new Canvas(bmpGrayscale);
            Paint paint = new Paint();
            ColorMatrix cm = new ColorMatrix();
            cm.SetSaturation(0);
            ColorMatrixColorFilter f = new ColorMatrixColorFilter(cm);
            paint.SetColorFilter(f);
            c.DrawBitmap(bmpOriginal, 0, 0, paint);
            return bmpGrayscale;
        }
    }

    public class UsbReciever : BroadcastReceiver
    {
        FingerprintTools activity;

        public UsbReciever(FingerprintTools tools)
        {
            activity = tools;
        }

        private static String ACTION_USB_PERMISSION = "com.digitalpersona.test.USB_PERMISSION";

        public override void OnReceive(Context context, Intent intent)
        {
            String action = intent.Action;
            if (ACTION_USB_PERMISSION.Equals(action))
            {
                lock (this)
                {
                    UsbDevice device = (UsbDevice)intent
                            .GetParcelableExtra(UsbManager.ExtraDevice);

                    if (intent.GetBooleanExtra(
                            UsbManager.ExtraPermissionGranted, false))
                    {
                        if (device != null)
                        {
                            activity.CheckDevices();
                        }
                    }
                    else
                    {

                    }
                }
            }
        }
    }
}