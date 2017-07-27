using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Timers;

namespace WpfApp1Kinect
{
    public partial class MainWindow : Window
    {
        #region Declarations
        KinectSensor sensor = null;

        MultiSourceFrameReader reader;

        Body[] bodies = null;

        Timer timer = null;

        LinkedList<Gesture> gestures = new LinkedList<Gesture>();

        float LastRightX, CurrentRightX= 0.0f, LastRightY, CurrentRightY = 0.0f;
        float LastLeftX, CurrentLeftX = 0.0f, LastLeftY, CurrentLeftY = 0.0f;

        readonly int ACTION_THRESHOLD_PX = 300;
        readonly int CHECK_INTERVAL_MS = 300;
        #endregion

        #region Boilerplate

        public MainWindow()
        { 
            InitializeComponent();
            Loaded += Window_Loaded;
            Closed += Window_Closed;  
        }
        
        //Performed when Window has finished loading
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();

            gestures.AddLast(new Gesture());

            timer = new Timer(CHECK_INTERVAL_MS);
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            //Open sensor, if found.
            if (sensor != null)
            {
                sensor.Open();

                //We will be using BodyFrame and ColorFrame. So, best to use MultiSourceFrameReader.
                reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
                reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            float movedRX = CurrentRightX - LastRightX, movedRY = CurrentRightY - LastRightY;
            float movedLX = CurrentLeftX - LastLeftX, movedLY = CurrentLeftY - LastLeftY;

            if (Math.Abs(movedRX) > ACTION_THRESHOLD_PX || Math.Abs(movedRY) > ACTION_THRESHOLD_PX)
                gestures.AddLast(new Gesture(LastRightX, CurrentRightX, LastRightY, CurrentRightY, CHECK_INTERVAL_MS));
            else if (Math.Abs(movedLX) > ACTION_THRESHOLD_PX || Math.Abs(movedLY) > ACTION_THRESHOLD_PX)
                gestures.AddLast(new Gesture(LastLeftX, CurrentLeftX, LastLeftY, CurrentLeftY, CHECK_INTERVAL_MS));
            
            LastRightX = CurrentRightX;
            LastRightY = CurrentRightY;
            LastLeftY = CurrentLeftY;
            LastLeftX = CurrentLeftX;
        }

        //Performed when the window is closed
        private void Window_Closed(object sender, EventArgs e)
        {
            //Close all instances of IDisposable<> objects
            if (reader != null)
                reader.Dispose();
            if (sensor != null)
                sensor.Close();
            if (timer != null)
                timer.Close();
        }
        #endregion

        #region FrameArrived

        //Find the body, and track hand movements.
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame refer = e.FrameReference.AcquireFrame();

            bool dataRecieved = false;

