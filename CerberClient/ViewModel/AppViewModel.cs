﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
using System.Windows.Threading;
using System.IO;
using CerberClient.ViewModel.BaseClasses;
using CerberClient.Model;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using CerberClient.Services;
using Microsoft.Identity.Client;
using RestSharp;
using CerberClient.Model.Api;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using RestSharp.Authenticators;

namespace CerberClient.ViewModel
{
    class AppViewModel : ViewModelBase
    {
        #region zmienne
        private string userLogin = "";
        private BitmapSource cameraView;
        private MainViewModel mainViewModel = new MainViewModel();
        private Capture videoCapture = null;
        private Image<Bgr, Byte> currentFrame = null;
        private CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private bool isTrained = false;
        private List<Image<Gray, Byte>> trainedFaces = new List<Image<Gray, Byte>>();
        private List<int> personLabels = new List<int>();
        private List<string> personsNames = new List<string>();
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private LBPHFaceRecognizer recognizer;
        private int numOfRecognisedFaces = 0;
        private int numOfNotRecognisedFaces = 0;
        private int numOfNotFoundFaces = 0;
        private int numOfLoops = 0;
        private CameraWatcher cameraWatcher = new CameraWatcher();
        private Task forceStop;
        private IAccount account;
        private bool closed = true;

        public BitmapSource CameraView
        {
            get => cameraView;
            set
            {
                cameraView = value;
                OnPropertyChanged(nameof(CameraView));
            }
        }
        #endregion

