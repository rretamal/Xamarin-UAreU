using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace DigitalPersona
{
    [Activity(Label = "EnrollmentActivity")]
    public class EnrollmentActivity : Activity
    {
        FingerprintTools fingerprintTools;
        TextView lblInstructions;
        int capturedFingerprints = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_enrollment);

            // Get controls of layout
            lblInstructions = FindViewById<TextView>(Resource.Id.lblInstructions);

            // Init fingerprint library
            fingerprintTools = new FingerprintTools();
            fingerprintTools.DevicesDetected += FingerprintTools_DevicesDetected;
            fingerprintTools.FingerprintDetected += FingerprintTools_FingerprintDetected;            
        }

        protected override void OnResume()
        {
            base.OnResume();

            fingerprintTools.Init(this);
        }

        private void FingerprintTools_FingerprintDetected(object sender, byte[] e)
        {           
            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            //    Bitmap bitmap = BitmapFactory.DecodeByteArray(e, 0, e.Length);

            //    //imgFingerprint.SetImageBitmap(bitmap);
            //});
        }

        private void FingerprintTools_DevicesDetected(object sender, EventArgs e)
        {
            var readers = fingerprintTools.GetReaders();

            foreach (var reader in readers)
            {
                fingerprintTools.InitDevice(reader, false);
            }
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblInstructions.Text = readers.Count.ToString() + " devices detected!";
            });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}