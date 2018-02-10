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
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Cuda;
using System.IO.MemoryMappedFiles;
//using Emgu.CV.

namespace Example
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> _imgInput;

        public Form1()
        {
            InitializeComponent();
        }
        int i=0;
        int fla = 0;
        char[] arrrr = new char[10] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j'}; 
        VideoCapture capture;
        VideoCapture _capture;
        Boolean Pause = false;

        /*private void rgbToHsv(Bitmap bitmap)
        {
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Image<Hsv, Byte> hsvImage = new Image<Hsv, Byte>(bitmap);
                    Hsv hsvColour = hsvImage[0, 0];

                    double imgHue = hsvColour.Hue;
                    double imgSat = hsvColour.Hue;
                    double imgVal = hsvColour.Hue;

                    //extract the hue and saturation channels
                    Image<Gray, Byte>[] channels = hsvImage.Split();
                    Image<Gray, Byte> imgHue = channels[0];
                    Image<Gray, Byte> imgSat = channels[1];
                    Image<Gray, Byte> imgVal = channels[2];
                    if (imgHue > 0.55 || imgSat <= 0.20 || imgSat > 0.95)
                    {
                        bitmap.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    }
                    else
                    {
                        bitmap.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                    }
                }
            }
        }*/

        public bool skinarea(Bitmap b)
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
                        b.SetPixel(i, j, Color.FromArgb(r1, g1, b1));
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
                int frmno =1;
                
                double framenum = capture.GetCaptureProperty(CapProp.FrameCount);

                float[] smoothgrad = new float[(int)framenum];

                label1.Text = "Frame Count is : " + framenum.ToString();
                while (!Pause)
                {

                    Mat m = new Mat();
                    capture.Read(m);
                    if (!m.IsEmpty)
                    {

                        //Video is Played in PictureBox1
                        //Mat to Image Bgr
                        Image<Bgr, Byte> star = m.ToImage<Bgr, Byte>();
                        pictureBox1.Image = star.ToBitmap();


                        //skin area detection Mat
                        skinarea(m.Bitmap);

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
                            22,
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
                        //output in _imgout
                        Image<Bgr, Byte> _imgout = new Image<Bgr, Byte>(m.Width, m.Height);
                        CvInvoke.MedianBlur(imsrc, _imgout, 11);

                        //Picture Box 2 Image without face
                        pictureBox2.Image = _imgout.Bitmap;
                        //Mat m1 = imdest.Mat;

                        //contour
                        //_imgout to Gray imgeOrigenal
                        Image<Gray, Byte> imgeOrigenal = _imgout.Convert<Gray, Byte>();
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

                        string fra = System.IO.File.ReadAllText(@"C:\Users\rhiray1996\Desktop\hcbkdshgj.txt");
                        if(max_magitude.Max == 0)
                        {
                            if(fla == 0)
                            {
                                i++;
                                fla = 1;
                            }
                            fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString()+"                Next Gesture " + Environment.NewLine;
                        }
                        else
                        {
                            fla = 0;
                            //label2.Text = arrrr[i].ToString();
                            fra += "Frame Number: " + frmno + " Max: " + max_magitude.Max.ToString() + "  Min: " + max_magitude.Min.ToString() + Environment.NewLine;
                        }
                        frmno++;
                        System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\hcbkdshgj.txt", fra);

                        //histogram
                        /*histogramBox1.ClearHistogram();
                        histogramBox1.GenerateHistograms(magnitude, 36);
                        histogramBox1.Refresh();*/
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

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause = !Pause;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to close window?", "System Message", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void openVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            System.IO.File.WriteAllText(@"C:\Users\rhiray1996\Desktop\hcbkdshgj.txt", " ");
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

        private void cannyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_imgInput == null)
            {
                return;
            }

            Image<Gray, byte> _imgCanny = new Image<Gray, byte>(_imgInput.Width, _imgInput.Height);
            _imgCanny = _imgInput.Canny(150, 100);
            imageBox1.Image = _imgCanny;
        }

        private void skinDetectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_imgInput == null)
            {
                return;
            }
            skinarea(_imgInput.Bitmap);
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
            RangeF a = mag.GetValueRange();
            label2.Text = "Max: " + a.Max.ToString() + "  Min: " + a.Min.ToString();

            imageBox1.Refresh();
            histogramBox1.ClearHistogram();
            histogramBox1.GenerateHistograms(magnitude, 36);
            histogramBox1.Refresh();
            HOGDescriptor h1 = new HOGDescriptor();

        }

        private void histogramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_imgInput == null)
            {
                MessageBox.Show("Please Select an image !");
                return;
            }
            histogramBox1.ClearHistogram();
            histogramBox1.GenerateHistograms(_imgInput, 36);
            histogramBox1.Refresh();
        }

        private void hOGDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //HOGDescriptor(magnitude);
        }

        private  void cameraInputToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (_capture == null)
            {
                _capture = new VideoCapture();
            }
            try
            {
                _capture = null;
                _capture = new VideoCapture(0);
                /*_capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS, 30);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 240);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 320);
                Time_Label.Text = "Time: ";
                Codec_lbl.Text = "Codec: ";
                Frame_lbl.Text = "Frame: ";
                webcam_frm_cnt = 0;
                cam = 1;
                Video_seek.Value = 0;*/
                Application.Idle += ProcessFrame;
                /*button1.Text = "Stop";
                comboBox1.Enabled = false;*/
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }

        }

        private async void ProcessFrame(object sender, EventArgs e)
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
        }
        
    }
}


/*using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace Example
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> _imgInput;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            if (of.ShowDialog() == DialogResult.OK)
            {
                _imgInput = new Image<Bgr, byte>(of.FileName);
                imageBox1.Image = _imgInput;
            }
        }
    }


    public mat imgradient(mat grayscaleimage)     
    {         
        mat grad_x=new mat();         
        mat grad_y = new mat();         
        mat abs_grad_x=new mat();         
        mat abs_grad_y=new mat();                     
        mat gradientimag = new mat(grayscaleimage.rows(),grayscaleimage.cols(),cvtype.cv_8uc1);           
        imgproc.sobel(grayscaleimage, grad_x, cvtype.cv_16s, 1, 0,3,1,0,imgproc.border_default );          
        core.convertscaleabs( grad_x, abs_grad_x );                       
        imgproc.sobel( grayscaleimage, grad_y, cvtype.cv_16s, 0, 1, 3, 1,0,imgproc.border_default );          
        core.convertscaleabs( grad_y, abs_grad_y );                          
        double[] buff_grad = new double[1];          
        for(int = 0; < abs_grad_y.cols(); i++)             
        {                 
            for(int j =0 ; j<abs_grad_y.rows() ; j++)                 
            {                     
                double[] buff_x = abs_grad_x.get(j, i);                     
                double[] buff_y = abs_grad_y.get(j, i);                     
                double x =  buff_x[0];                     
                double y =  buff_y[0];                     
                double ans=0;                     
                try                     
                {                          
                    ans = math.sqrt(math.pow(x,2)+math.pow(y,2));                     
                }catch(nullpointerexception e)                     
                {                         
                    ans = 0;                      
                }                     
                buff_grad[0] =  ans;                                             
                gradientimag.put(j, i, buff_grad);                    
            }             
        }                    
        return gradientimag;     
    }






}*/
