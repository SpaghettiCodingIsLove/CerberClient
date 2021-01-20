using CerberClient.Model.Api;
using CerberClient.ViewModel.BaseClasses;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DataFormat = RestSharp.DataFormat;

namespace CerberClient.ViewModel
{
    class RegisterViewModel : ViewModelBase
    {
        private Capture videoCapture = null;
        private Image<Bgr, Byte> currentFrame = null;
        private CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private bool isTrained = false;
        private List<int> personLabels = new List<int>();
        private List<string> personsNames = new List<string>();
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private EigenFaceRecognizer recognizer;
        private int numOfRecognisedFaces = 0;
        private int numOfLoops = 0;
        private List<string> imagesToSend = new List<string>();
        private string pass;

        private MainViewModel mainViewModel = new MainViewModel();

        private string name;
        private string lastName;
        private string email;
        private bool consent;
        private byte[] image;
        private string imagePath;
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

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string LastName
        {
            get => lastName;
            set
            {
                lastName = value;
                OnPropertyChanged(nameof(LastName));
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

        public byte[] Image
        {
            get => image;
            set
            {
                image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public string ImagePath
        {
            get => imagePath;
            set
            {
                imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
            }
        }

        public bool Consent
        {
            get => consent;
            set
            {
                consent = value;
                OnPropertyChanged(nameof(Consent));
            }
        }


        private ICommand createAccount;
        public ICommand CreateAccount 
        {
            get
            {
                if(createAccount == null)
                {
                    createAccount = new RelayCommand(
                        x =>
                        {
                            PasswordBox pwBox = x as PasswordBox;
                            if (!string.IsNullOrWhiteSpace(pwBox.Password) && pwBox.Password.Length >= 4)
                            {
                                pass = pwBox.Password;
                                IsOpen = true;
                                OpenCamera();
                            }     
                        },
                        x => !string.IsNullOrWhiteSpace(Email)
                        && !string.IsNullOrWhiteSpace(Name) 
                        && !string.IsNullOrWhiteSpace(LastName) 
                        && Consent
                        );
                }

                return createAccount;
            }
        }

        private void OpenCamera()
        {
            if (videoCapture != null)
                videoCapture.Dispose();
            videoCapture = new Capture();
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

                if (faces.Length == 1)
                {
                    foreach (var face in faces)
                    {
                        CvInvoke.Rectangle(currentFrame, face, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);

                        Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
                        resultImage.ROI = face;

                        Image<Gray, Byte> grayImage2 = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                        CvInvoke.EqualizeHist(grayImage2, grayImage2);
                        var converter = new ImageConverter();
                        var outputArray = (byte[])converter.ConvertTo(grayImage2.ToBitmap(), typeof(byte[]));

                        imagesToSend.Add(Convert.ToBase64String(outputArray));
                        if (imagesToSend.Count == 3)
                        {
                            timer.Stop();
                            timer = null;
                            videoCapture.Dispose();
                            videoCapture = null;
                            RegisterRequest registerRequest = new RegisterRequest
                            {
                                Email = this.Email,
                                FirstName = this.Name,
                                LastName = this.LastName,
                                Password = pass,
                                ImageArray = imagesToSend.ToArray()
                            };
                            pass = null;
                            RestClient client = new RestClient("http://localhost:4000/");
                            RestRequest request = new RestRequest("Account/register", Method.POST);
                            request.RequestFormat = DataFormat.Json;
                            request.AddJsonBody(registerRequest);
                            IRestResponse response = client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                Task.Run(() =>
                                {
                                    MessageBox.Show("Pomyślnie założono konto");
                                });
                                mainViewModel.SwapPage("login");
                            }
                            else
                            {

                               
                                 Task.Run(() =>
                                 {
                                     MessageBox.Show("Rejestracja nie powiodła się");
                                 });
                            }
                            IsOpen = false;
                            imagesToSend.Clear();
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                }

            }
            CameraView = ToBitmapSource(currentFrame);
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

    }
}
