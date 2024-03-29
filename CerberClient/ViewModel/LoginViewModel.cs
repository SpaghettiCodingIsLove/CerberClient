﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CerberClient.ViewModel.BaseClasses;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.IO;
using System.Threading;
using CerberClient.Model;
using CerberClient.Model.Api;
using System.Windows.Controls;
using RestSharp;
using Newtonsoft.Json;
using System.Drawing;

namespace CerberClient.ViewModel
{
    class LoginViewModel : ViewModelBase
    {
        #region Variables

        private MainViewModel mainViewModel = new MainViewModel();

        private Capture videoCapture = null;
        private Image<Bgr, Byte> currentFrame = null;
        private CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private bool isTrained = false;
        private List<Image<Bgr, Byte>> trainedFaces = new List<Image<Bgr, byte>>();
        private List<int> personLabels = new List<int>();
        private List<string> personsNames = new List<string>();
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private EigenFaceRecognizer recognizer;
        private int numOfRecognisedFaces = 0;
        private int numOfLoops = 0;

        private string email;
        private string password;
        private bool isOpen = false;
        private BitmapSource cameraView;

        public BitmapSource CameraView
        {
            get => cameraView;
            set
            {
                cameraView = value;
                OnPropertyChanged(nameof(CameraView));
            }
        }

        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                OnPropertyChanged(nameof(IsOpen));
            }
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        #endregion

        private ICommand goToRegister;
        public ICommand GoToRegister
        {
            get
            {
                if(goToRegister == null)
                {
                    goToRegister = new RelayCommand(
                        x => {
                            mainViewModel.SwapPage("register");
                        },
                        x => true
                        );
                }

                return goToRegister;
            }
        }

        private ICommand goToApp;
        public ICommand GoToApp
        {
            get
            {
                if(goToApp == null)
                {
                    goToApp = new RelayCommand(
                        x => {
                            PasswordBox pwBox = x as PasswordBox;
                            if (!string.IsNullOrWhiteSpace(pwBox.Password))
                            {
                                AuthenticateRequest authenticateRequest = new AuthenticateRequest
                                {
                                    Email = this.Email,
                                    Password = pwBox.Password
                                };
                                RestClient client = new RestClient("http://localhost:4000/");
                                RestRequest request = new RestRequest("Account/authenticate", Method.POST);
                                request.RequestFormat = RestSharp.DataFormat.Json;
                                request.AddJsonBody(authenticateRequest);
                                IRestResponse response = client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    UserData.Response = JsonConvert.DeserializeObject<AuthenticateResponse>(response.Content);

                                    if (UserData.Response.IsOperator)
                                    {
                                        mainViewModel.SwapPage("operator");
                                    }
                                    else
                                    {
                                        IsOpen = true;
                                        OpenCamera();
                                        //mainViewModel.SwapPage("app");
                                    }

                                }
                                else
                                {

                                    Task.Run(() =>
                                    {
                                        MessageBox.Show("Login in failed!");
                                    });
                                }
                            }
                        },
                        x => Email != null 
                        );
                }

                return goToApp;
            }
        }

        #region Face Detection

        private void isFaceRecognized()
        {
            timer.Stop();
            timer = null;
            videoCapture.Dispose();
            videoCapture = null;

            if (numOfRecognisedFaces >= 35)
            {
                /*Task.Run(() =>
                {
                    MessageBox.Show("Rozpoznano");
                });*/
                mainViewModel.SwapPage("app");
            }
            else
            {
                Task.Run(() =>
                {
                    MessageBox.Show("Not recoginzed");
                });
            }
            IsOpen = false;
        }

        // Włączenie kamery
        private void OpenCamera()
        {
            if (videoCapture != null)
                videoCapture.Dispose();
            videoCapture = new Capture(1);
            TrainImages();
            numOfLoops = 0;
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        // Wykrywanie i rozpoznawanie twarzy oraz przesyłanie obrazu do obiektu Image
        private void Timer_Tick(object sender, EventArgs e)
        {
            numOfLoops++;
            if (videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                videoCapture.Retrieve(frame, 0);
                currentFrame = frame.ToImage<Bgr, Byte>().Resize(320, 240, Inter.Cubic);

                Mat grayImage = new Mat();
                CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(grayImage, grayImage);

                var faces = classifier.DetectMultiScale(grayImage, 1.1, 3);

                foreach(var face in faces)
                {
                    CvInvoke.Rectangle(currentFrame, face, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);

                    Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
                    resultImage.ROI = face;

                    if (isTrained)
                    {
                        Image<Gray, Byte> grayImage2 = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                        CvInvoke.EqualizeHist(grayImage2, grayImage2);
                        var result = recognizer.Predict(grayImage2);

                        // Rozpoznano osobę ze zdjęcia
                        if (result.Label != -1 && result.Distance < 3000)
                        {
                            CvInvoke.PutText(currentFrame, personsNames[result.Label], new System.Drawing.Point(face.X - 2, face.Y - 2),
                                FontFace.HersheyComplex, 1.0, new Bgr(System.Drawing.Color.Orange).MCvScalar);
                            CvInvoke.Rectangle(currentFrame, face, new Bgr(System.Drawing.Color.Green).MCvScalar, 2);
                            numOfRecognisedFaces++;
                        }
                        // Nie rozpoznało osoby ze zdjęcia
                        else
                        {
                            CvInvoke.PutText(currentFrame, "Unknown", new System.Drawing.Point(face.X - 2, face.Y - 2),
                                FontFace.HersheyComplex, 1.0, new Bgr(System.Drawing.Color.Orange).MCvScalar);
                            CvInvoke.Rectangle(currentFrame, face, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);
                        }
                    }
                }

            }
            CameraView = ToBitmapSource(currentFrame);

            if (numOfLoops == 50)
            {
                isFaceRecognized();
            }
        }

        // Trenowanie klasyfikatora za pomocą zdjęcia użytkownika

        private bool TrainImages()
        {
            int imagesCount = 0;
            double tresholds = 5000;
            trainedFaces.Clear();
            personLabels.Clear();

            try
            {
                foreach (string img in UserData.Response.Image)
                {
                    Bitmap bmp;
                    using (var ms = new MemoryStream(Convert.FromBase64String(img)))
                    {
                        bmp = new Bitmap(ms);
                    }
                    Image<Bgr, Byte> image = new Image<Bgr, byte>(bmp).Resize(200, 200, Inter.Cubic);

                    trainedFaces.Add(image);
                    personLabels.Add(imagesCount);
                    string name = UserData.Response.FirstName + " " + UserData.Response.LastName;
                    personsNames.Add(name);
                }

                recognizer = new EigenFaceRecognizer(imagesCount, tresholds);
                recognizer.Train(trainedFaces.ToArray(), personLabels.ToArray());

                isTrained = true;
                return true;
            }
            catch (Exception e)
            {
                isTrained = false;
                return false;
            }
        }
        

        // Zamiana przechwyconego obrazu z kamery na odpowiedni format dla elementu Image
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        private static BitmapSource ToBitmapSource(Image<Bgr, Byte> image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr, IntPtr.Zero, Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
                    );

                DeleteObject(ptr);
                return bs;
            }
        }

        #endregion
    }
}
