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
        #region declaration
        VideoWriter VideoW;
        Image<Bgr, byte> _imgInput;
        int frameNumber = 1;
        int first = -1;
        int last = -1;
        VideoCapture capture;
        Boolean Pause = false;
        Boolean captureProcess = false;
        Boolean isFirst = false;
        Boolean isLast = false;
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

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
                VideoW = new VideoWriter(@"temp.avi",
                                    Convert.ToInt32(capture.GetCaptureProperty(CapProp.Fps)),
                                    new Size(capture.Width, capture.Height),
                                    true);
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
            pictureBox2.Image = inputMat.Bitmap;  //Face un-eliminated image


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
            imageBox1.Image = imageInput; //Face elimination

            //MedianBlur for Key Frame Extraction
            Image<Bgr, Byte> imageMedianBlur = new Image<Bgr, Byte>(inputMat.Width, inputMat.Height);
            CvInvoke.MedianBlur(imageInput, imageMedianBlur, 21);
            imageBox2.Image = imageMedianBlur; //Noise Removing


            
            VideoW.Write(imageMedianBlur.Mat);

            //MedianBlur for Feature Extraction
            /*Image<Bgr, Byte> imageFE = new Image<Bgr, Byte>(inputMat.Width, inputMat.Height);
            CvInvoke.MedianBlur(imageInput, imageFE, 7);
            pictureBox2.Image = imageFE.Bitmap;*/

            //Sobel
            Image<Gray, float> imageSobelInput = imageMedianBlur.Convert<Gray, float>();
            Image<Gray, float> _imgSobelx = new Image<Gray, float>(imageSobelInput.Width, imageSobelInput.Height);
            Image<Gray, float> _imgSobely = new Image<Gray, float>(imageSobelInput.Width, imageSobelInput.Height);
            _imgSobelx = imageSobelInput.Sobel(1, 0, 3);
            _imgSobely = imageSobelInput.Sobel(0, 1, 3);
            Image<Gray, float> magnitude = new Image<Gray, float>(imageSobelInput.Width, imageSobelInput.Height);
            Image<Gray, float> angle = new Image<Gray, float>(imageSobelInput.Width, imageSobelInput.Height);
            CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);

            //Image box2 image of gradient
            imageBox3.Image = magnitude;
            //FrameNumber Display
            label2.Text = frameNumber.ToString();

            double avg = magnitude.GetAverage().Intensity;
            
            //Chart of gradient
            chart1.Series["Gradient"].Points.AddXY(frameNumber, avg);

            if (!isFirst)
            {
                if(avg > 0)
                {
                    isFirst = true;
                    first = frameNumber;
                    isLast = false;
                }
            }
            if (!isLast)
            {
                if (avg == 0)
                {
                    isLast = true;
                    last = frameNumber;
                    label3.Text = "First: " + first + " Last: " + last;
                    isFirst = false;
                }
            }

            Image<Bgr, byte> imageHogInput = imageMedianBlur.Convert<Bgr, byte>();
            HOGDescriptor ho = new HOGDescriptor();
            float[] desc = new float[3780];
            desc = GetVector(imageHogInput);
            string fram = "";
            System.IO.File.WriteAllText(@"des.txt", desc.Length.ToString());

            frameNumber++;
            
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause = !Pause;
        }

        private void openVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            System.IO.File.WriteAllText(@"g.txt", " ");
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

        public Image<Bgr, Byte> resize(Image<Bgr, Byte> im)
        {
            return im.Resize(64, 128, Emgu.CV.CvEnum.Inter.Linear);
        }

        public float[] GetVector(Image<Bgr, Byte> im)
        {
            HOGDescriptor hog = new HOGDescriptor();    // with defaults values
            Image<Bgr, Byte> imageOfInterest = resize(im);
            //imageBox3.Image = imageOfInterest;
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
            Image<Gray, byte> imageSobelInput = _imgInput.Convert<Gray, byte>();
            Image<Gray, float> _imgSobelx = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            Image<Gray, float> _imgSobely = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            _imgSobelx = imageSobelInput.Sobel(1, 0, 3);
            _imgSobely = imageSobelInput.Sobel(0, 1, 3);
            Image<Gray, float> magnitude = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            Image<Gray, float> angle = new Image<Gray, float>(_imgInput.Width, _imgInput.Height);
            CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);
            imageBox1.Image = magnitude;

            Mat mag = magnitude.Mat;
            //Wrong...............Wrong
            Image<Bgr, byte> imageHogInput = magnitude.Convert<Bgr, byte>();
            RangeF a = mag.GetValueRange();
            label2.Text = "Max: " + a.Max.ToString() + "  Min: " + a.Min.ToString();
            
            HOGDescriptor ho = new HOGDescriptor();
            float[] desc = new float[3780];
            desc = GetVector(imageHogInput);
            string fra = "";
            label3.Text = desc.Length.ToString();// desc.GetValue(10).ToString();
            System.IO.File.WriteAllText(@"des.txt", desc.Length.ToString());
            /*for (int i = 0; i< desc.Length; i++)
            {
                fra = "Frame Number: " + i + " Value " + desc.GetValue(i).ToString() + Environment.NewLine;
                System.IO.File.WriteAllText(@"des.txt", fra);
            }*/

        }

        private  void cameraInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.File.WriteAllText(@"g.txt", " ");
            /* if(capture==null)
             {
                 capture = new Emgu.CV.VideoCapture(0);
             }
             capture.ImageGrabbed += Capture1_ImageGrabbed;
             capture.Start();*/
            try
            {
                capture = new VideoCapture();
            }
            catch(NullReferenceException exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }
            Application.Idle += new EventHandler(ProcessFunction);
            captureProcess = true;

        }

        void ProcessFunction(object sender, EventArgs e)
        {
            int frameNumber = 2000;

            if (!capture.QueryFrame().IsEmpty) { }
                
            if (!capture.QueryFrame().IsEmpty)
            {
                Mat matInput = capture.QueryFrame();
                float[] smoothgrad = new float[(int)frameNumber];
                if (!matInput.IsEmpty)
                {
                    modulePreProcessing(matInput, smoothgrad);
                }

                
            }
        }

        private void playPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(captureProcess == true)
            {
                Application.Idle -=ProcessFunction;
                captureProcess = false;
                playPauseToolStripMenuItem.Text = "Play";
            }
            else
            {
                Application.Idle += ProcessFunction;
                captureProcess = true;
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
        CvInvoke.FindContours(imageSobelInput, countour, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);
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
           // CvInvoke.Rectangle(imageSobelInput, rect, new MCvScalar(255, 0, 0));
        }*/
        #endregion

    }
}


