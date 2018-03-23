using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using System.Speech.Synthesis;


namespace Example
{
    public partial class Form1 : Form
    {
        #region declaration
        VideoWriter VideoW;
        SVM svm = new SVM();
        ANN_MLP ann = new ANN_MLP();
        int indexOfResponse = 0;
        char[] labelArray = new char[] {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k'};
        string frameName;
        Image<Bgr, byte> _imgInput;
        int frameNumber = 1;
        int first = -1;
        int last = -1;
        VideoCapture capture;
        VideoCapture captureFeature;
        Boolean Pause = false;
        Boolean captureProcess = false;
        Boolean isFirst = false;
        Boolean isLast = true;
        Matrix<float> featureOfSample = new Matrix<float>(16, 16) { };
        Matrix<float> allFeatureOfSample = new Matrix<float>(416, 16) {};
        Matrix<int> svmResponse = new Matrix<int>(16, 1) { };
        Matrix<int> annResponse = new Matrix<int>(16, 26) { };
        Matrix<int> svmAllResponse = new Matrix<int>(416, 1) {};
        Matrix<float> annAllResponse = new Matrix<float>(416, 26) { };
        string svmData = "";
        string annData = "";
        int keyFrameNumber = 0;

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
                                    FourCC.H264/*VideoWriter.Fourcc('M','J','P','G')Convert.ToInt32(capture.GetCaptureProperty(CapProp.FourCC))*/,
                                    30,
                                    new Size(capture.Width, capture.Height),
                                    true);
                while (!Pause)
                {
                    Mat matInput = new Mat();
                    capture.Read(matInput);
                    if (!matInput.IsEmpty)
                    {
                        moduleKeyFrameExtraction(matInput);
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

        private void moduleKeyFrameExtraction(Mat inputMat)
        {

            Image<Bgr, Byte>  imageMedianBlur = modulePreProcessing(inputMat);
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
            label2.Text = "Frame Count = " + frameNumber.ToString();
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
                    int mid = (first + last) / 2;
                    moduleFeatureExtraction(first, last);
                    isFirst = false;
                }
            }
            frameNumber++;
        }
        
        private Image<Bgr, byte> modulePreProcessing( Mat inputMat)
        {

            //Actual video is played in pictureBox1
            Image<Bgr, Byte> imageInput = inputMat.ToImage<Bgr, Byte>();
            pictureBox1.Image = imageInput.Bitmap;


            //Face elimination
            imageInput = inputMat.ToImage<Bgr, Byte>();
            Image<Gray, byte> grayframe = imageInput.Convert<Gray, byte>();
            CascadeClassifier face = new CascadeClassifier("C:\\Emgu\\emgucv-windesktop 3.3.0.2824\\opencv\\data\\haarcascades\\haarcascade_frontalface_default.xml");
            var faces = face.DetectMultiScale(grayframe, 1.1, 25, new Size(10, 10));
            foreach (var f in faces)
            {
                Rectangle faceRectangle = Rectangle.Inflate(f, 40, 40);
                imageInput.Draw(faceRectangle, new Bgr(Color.Black), -1);
            }
            Mat faceEliminationMat = imageInput.Mat;
            Image<Bgr, Byte> faceElimination = faceEliminationMat.ToImage<Bgr, Byte>();
            pictureBox2.Image = faceElimination.Bitmap; //Face elimination


            //Skin area detection
            
            skinAreaDetection(imageInput.Bitmap);
            imageBox1.Image = imageInput;


            Image<Bgr, Byte> imageMedianBlurForExtraction = new Image<Bgr, Byte>(inputMat.Width, inputMat.Height);
            CvInvoke.MedianBlur(imageInput, imageMedianBlurForExtraction, 7);
            imageBox2.Image = imageMedianBlurForExtraction; //Noise Removing
            Image<Bgr, Byte> real = resize(imageMedianBlurForExtraction);
            frameName = "gesture\\" + frameNumber + ".jpeg";
            real.Save(frameName);

            //MedianBlur for KeySize(imageMedianBlur.Width, imageMedianBlur.Height) Frame Extraction
            Image<Bgr, Byte> imageMedianBlur = new Image<Bgr, Byte>(inputMat.Width, inputMat.Height);
            CvInvoke.MedianBlur(imageInput, imageMedianBlur, 21);
            return imageMedianBlur;
        }

