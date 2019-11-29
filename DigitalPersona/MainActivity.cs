using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace DigitalPersona
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        TextView lblStatus;
        Button btnSelect;
        Button btnDetect;
        FingerprintTools fingerprintTools;
        ImageView imgFingerprint;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            lblStatus = FindViewById<TextView>(Resource.Id.lblStatus);
            btnSelect = FindViewById<Button>(Resource.Id.btnSelect);
            btnDetect = FindViewById<Button>(Resource.Id.btnDetect);
            imgFingerprint = FindViewById<ImageView>(Resource.Id.imgFingerprint);

            fingerprintTools = new FingerprintTools();
            fingerprintTools.DevicesDetected += FingerprintTools_DevicesDetected;
            fingerprintTools.FingerprintDetected += FingerprintTools_FingerprintDetected;
            fingerprintTools.Init(this);
            var devices = fingerprintTools.GetReaders();

            if (devices.Count > 0)
            {
                if (devices.Count == 1)
                {
                    lblStatus.Text = "Device selected: " + devices[0];
                    fingerprintTools.InitDevice(devices[0]);
                }
                else {
                    lblStatus.Text = "Please select a device to read";
                    btnSelect.Visibility = ViewStates.Visible;
                    btnDetect.Visibility = ViewStates.Visible;
                }
            }

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;
        }

        private void FingerprintTools_FingerprintDetected(object sender, byte[] e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Bitmap bitmap = BitmapFactory.DecodeByteArray(e, 0, e.Length);
                
                imgFingerprint.SetImageBitmap(bitmap);
            });
        }

        private void FingerprintTools_DevicesDetected(object sender, EventArgs e)
        {
            
        }

        protected override void OnResume()
        {
            base.OnResume();

            //var devices = FingerprintTools.CheckDevices(this);

            //if (devices.Count > 0)
            //{
            //    lblStatus.Text = "Please select a device to read";
            //    btnSelect.Visibility = ViewStates.Visible;
            //    btnDetect.Visibility = ViewStates.Visible;
            //}
            //else
            //{
            //}
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

