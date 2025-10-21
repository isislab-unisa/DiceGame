using BarcodeScanner.Parser;
using BarcodeScanner.Webcam;
using System;
using UnityEngine;
using Wizcorp.Utils.Logger;

#if !UNITY_WEBGL
using System.Threading;
#endif

namespace BarcodeScanner.Scanner
{
    /// <summary>
    /// This Scanner Is used to manage the Camera & the parser and provide:
    /// * Simple methods : Scan / Stop
    /// * Simple events : OnStatus / StatusChanged
    /// </summary>
    public class Scanner : IScanner
    {
        //
        public event EventHandler OnReady;
        public event EventHandler StatusChanged;

        //
        public IWebcam Camera { get; private set; }
        public IParser Parser { get; private set; }
        public ScannerSettings Settings { get; private set; }

        //
        private ScannerStatus status;
        public ScannerStatus Status
        {
            get { return status; }
            private set
            {
                status = value;
                if (StatusChanged != null)
                {
                    StatusChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Store information about last image / results (use the update loop to access camera and callback)
        public Color32[] pixels = null;
        private Action<string, string> Callback;
        private ParserResult Result;

        //
        private bool parserPixelAvailable = false;
        private float mainThreadLastDecode = 0;
        private int webcamFrameDelayed = 0;
        private int webcamLastChecksum = -1;
        private bool decodeInterrupted = true;

        public Scanner() : this(null, null, null) { }
        public Scanner(ScannerSettings settings) : this(settings, null, null) { }
        public Scanner(IParser parser, IWebcam webcam) : this(null, parser, webcam) { }

        public Scanner(ScannerSettings settings, IParser parser, IWebcam webcam)
        {
            // Check Device Authorization
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                throw new Exception("This Webcam Library can't work without the webcam authorization");
            }

            Status = ScannerStatus.Initialize;

            // Default Properties
            Settings = (settings == null) ? new ScannerSettings() : settings;
            Parser = (parser == null) ? new ZXingParser(Settings) : parser;
            //Camera = (webcam == null) ? new UnityWebcam(Settings) : webcam;
        }

        /// <summary>
        /// Used to start Scanning
        /// </summary>
        /// <param name="callback"></param>
        public void Scan(Action<string, string> callback)
        {
            if (Callback != null)
            {
                Debug.Log(this + " Already Scan");
                return;
            }
            Callback = callback;

            Debug.Log(this + " SimpleScanner -> Start Scan");
            Status = ScannerStatus.Running;

#if !UNITY_WEBGL
            if (Settings.ScannerBackgroundThread)
            {
                if (CodeScannerThread != null)
                {
                    Stop(true);
                }

                decodeInterrupted = false;
                CodeScannerThread = new Thread(ThreadDecodeQR);
                Debug.Log("launching thread");
                CodeScannerThread.Start();
            }
#endif
        }

        /// <summary>
        /// Used to Stop Scanning
        /// </summary>
        public void Stop()
        {
            Stop(false);
        }

        /// <summary>
        /// Used to Stop Scanning internaly (can be forced)
        /// </summary>
        private void Stop(bool forced)
        {
            if (!forced && Callback == null)
            {
                Log.Warning(this + " No Scan running");
                return;
            }

            // Stop thread / Clean callback
            Log.Info(this + " SimpleScanner -> Stop Scan");
#if !UNITY_WEBGL
            if (CodeScannerThread != null)
            {
                decodeInterrupted = true;
                CodeScannerThread.Join();
                CodeScannerThread = null;
            }
#endif

            Callback = null;
            Status = ScannerStatus.Paused;
        }

        /// <summary>
        /// Used to be sure that everything is properly clean
        /// </summary>
        public void Destroy()
        {
            // clean events
            OnReady = null;
            StatusChanged = null;

            // Stop it
            Stop(true);

            // clean returns
            Callback = null;
            Result = null;
            pixels = null;
            parserPixelAvailable = false;

            // clean camera
            //Camera.Destroy();
            //Camera = null;
            Parser = null;
        }

        #region Unthread

        /// <summary>
        /// Process Image Decoding in the main Thread
        /// Background Thread : OFF
        /// </summary>
        public void DecodeQR()
        {
            // Wait
            if (Status != ScannerStatus.Running || !parserPixelAvailable /*|| Camera.Width == 0*/)
            {
                return;
            }

            // Process
            Debug.Log(this + " SimpleScanner -> Scan ... " + Screen.width + " / " + Screen.height / 2);
            try
            {
                Result = Parser.Decode(pixels, Settings.width, Settings.height);
                parserPixelAvailable = false;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion

        #region Background Thread

#if !UNITY_WEBGL
        private Thread CodeScannerThread;

        /// <summary>
        /// Process Image Decoding in a Background Thread
        /// Background Thread : OFF
        /// </summary>
        public void ThreadDecodeQR()
        {
            Debug.Log("thread started");
            while (!decodeInterrupted && Result == null)
            {
                // Wait
                if (Status != ScannerStatus.Running || !parserPixelAvailable /*|| Camera.Width == 0*/)
                {
                    //Debug.LogFormat("wait - Status: {0}; parserPixelAvailable: {1}", Status, parserPixelAvailable);
                    Thread.Sleep(Mathf.FloorToInt(Settings.ScannerDecodeInterval * 1000));
                    continue;
                }

                // Process
                //Debug.Log(this + " SimpleScanner -> Scan ... " + Settings.width + " / " + Settings.height);
                try
                {
                    Result = Parser.Decode(pixels, Settings.width, Settings.height);
                    parserPixelAvailable = false;
                    //Debug.Log("result: "+Result);
                    if (Result == null)
                    {
                        continue;
                    }

                    // Sleep a little bit and set the signal to get the next frame
                    Thread.Sleep(Mathf.FloorToInt(Settings.ScannerDecodeInterval * 1000));
                }
                catch (ThreadAbortException te) { Debug.Log("error: " + te.Message); }
                catch (Exception e)
                {
                    Debug.Log("error: " + e.Message);
                }
            }
        }
#endif

        #endregion

        /// <summary>
        /// This Update Loop is used to :
        /// * Wait the Camera is really ready
        /// * Bring back Callback to the main thread when using Background Thread
        /// * To execute image Decoding When not using the background Thread
        /// </summary>
        public void Update()
        {
            if (Status == ScannerStatus.Running)
            {
                //Debug.Log("scanner update");
                // Call the callback if a result is there
                if (Result != null)
                {
                    //
                    Debug.Log(Result);
                    Callback(Result.Type, Result.Value);

                    // clean and return
                    Result = null;
                    parserPixelAvailable = false;
                    return;
                }

                // Get the image as an array of Color32
                //pixels = Camera.GetPixels(pixels);
                parserPixelAvailable = true;

                // If background thread OFF, do the decode main thread with 500ms of pause for UI
                if (!Settings.ScannerBackgroundThread && mainThreadLastDecode < Time.realtimeSinceStartup - Settings.ScannerDecodeInterval)
                {
                    DecodeQR();
                    mainThreadLastDecode = Time.realtimeSinceStartup;
                }
            }
        }

        public override string ToString()
        {
            return "[UnityBarcodeScanner]";
        }
    }
}