        private async void moduleFeatureExtraction(int first,int last)
        {
            
            double[,] RawData = new double[16, 3780];
            int mid = (first + last) / 2;
            int low = mid - 8; ;
            int high = mid + 8;

            //svmResponse generation
            
            for (int i = 0, j = 0; i < 16; i++)
            {
                svmResponse[i, j] = indexOfResponse;
            }

            //annResponse generation
            
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 26; j++)
                {
                    if (j == indexOfResponse)
                        annResponse[i, j] = 1;
                    if (j != indexOfResponse)
                        annResponse[i, j] = 0;
                }
                
            }

            if (low < first)
                low++;
            if (high > last)
                low++;
            int length = high - low;
            int k;
            for (k = (low); k < (high); k++)
            {
                string frameName = "gesture//" + k + ".jpeg";
                Image<Bgr, byte> featurExtractionInput = new Image<Bgr, byte>(frameName);
                string keyFrameName = "keyframes//" + keyFrameNumber + ".jpeg";
                featurExtractionInput.Save(keyFrameName);
                pictureBox3.Image = featurExtractionInput.Bitmap;
                await Task.Delay(1000 / Convert.ToInt32(2));
                float[] desc = new float[3780];
                desc = GetVector(featurExtractionInput);

                int i = k - (low);
                for (int j = 0; j < 3780; j++)
                {
                    double val = Convert.ToDouble(desc[j]);
                    RawData.SetValue(val, i, j);
                }
            }
            if (k == high)
            {
                Matrix<Double> DataMatrix = new Matrix<Double>(RawData);
                Matrix<Double> Mean = new Matrix<Double>(1, 3780);
                Matrix<Double> EigenValues = new Matrix<Double>(1, 3780);
                Matrix<Double> EigenVectors = new Matrix<Double>(3780, 3780);
                CvInvoke.PCACompute(DataMatrix, Mean, EigenVectors, 100);
                Matrix<Double> result = new Matrix<Double>(10, 16);
                CvInvoke.PCAProject(DataMatrix, Mean, EigenVectors, result);

                
                
                featureOfSample = result.Convert<float>();
                for(int p=0; p<16; p++)
                {
                    svmData += "Response:   " + svmResponse[p, 0].ToString() + "   Feature:    ";
                    //annData += "Response:   " + svmResponse[p, 0].ToString() + "   Feature:    ";
                    svmAllResponse[(((indexOfResponse) * 16) + p), 0] = svmResponse[p, 0];


                    annData += "Response:   ";
                    for (int q = 0; q < 26; q++)
                    {
                        annAllResponse[((indexOfResponse *16) + p), q] = annResponse[p, q];
                        annData +=  annResponse[p, q].ToString() + ", ";
                    }
                    annData += " Feature:    "; 

                    for (int q = 0; q < 16; q++)
                    {
                        allFeatureOfSample[((indexOfResponse*16) + p), q] = featureOfSample[p, q];
                        annData += featureOfSample[p, q].ToString() + ",  ";
                        svmData += featureOfSample[p, q].ToString() + ",  ";
                    }
                    annData += Environment.NewLine;
                    svmData += Environment.NewLine;
                }
                System.IO.File.WriteAllText(@"SVMTrainingData.txt", svmData);
                System.IO.File.WriteAllText(@"ANNTrainingData.txt", annData);
                
                if(indexOfResponse>0)
                {
                    svmTraining();
                    //annTraining();
                }
                indexOfResponse++;
            }
        }

        private void svmTraining()
        {
            string finalOutput = "";
            //TrainData SvmTrainData = new TrainData(allFeatureOfSample, DataLayoutType.RowSample, svmAllResponse);
            //svm.SetKernel(Emgu.CV.ML.SVM.SvmKernelType.Linear);
            //svm.Type = Emgu.CV.ML.SVM.SvmType.CSvc;
            //svm.C = 1;
            //svm.TermCriteria = new MCvTermCriteria(100, 0.00001);
            //svm.Train(SvmTrainData);
            //bool trained = svm.TrainAuto(SvmTrainData, 2);
            //svm.Save("SVM_Model.xml");
            //FileStorage fileStorageWrite = new FileStorage(@"SVM_Model.xml", FileStorage.Mode.Write);
            //svm.Write(fileStorageWrite);


            FileStorage fileStorageWrite = new FileStorage(@"SVM_Model.xml", FileStorage.Mode.Read);
            svm.Read(fileStorageWrite.GetFirstTopLevelNode());

            
            Matrix<float> testSample = new Matrix<float>(1, 16);
            for (int q = 0; q < 16; q++)
            {
                testSample[0, q] = allFeatureOfSample[27, q];
            }
            float real = svm.Predict(testSample);

            finalOutput += labelArray[(int)real];
            label5.Text = finalOutput.ToString();
            SpeechSynthesizer reader1 = new SpeechSynthesizer();


            if (label5.Text != " ")
            {
                reader1.Dispose();
                reader1 = new SpeechSynthesizer();
                reader1.SpeakAsync(finalOutput.ToString());
            }
            else
            {
                MessageBox.Show("No Text Present!");
            }

            System.IO.File.WriteAllText(@"SVMResult.txt", real.ToString());
        }

        private void annTraining()
        {
            string finalOutput = "";
            int features = 16;
            int classes = 26;
            Matrix<int> layers = new Matrix<int>(6, 1);
            layers[0, 0] = features;
            layers[1, 0] = classes * 16;
            layers[2, 0] = classes * 8;
            layers[3, 0] = classes * 4;
            layers[4, 0] = classes * 2;
            layers[5, 0] = classes;

            FileStorage fileStorageRead = new FileStorage(@"ANN_Model.xml", FileStorage.Mode.Read);
            ann.Read(fileStorageRead.GetRoot(0));
            ann.SetLayerSizes(layers);
            ann.SetActivationFunction(ANN_MLP.AnnMlpActivationFunction.SigmoidSym, 0, 0);
            ann.SetTrainMethod(ANN_MLP.AnnMlpTrainMethod.Backprop, 0, 0);
            ann.Train(allFeatureOfSample, DataLayoutType.RowSample, annAllResponse);

            FileStorage fileStorageWrite = new FileStorage(@"ANN_Model.xml", FileStorage.Mode.Write);
            ann.Write(fileStorageWrite);

            Matrix<float> testSample = new Matrix<float>(1, 16);
            for (int q = 0; q < 16; q++)
            {
                testSample[0, q] = allFeatureOfSample[12, q];
            }
            float real = ann.Predict(testSample);

            finalOutput += labelArray[(int)real];
            label5.Text = finalOutput.ToString();
            SpeechSynthesizer reader1 = new SpeechSynthesizer();


            if (label5.Text != " ")
            {
                reader1.Dispose();
                reader1 = new SpeechSynthesizer();
                reader1.SpeakAsync(finalOutput.ToString());
            }
            else
            {
                MessageBox.Show("No Text Present!");
            }

            System.IO.File.WriteAllText(@"ANNResult.txt", real.ToString());
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause = !Pause;
        }

        private void openVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            System.IO.File.WriteAllText(@"SVMTrainingData.txt", " ");
            System.IO.File.WriteAllText(@"ANNTrainingData.txt", " ");
            System.IO.File.WriteAllText(@"SVMResult.txt", " ");
            System.IO.File.WriteAllText(@"ANNResult.txt", " ");
            //ofd.filter
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                frameNumber = 1;
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
            Mat matInput = _imgInput.Mat;
            moduleKeyFrameExtraction(matInput);
        }

        public Image<Bgr, Byte> resize(Image<Bgr, Byte> im)
        {
            return im.Resize(64, 128, Emgu.CV.CvEnum.Inter.Linear);
        }

        public float[] GetVector(Image<Bgr, Byte> imageOfInterest)
        {
            HOGDescriptor hog = new HOGDescriptor();    // with defaults values
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
            Mat matInput = capture.QueryFrame();
            if (!capture.QueryFrame().IsEmpty)
            { 
                float[] smoothgrad = new float[(int)frameNumber];
                if (!matInput.IsEmpty)
                {
                    moduleKeyFrameExtraction(matInput);
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

        private void speechToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpeechSynthesizer reader = new SpeechSynthesizer();


            if (label5.Text != " ")
            {
                reader.Dispose();
                reader = new SpeechSynthesizer();
                reader.SpeakAsync(label5.Text);
            }
            else
            {
                MessageBox.Show("No Text Present!");
            }

        }

        private void sVMTPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            svmTraining();
        }

        private void aNNTPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            annTraining();
        }

        #region extra

            /* String filePath = @"test.xml";
                    StringBuilder sb = new StringBuilder();
                    (new XmlSerializer(typeof(Matrix<double>))).Serialize(new StringWriter(sb), result);
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml(sb.ToString());

                    System.IO.File.WriteAllText(filePath, sb.ToString());
                    Matrix<double> matrix = (Matrix<double>)(new XmlSerializer(typeof(Matrix<double>))).Deserialize(new XmlNodeReader(xDoc));

                    string djf = null;
                    djf = System.IO.File.ReadAllText(@"g.txt");
                    djf += Environment.NewLine;
                    djf += Environment.NewLine;
                    for (int p = 0; p < 16; p++)
                    {
                        for (int q = 0; q < 100; q++)
                        {
                            djf += p + " , " + q + "  " + matrix[p, q].ToString() + "    ";
                        }
                        djf += Environment.NewLine;
                    }*/

            /*int features = 100;
                    int classes = 26;
                    Matrix<int> layers = new Matrix<int>(6, 1);
                    layers[0, 0] = features;
                    layers[1, 0] = classes * 16;
                    layers[2, 0] = classes * 8;
                    layers[3, 0] = classes * 4;
                    layers[4, 0] = classes * 2;
                    layers[5, 0] = classes;*/
            //ANN_MLP ann = new ANN_MLP();

            //FileStorage fileStorageRead = new FileStorage(@"abc.csv", FileStorage.Mode.Read);
            //ann.Read(fileStorageRead.GetRoot(0));
            //svm.Read(fileStorageRead.GetRoot(0));
            //ann.SetLayerSizes(layers);
            //ann.SetActivationFunction(ANN_MLP.AnnMlpActivationFunction.SigmoidSym, 0, 0);
            //ann.SetTrainMethod(ANN_MLP.AnnMlpTrainMethod.Backprop, 0, 0);
            //ann.Train(featureOfSample, DataLayoutType.RowSample, svmResponse);


            /*SVM svm = new SVM();
            FileStorage fileStorageRead = new FileStorage(@"abc.xml", FileStorage.Mode.Read);
            svm.Read(fileStorageRead.GetRoot(0));
            svm.TrainAuto(trainData);

                        FileStorage fileStorageWrite = new FileStorage(@"abc.xml", FileStorage.Mode.Write);
            svm.Write(fileStorageWrite);
                        Matrix<float> testSample = new Matrix<float>(1, 16);
                        for (int q = 0; q< 16; q++)
                        {
                            testSample[0, q] = featureOfSample[11, q];
                        }
        float real = svm.Predict(testSample);



        finalOutput += labelArray[(int)real];

            */
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