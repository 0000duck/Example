using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Cuda;
using System.IO.MemoryMappedFiles;


namespace Example
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> _imgInput;
        VideoCapture capture1;

        public Form1()
        {
            InitializeComponent();
        }

        int i=0;
        int fla = 0;
        int frmno = 1;
        char[] arrrr = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j'}; 
        VideoCapture capture;
        Boolean Pause = false;
        Boolean captureprocess = false;

        public bool skinAreaDetection(Bitmap b)
        {
            for (int i = 0; i < b.Width; i++)
            {
                for (int j = 0; j < b.Height; j++)
                {
                    Color c1 = b.GetPixel(i, j);
                    int r1 = c1.R;
                    int g1 = c1.G;
                    int b1 = c1.B;
                    int a1 = c1.A;
                    int gray = (byte)(.299 * r1 + .587 * g1 + .114 * b1);
                    float hue = c1.GetHue();
                    float sat = c1.GetSaturation();
                    float val = c1.GetBrightness();
                    if (0.0 <= hue && hue <= 50.0 && 0.1 <= sat && sat <= 1 /*&& val > 130*/ && r1 > 95 && g1 > 40 && b1 > 20 && r1 > g1 && r1 > b1 && Math.Abs(r1 - g1) > 15 && a1 > 15)
                    {
                        b.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                    }
                    else
                    {
                        b.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    }
                }
            }
            return true;
        }

        private async void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                return;
            }
            try
            {
                double frameNumber = capture.GetCaptureProperty(CapProp.FrameCount);
                float[] smoothgrad = new float[(int)frameNumber];
                label1.Text = "Frame Count is : " + frameNumber.ToString();
                while (!Pause)
                {
                    Mat matInput = new Mat();
                    capture.Read(matInput);
                    if (!matInput.IsEmpty)
                    {
                        modulePreProcessing(matInput, smoothgrad);
                        double fps = capture.GetCaptureProperty(CapProp.Fps);
                        await Task.Delay(1000 / Convert.ToInt32(fps));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void modulePreProcessing(Mat inputMat, float[] smoothgrad)
        {
            //Actual video is played in pictureBox1
            Image<Bgr, Byte> imageInput = inputMat.ToImage<Bgr, Byte>();
            pictureBox1.Image = imageInput.ToBitmap();

            //Skin area detection
            skinAreaDetection(inputMat.Bitmap);

            //Face elimination
            imageInput = inputMat.ToImage<Bgr, Byte>();
            Image<Gray, byte> grayframe = imageInput.Convert<Gray, byte>();
            CascadeClassifier face = new CascadeClassifier("C:\\Emgu\\emgucv-windesktop 3.3.0.2824\\opencv\\data\\haarcascades\\haarcascade_frontalface_default.xml");
            var faces = face.DetectMultiScale( grayframe, 1.1, 22, new Size(10, 10));
            foreach (var f in faces)
            {
                Rectangle faceRectangle = Rectangle.Inflate(f, 40, 40);
                imageInput.Draw(faceRectangle, new Bgr(Color.Black), -1);
            }

            //MedianBlur
            Image<Bgr, Byte> imageMedianBlur = new Image<Bgr, Byte>(inputMat.Width, inputMat.Height);
            CvInvoke.MedianBlur(imageInput, imageMedianBlur, 21);
            pictureBox2.Image = imageMedianBlur.Bitmap;


            Image<Gray, Byte> imgeOrigenal = imageMedianBlur.Convert<Gray, Byte>();
            
            //Imagebox1 gray Image without face
            imageBox1.Image = imgeOrigenal;

            //sobel
            //Image<Gray, Byte> imgeOrigenal = m1.ToImage<Gray, Byte>();
            Image<Gray, float> _imgSobelx = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
            Image<Gray, float> _imgSobely = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
            _imgSobelx = imgeOrigenal.Sobel(1, 0, 3);
            _imgSobely = imgeOrigenal.Sobel(0, 1, 3);
            Image<Gray, float> magnitude = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
            Image<Gray, float> angle = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
            CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);

            //Image box2 image of gradient
            imageBox2.Image = magnitude;
            double inu = magnitude.GetAverage().Intensity;

            Mat mat_magnitude = magnitude.Mat;

            ///*
            ///
            /// 
            /// 
            /// 
            Image<Bgr, byte> ajkd = magnitude.Convert<Bgr, byte>();
            HOGDescriptor ho = new HOGDescriptor();
            float[] sdff = GetVector(ajkd);
            string fram = "";
            label3.Text = sdff.Length.ToString();
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\des.txt", sdff.Length.ToString());





            RangeF max_magitude = mat_magnitude.GetValueRange();
            //array of max_magnitude
            smoothgrad[frmno] = max_magitude.Max;
            label2.Text = frmno.ToString();

            //chart of gradient
            chart1.Series["Gradient"].Points.AddXY(frmno, inu);

            string fra = System.IO.File.ReadAllText(@"C:\Users\rhiray1996\Desktop\g.txt");
            if (max_magitude.Max == 0)
            {
                if (fla == 0)
                {
                    i++;
                    fla = 1;
                }
                fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString() + "                Next Gesture " + Environment.NewLine;
            }
            else
            {
                fla = 0;
                //label2.Text = arrrr[i].ToString();
                fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString() + Environment.NewLine;
            }
            frmno++;
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\g.txt", fra);

            //histogram
            /*histogramBox1.ClearHistogram();
            histogramBox1.GenerateHistograms(magnitude, 36);
            histogramBox1.Refresh();*/
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause = !Pause;
        }

        private void openVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\g.txt", " ");
            //ofd.filter
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                capture = new VideoCapture(ofd.FileName);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, 240);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, 320);
                Mat m = new Mat();
                capture.Read(m);
                pictureBox1.Image = m.Bitmap;
            }
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            if (of.ShowDialog() == DialogResult.OK)
            {
                _imgInput = new Image<Bgr, byte>(of.FileName);
                imageBox1.Image = _imgInput;
            }
        }

        public Image<Bgr, Byte> Resize(Image<Bgr, Byte> im)
        {
            return im.Resize(64, 128, Emgu.CV.CvEnum.Inter.Linear);
        }

        public float[] GetVector(Image<Bgr, Byte> im)
        {
            HOGDescriptor hog = new HOGDescriptor();    // with defaults values
            Image<Bgr, Byte> imageOfInterest = Resize(im);
            imageBox2.Image = imageOfInterest;
            System.Drawing.Point[] p = new System.Drawing.Point[imageOfInterest.Width * imageOfInterest.Height];
            int k = 0;
            for (int i = 0; i < imageOfInterest.Width; i++)
            {
                for (int j = 0; j < imageOfInterest.Height; j++)
                {
                    System.Drawing.Point p1 = new System.Drawing.Point(i, j);
                    p[k++] = p1;
                }
            }
            float[] result = hog.Compute(imageOfInterest, new System.Drawing.Size(16, 16), new System.Drawing.Size(0, 0), p);
            return result;
        }

        private void skinDetectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_imgInput == null)
            {
                return;
            }
            skinAreaDetection(_imgInput.Bitmap);
            imageBox1.Image = _imgInput;
        }

        private void sobelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_imgInput == null)
            {
                return;
            }

            //IOutputArray magnitude;
            // IOutputArray angle;
            Image<Gray, byte> imgeOrigenal = _imgInput.Convert<Gray, byte>();
            Image<Gray, float> _imgSobelx = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            Image<Gray, float> _imgSobely = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            _imgSobelx = imgeOrigenal.Sobel(1, 0, 3);
            _imgSobely = imgeOrigenal.Sobel(0, 1, 3);
            Image<Gray, float> magnitude = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            Image<Gray, float> angle = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);
            imageBox1.Image = magnitude;

            Mat mag = magnitude.Mat;
            Image<Bgr, byte> ajkd = magnitude.Convert<Bgr, byte>();
            RangeF a = mag.GetValueRange();
            label2.Text = "Max: " + a.Max.ToString() + "  Min: " + a.Min.ToString();
            
            imageBox1.Refresh();
            histogramBox1.ClearHistogram();
            histogramBox1.GenerateHistograms(magnitude, 36);
            histogramBox1.Refresh();
            HOGDescriptor ho = new HOGDescriptor();
            float[] sdff = GetVector(ajkd);
            string fra = "";
            label3.Text = sdff.GetValue(10).ToString();
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\des.txt", sdff.Length.ToString());
            /*for (int i = 0; i< sdff.Length; i++)
            {
                fra = "Frame Number: " + i + " Value " + sdff.GetValue(i).ToString() + Environment.NewLine;
                System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\des.txt", fra);
            }*/

        }

        private  void cameraInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\g.txt", " ");
            /* if(capture1==null)
             {
                 capture1 = new Emgu.CV.VideoCapture(0);
             }
             capture1.ImageGrabbed += Capture1_ImageGrabbed;
             capture1.Start();*/
            try
            {
                capture1 = new VideoCapture();
            }
            catch(NullReferenceException exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }
            Application.Idle += new EventHandler(ProcessFunction);
            captureprocess = true;

        }

        void ProcessFunction(object sender, EventArgs e)
        {
            int frmno = 1;
            int frameNumber = 2000;

            Mat m = new Mat();
            float[] smoothgrad = new float[(int)frameNumber];
            m = capture1.QueryFrame();

           
            if (!m.IsEmpty)
            {

                //Video is Played in PictureBox1
                //Mat to Image Bgr
                Image<Bgr, Byte> star = m.ToImage<Bgr, Byte>();
                pictureBox1.Image = star.ToBitmap();


                //skin area detection Mat
                skinAreaDetection(m.Bitmap);

                //Mat to Image Bgr
                //Face detection and removal
                //input imsrc and grayframe
                //output in imsrc image
                Image<Bgr, byte> imsrc = m.ToImage<Bgr, byte>();
                Image<Gray, byte> grayframe = imsrc.Convert<Gray, byte>();

                #region face
                CascadeClassifier face = new CascadeClassifier("C:\\Emgu\\emgucv-windesktop 3.3.0.2824\\opencv\\data\\haarcascades\\haarcascade_frontalface_default.xml");
                CascadeClassifier eye = new CascadeClassifier("C:\\Emgu\\emgucv-windesktop 3.3.0.2824\\opencv\\data\\haarcascades\\haarcascade_eye.xml");

                var faces = face.DetectMultiScale(
                    grayframe,
                    1.1,
                    10,
                    new Size(10, 10));
                foreach (var f in faces)
                {
                    Rectangle yuhu = Rectangle.Inflate(f, 40, 40);
                    imsrc.Draw(yuhu, new Bgr(Color.Black), -1);
                }
                #endregion


                //medianBlur
                // Image<Bgr, Byte> imsrc = m.ToImage<Bgr, Byte>();
                //input imsrc without face
                //output in imageMedianBlur
                Image<Bgr, Byte> imageMedianBlur = new Image<Bgr, Byte>(m.Width, m.Height);
                CvInvoke.MedianBlur(imsrc, imageMedianBlur, 51);

                //Picture Box 2 Image without face
                pictureBox2.Image = imageMedianBlur.Bitmap;
                //Mat m1 = imdest.Mat;

                //contour
                //imageMedianBlur to Gray imgeOrigenal
                Image<Gray, Byte> imgeOrigenal = imageMedianBlur.Convert<Gray, Byte>();
                #region countour
                //ThresholdBinary(new Gray(50), new Gray(255));
                /*Emgu.CV.Util.VectorOfVectorOfPoint countour = new Emgu.CV.Util.VectorOfVectorOfPoint();
                Mat hier = new Mat();
                CvInvoke.FindContours(imgeOrigenal, countour, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                Dictionary<int, double> dict = new Dictionary<int, double>();

                if(countour.Size > 0)
                {
                    for(int i = 0; i < countour.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(countour[i]);
                        dict.Add(i, area);
                    }
                }

                var item = dict.OrderByDescending(v => v.Value).Take(2);

                foreach(var it in item)
                {
                    int key = int.Parse(it.Key.ToString());
                    Rectangle rect = CvInvoke.BoundingRectangle(countour[key]);
                   // CvInvoke.Rectangle(imgeOrigenal, rect, new MCvScalar(255, 0, 0));
                }*/
                #endregion

                //Imagebox1 gray Image without face
                imageBox1.Image = imgeOrigenal;

                //sobel
                //Image<Gray, Byte> imgeOrigenal = m1.ToImage<Gray, Byte>();
                Image<Gray, float> _imgSobelx = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
                Image<Gray, float> _imgSobely = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
                _imgSobelx = imgeOrigenal.Sobel(1, 0, 3);
                _imgSobely = imgeOrigenal.Sobel(0, 1, 3);
                Image<Gray, float> magnitude = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
                Image<Gray, float> angle = new Image<Gray, float>(imgeOrigenal.Width, imgeOrigenal.Height);
                CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);

                //Image box2 image of gradient
                imageBox2.Image = magnitude;
                double inu = magnitude.GetAverage().Intensity;

                Mat mat_magnitude = magnitude.Mat;
                RangeF max_magitude = mat_magnitude.GetValueRange();
                //array of max_magnitude
                smoothgrad[frmno] = max_magitude.Max;
                label2.Text = frmno.ToString();

                //chart of gradient
                chart1.Series["Gradient"].Points.AddXY(frmno, inu);

                string fra = System.IO.File.ReadAllText(@"C:\Users\rhiray1996\Desktop\g.txt");
                if (max_magitude.Max == 0)
                {
                    if (fla == 0)
                    {
                        i++;
                        fla = 1;
                    }
                    fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString() + "                Next Gesture " + Environment.NewLine;
                }
                else
                {
                    fla = 0;
                    //label2.Text = arrrr[i].ToString();
                    fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString() + Environment.NewLine;
                }
                frmno++;
                System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\g.txt", fra);

                //histogram
                /*histogramBox1.ClearHistogram();
                histogramBox1.GenerateHistograms(magnitude, 36);
                histogramBox1.Refresh();*/
                //double fps = capture.GetCaptureProperty(CapProp.Fps);
                //Task.Delay(1000 / Convert.ToInt32(fps));
            }

            /* VideoW = new VideoWriter(@"temp.avi",
                                    CvInvoke.CV_FOURCC('M', 'P', '4', '2'),
                                    (Convert.ToInt32(upDownFPS.Value)),
                                    imgOrg.Width,
                                    imgOrg.Height,
                                    true);
             VideoW.WriteFrame(imgOrg);*/
        }

        private void playPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(captureprocess == true)
            {
                Application.Idle -=ProcessFunction;
                captureprocess = false;
                playPauseToolStripMenuItem.Text = "Play";
            }
            else
            {
                Application.Idle += ProcessFunction;
                captureprocess = true;
                playPauseToolStripMenuItem.Text = "Pause";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to close window?", "System Message", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.Close();
            }
        }

        #region extra
        /* private void Capture1_ImageGrabbed(object sender, EventArgs e)
         {
             throw new NotImplementedException();
         }*/
        /* private async void ProcessFrame(object sender, EventArgs e)
         {
             try
             {
                 while (!Pause)
                 {

                     Mat m = _capture.QueryFrame();
                     pictureBox1.Image = m.ToImage<Bgr, Byte>().Bitmap;
                     await Task.Delay(1000);

                 }
             }
             catch (Exception ex)
             {
                 MessageBox.Show(ex.Message);
             }
         }*/
        /*Emgu.CV.Util.VectorOfVectorOfPoint countour = new Emgu.CV.Util.VectorOfVectorOfPoint();
        Mat hier = new Mat();
        CvInvoke.FindContours(imgeOrigenal, countour, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);
        Dictionary<int, double> dict = new Dictionary<int, double>();

        if(countour.Size > 0)
        {
            for(int i = 0; i < countour.Size; i++)
            {
                double area = CvInvoke.ContourArea(countour[i]);
                dict.Add(i, area);
            }
        }

        var item = dict.OrderByDescending(v => v.Value).Take(2);

        foreach(var it in item)
        {
            int key = int.Parse(it.Key.ToString());
            Rectangle rect = CvInvoke.BoundingRectangle(countour[key]);
           // CvInvoke.Rectangle(imgeOrigenal, rect, new MCvScalar(255, 0, 0));
        }*/
        #endregion

    }
}