            canvas.Children.Clear();
            using (ColorFrame frame = refer.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = ToBitmap(frame);
                    canvas.Children.Add(camera);
                }
            }
            using (BodyFrame bFrame = refer.BodyFrameReference.AcquireFrame())
            {
                if (bFrame != null)
                {
                    bodies = new Body[bFrame.BodyCount];
                    bFrame.GetAndRefreshBodyData(bodies);
                    if(bodies.Length > 0)
                        dataRecieved = true;
                }
                if(dataRecieved)
                {
                    bool bodyFound = false;
                    Body bodyInFocus = bodies[0];
                    float closestDepth = float.MaxValue;
                    byte bodiesFound = 0;
                    
                    foreach(Body b in bodies)
                    {
                        if (b.IsTracked)
                        {
                            bodyFound = true; bodiesFound++;

                            IReadOnlyDictionary<JointType, Joint> joints = b.Joints;

                            if (joints[JointType.SpineBase].Position.Z < closestDepth)
                            {
                                closestDepth = joints[JointType.SpineBase].Position.Z;
                                bodyInFocus = b;
                            }
                            else continue; //Ignore this body if it's not the closest

                            Joint handLeft = joints[JointType.HandLeft],
                                  handRight = joints[JointType.HandRight],
                                  thumbRight = joints[JointType.ThumbRight],
                                  thumbLeft = joints[JointType.ThumbLeft];

                            Point lhPoint = DrawJointMarker(handLeft, 50, sensor.CoordinateMapper),
                                  rhPoint = DrawJointMarker(handRight, 50, sensor.CoordinateMapper),
                                  rtPoint = DrawJointMarker(thumbRight, 20, sensor.CoordinateMapper),
                                  ltPoint = DrawJointMarker(thumbLeft, 20, sensor.CoordinateMapper);

                            CurrentRightX = (float)rhPoint.X;
                            CurrentRightY = (float)rhPoint.Y;

                            CurrentLeftX = (float)lhPoint.X;
                            CurrentLeftY = (float)lhPoint.Y;

                            string rhs = getHandState(b.HandRightState),
                                   lhs = getHandState(b.HandLeftState);

                            RHInfo.Text = "RIGHT HAND \n" + rhs + "\nX: " + rhPoint.X.ToString("#.##") + "\nY: " + rhPoint.Y.ToString("#.##"); //RHInfo is a XAML UIElement!
                            LHInfo.Text = "LEFT HAND  \n" + lhs + "\nX: " + lhPoint.X.ToString("#.##") + "\nY: " + lhPoint.Y.ToString("#.##");

                            ThumbsInfo.Text = "THUMBS \n" + "R: (" + rtPoint.X.ToString("#.#") + ", " + rtPoint.Y.ToString("#.#") + ")\n" +
                                              "L: (" + ltPoint.X.ToString("#.#") + ", " + ltPoint.Y.ToString("#.#") + ")";

                            BodiesInFocus.Text = "TARGET ID \n" + bodyInFocus.TrackingId + "\nOther Bodies Found: " + (bodiesFound - 1) + "\nLast Action: " + gestures.Last.Value.direction;
                        } 
                        if (!bodyFound)
                        {
                            BodiesInFocus.Text = "TARGET ID \nNONE!";
                            RHInfo.Text = "RIGHT HAND \nNONE!";
                            LHInfo.Text = "LEFT HAND \nNONE!";
                            ThumbsInfo.Text = "THUMBS\nNONE!";
                        }
                            
                    }
                }
            }
            
            
        } 

        #endregion
        
        #region Operations
        public static string getHandState(HandState state)
        {
            string handState = "-";
            switch (state)
            {
                case HandState.Open: handState = "Open"; break;

                case HandState.Closed: handState = "Closed"; break;

                case HandState.NotTracked: handState = "Not Tracked"; break;

                case HandState.Lasso: handState = "Lasso"; break;

                case HandState.Unknown: handState = "Unknown"; break;

                default: break;
            }
            return handState;
        }

        //Purpose: Take a color frame and convert to Bitmap image, to be displayed to screen.
        private ImageSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width, height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

        }
        
        //Generic Drawing Function for hand or thumb.
        private Point DrawJointMarker(Joint joint, int diameter, CoordinateMapper map)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return new Point();

            Point point = Scale(joint, map);

            Ellipse ellipse = new Ellipse
            { 
                //Using Auto-Implemented Properties 
                Width = diameter,
                Height = diameter,
                Fill = new SolidColorBrush(Colors.DarkRed)
            };   

            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);

            return point;
        }

        //Return the pixel XY coordinate of a Joint using CoordinateMapper
        public static Point Scale(Joint joint, CoordinateMapper map)
        {
            Point point = new Point();

            ColorSpacePoint cPoint = map.MapCameraPointToColorSpace(joint.Position);

            if (Double.IsInfinity(cPoint.X)) point.X = 0;
            else point.X = cPoint.X;

            if (Double.IsInfinity(cPoint.Y)) point.Y = 0;
            else point.Y = cPoint.Y;

            return point;
        }

        #endregion
    }

}
