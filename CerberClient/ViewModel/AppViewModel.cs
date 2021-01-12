using System;
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
        private List<Image<Bgr, Byte>> trainedFaces = new List<Image<Bgr, byte>>();
        private List<int> personLabels = new List<int>();
        private List<string> personsNames = new List<string>();
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private EigenFaceRecognizer recognizer;
        private int numOfRecognisedFaces = 0;
        private int numOfNotRecognisedFaces = 0;
        private int numOfNotFoundFaces = 0;
        private int numOfLoops = 0;
        private CameraWatcher cameraWatcher = new CameraWatcher();
        private Task forceStop;

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
                    timer.Stop();
                    timer = null;
                    videoCapture.Dispose();
                    videoCapture = null;
                    UserData.Response = null;
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
        }
        #endregion

        #region zmienne publiczne
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
            videoCapture = new Capture();
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
                            numOfNotRecognisedFaces++;
                        }
                    }
                }

            }

            CameraView = ToBitmapSource(currentFrame);

            if (numOfLoops == 333)
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
            if(numOfNotRecognisedFaces >= 250 || (numOfNotRecognisedFaces > numOfNotFoundFaces && numOfNotRecognisedFaces > numOfRecognisedFaces))
            {
                MessageBox.Show("Nie jesteś właścicielem konta");
                writer.WriteLine(DateTime.Today.ToString() + " | " + userLogin + " | Inna osoba przed monitorem");
                if(cameraWatcher.Problem == false)
                    cameraWatcher.ProblemStart();

            }
            if(numOfNotFoundFaces >= 250 || (numOfNotRecognisedFaces < numOfNotFoundFaces && numOfNotFoundFaces > numOfRecognisedFaces))
            {
                MessageBox.Show("Nie ma nikogo przed monitorem");
                writer.WriteLine(DateTime.Today.ToString() + " | " + userLogin + " | Nie ma nikogo przed monitorem");
                if(cameraWatcher.Problem == false)
                    cameraWatcher.ProblemStart();
            }

            if(numOfNotRecognisedFaces >= 250 || (numOfRecognisedFaces > numOfNotFoundFaces && numOfRecognisedFaces > numOfNotFoundFaces))
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
                Bitmap bmp;
                using (var ms = new MemoryStream(Convert.FromBase64String(UserData.Response.Image)))
                {
                    bmp = new Bitmap(ms);
                }
                Image<Bgr, Byte> image = new Image<Bgr, byte>(bmp).Resize(200, 200, Inter.Cubic);

                trainedFaces.Add(image);
                personLabels.Add(imagesCount);
                string name = userLogin;
                personsNames.Add(name);

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