        #region Konstruktor
        public AppViewModel()
        {
            cameraWatcher.StartWatching();
            forceStop = new Task(() =>
            {
                while (!cameraWatcher.Stop)
                {

                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer = null;
                        videoCapture.Dispose();
                        videoCapture = null;
                    }

                    mainViewModel.SwapPage("login");
                }));
            });
            forceStop.Start();

            if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\Logs"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Logs");
            }

            if (!File.Exists(Directory.GetCurrentDirectory() + @"\Logs\zdarzenia.txt"))
            {
                File.Create(Directory.GetCurrentDirectory() + @"\Logs\zdarzenia.txt");
            }

            userLogin = UserData.Response.FirstName + " " + UserData.Response.LastName;
            RecognizingUserFace();
            IsInOrganization = UserData.Response.OrganisationId == null;
        }
        #endregion

        #region zmienne publiczne
        private bool isInOrganization;
        public bool IsInOrganization
        {
            get => isInOrganization;
            set
            {
                isInOrganization = value;
                OnPropertyChanged(nameof(IsInOrganization));
            }
        }

        private string key;
        public string Key
        {
            get => key;
            set
            {
                key = value;
                OnPropertyChanged(nameof(Key));
            }
        }

        private ObservableCollection<Contact> contacts;
        public ObservableCollection<Contact> Contacts
        {
            get => contacts;
            set
            {
                contacts = value;
                OnPropertyChanged(nameof(Contacts));
            }
        }

        private ICommand logOut;
        public ICommand LogOut
        {
            get
            {
                if(logOut == null)
                {
                    logOut = new RelayCommand(
                        x => {
                            LogOutUser();
                            mainViewModel.SwapPage("login");
                        },
                        x => true
                        );
                }

                return logOut;
            }
        }

        private ICommand connect;
        public ICommand Connect
        {
            get
            {
                if (connect == null)
                {
                    connect = new RelayCommand(async x =>
                    {
                        try
                        {
                            IPublicClientApplication app = PublicClientApplicationBuilder.Create("5cf90cca-ff25-4581-bc89-8ba5793a2fd3")
                                                        .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                                                        .Build();

                            string[] scopes = new string[]
                            {
                                 "user.read",
                                 "Contacts.Read",
                                 "Contacts.ReadWrite"
                            };

                            AuthenticationResult result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                            account = (await app.GetAccountsAsync()).First();

                            RestClient client = new RestClient("https://graph.microsoft.com/v1.0/me/contacts");
                            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(result.AccessToken, "Bearer");
                            RestRequest request = new RestRequest(Method.GET);
                            IRestResponse response = client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                ContactResponse contactResponse = JsonConvert.DeserializeObject<ContactResponse>(response.Content);

                                Contacts = contactResponse.Value;
                            }
                        }
                        catch(Exception ex) { }
                    },
                    x => account == null);
                }

                return connect;
            }
        }

        private ICommand joinOrganization;
        public ICommand JoinOrganization
        {
            get
            {
                if (joinOrganization == null)
                {
                    joinOrganization = new RelayCommand(x =>
                    {
                        JoinOrganisationRequest joinRequest = new JoinOrganisationRequest
                        {
                            Id = UserData.Response.Id,
                            Token = UserData.Response.RefreshToken,
                            Key = this.Key
                        };
                        RestClient client = new RestClient("http://localhost:4000/");
                        RestRequest request = new RestRequest("Account/join-organisation", Method.POST);
                        request.RequestFormat = RestSharp.DataFormat.Json;
                        request.AddJsonBody(joinRequest);
                        IRestResponse response = client.Execute(request);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            UserData.Response.OrganisationId = 0;
                            IsInOrganization = false;
                            Task.Run(() =>
                            {
                                MessageBox.Show("Joinded organisation");
                            });
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                MessageBox.Show("Cannot join organisation");
                            });
                        }
                    });
                }

                return joinOrganization;
            }
        }

        private ICommand sendMail;
        public ICommand SendMail
        {
            get
            {
                if (sendMail == null)
                {
                    sendMail = new RelayCommand(x =>
                    {
                        System.Diagnostics.Process.Start("mailto:" + ((Contact)x).Email);
                    });
                }

                return sendMail;
            }
        }
        #endregion

        #region Funkcja Wylogowania

        private void LogOutUser()
        {
            cameraWatcher.StopWatching();
            timer.Stop();
            timer = null;
            videoCapture.Dispose();
            videoCapture = null;
        }

        #endregion

        #region rozpoznawanie twarzy
        private void RecognizingUserFace()
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            numOfLoops++;
            if (videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                videoCapture.Retrieve(frame, 0);
                currentFrame = frame.ToImage<Bgr, Byte>().Resize(200, 200, Inter.Cubic);

                Mat grayImage = new Mat();
                CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(grayImage, grayImage);

                var faces = classifier.DetectMultiScale(grayImage, 1.1, 3);

                if(faces.Length == 0)
                {
                    numOfNotFoundFaces++;
                }

                foreach (var face in faces)
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
                        if (result.Label == 0 && result.Distance < 100)
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
                            numOfNotRecognisedFaces++;
                        }
                    }
                }

            }

            CameraView = ToBitmapSource(currentFrame);

            if (numOfLoops == 50)
            {
                UserNotification();
                numOfNotFoundFaces = 0;
                numOfNotRecognisedFaces = 0;
                numOfRecognisedFaces = 0;
                numOfLoops = 0;
            }
        }

        private void UserNotification()
        {
            StreamWriter writer;
            writer = File.AppendText(Directory.GetCurrentDirectory() + @"\Logs\zdarzenia.txt");
            if(numOfNotRecognisedFaces >= 35 || (numOfNotRecognisedFaces > numOfNotFoundFaces && numOfNotRecognisedFaces > numOfRecognisedFaces))
            {
                if (closed)
                {
                    Task.Run(() =>
                    {
                        closed = false;
                        MessageBox.Show("Your are not owner of this account!");
                        closed = true;
                    });
                }
                
                writer.WriteLine(DateTime.Today.ToString() + " | " + userLogin + " | Inna osoba przed monitorem");
                cameraWatcher.ProblemStart();

            }
            if(numOfNotFoundFaces >= 35 || (numOfNotRecognisedFaces < numOfNotFoundFaces && numOfNotFoundFaces > numOfRecognisedFaces))
            {
                if (closed)
                {
                    Task.Run(() =>
                    {
                        closed = false;
                        MessageBox.Show("No one in front of screen!");
                        closed = true;
                    });
                }
                
                writer.WriteLine(DateTime.Today.ToString() + " | " + userLogin + " | Nie ma nikogo przed monitorem");
                cameraWatcher.ProblemStart();
            }

            if(numOfRecognisedFaces >= 35 || (numOfRecognisedFaces > numOfNotFoundFaces && numOfRecognisedFaces > numOfNotFoundFaces))
            {
                cameraWatcher.CameraOk();
            }

            writer.Close();
        }

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
                    Image<Gray, Byte> image = new Image<Gray, Byte>(bmp).Resize(200, 200, Inter.Cubic);

                    trainedFaces.Add(image);
                    personLabels.Add(imagesCount);
                    string name = userLogin;
                    if (!personsNames.Contains(name))
                    {
                        personsNames.Add(name);
                    }
                }

                try
                {
                    string path = Directory.GetCurrentDirectory() + @"\Lewy\";
                    string[] files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        Image<Gray, Byte> image = new Image<Gray, Byte>(file).Resize(200, 200, Inter.Cubic);
                        trainedFaces.Add(image);
                        personLabels.Add(1);
                        string name = userLogin;
                        if (!personsNames.Contains("lewy"))
                        {
                            personsNames.Add("lewy");
                        }
                    }

                    path = Directory.GetCurrentDirectory() + @"\Tusk\";
                    files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        Image<Gray, Byte> image = new Image<Gray, byte>(file).Resize(200, 200, Inter.Cubic);
                        trainedFaces.Add(image);
                        personLabels.Add(2);
                        string name = userLogin;
                        if(!personsNames.Contains("tusk"))
                        {
                            personsNames.Add("tusk");
                        }
                    }
                }
                catch { }

                recognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 100);
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
