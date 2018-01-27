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

        VideoCapture capture;
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

        public bool convertToGray(Bitmap b)
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
                    if (0.0 <= hue && hue <= 50.0 && 0.1 <= sat && sat <= 0.95 && r1 > 95 && g1 > 40 && b1 > 20 && r1 > g1 && r1 > b1 && Math.Abs(r1 - g1) > 15 && a1 > 15)
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
                double framenum = capture.GetCaptureProperty(CapProp.FrameCount);
                label1.Text = "Frame Count is : " + framenum.ToString();
                while (!Pause)
                {
                    Mat m = new Mat();
                    capture.Read(m);
                    if (!m.IsEmpty)
                    {
                        convertToGray(m.Bitmap);
                        /*Image<Gray, Byte> imgeOrigenal = m.ToImage<Gray, Byte>();
                        Image <Gray, byte> _imgCanny = new Image<Gray, byte>(m.Width, m.Height);
                        _imgCanny = imgeOrigenal.Canny(150, 100);
                        imageBox1.Image = _imgCanny;*/
                        Image<Gray, Byte> imgeOrigenal = m.ToImage<Gray, Byte>();
                        Image<Gray, float> _imgSobelx = new Image<Gray, float>(m.Width, m.Height);
                        Image<Gray, float> _imgSobely = new Image<Gray, float>(m.Width, m.Height);
                        _imgSobelx = imgeOrigenal.Sobel(1, 0, 3);
                        _imgSobely = imgeOrigenal.Sobel(0, 1, 3);
                        Image<Gray, float> magnitude = new Image<Gray, float>(m.Width, m.Height);
                        Image<Gray, float> angle = new Image<Gray, float>(m.Width, m.Height);
                        CvInvoke.CartToPolar(_imgSobelx, _imgSobely, magnitude, angle, true);
                        imageBox1.Image = magnitude;
                        imageBox1.Refresh();
                        histogramBox1.ClearHistogram();
                        histogramBox1.GenerateHistograms(magnitude, 36);
                        histogramBox1.Refresh();
                        pictureBox2.Image = m.Bitmap;
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
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                capture = new VideoCapture(ofd.FileName);
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
            convertToGray(_imgInput.Bitmap);
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
            imageBox1.Refresh();
            histogramBox1.ClearHistogram();
            histogramBox1.GenerateHistograms(magnitude, 36);
            histogramBox1.Refresh();
            HOGDescriptor h1 = new HOGDescriptor();
            
        }

        private void histogramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(_imgInput==null)
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
