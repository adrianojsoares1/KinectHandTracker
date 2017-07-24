using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WpfApp1Kinect
{
    public partial class MainWindow : Window
    {
        #region Declarations
        KinectSensor sensor = null;

        ColorFrameReader colorReader = null;

        BodyFrameReader bodyReader = null;

        Body[] bodies = null;

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

            //Open sensor, if found.
            if (sensor != null)
            {
                sensor.Open();

                //Call the body reader. Every Reader must have a .FrameArrived method.
                bodyReader = sensor.BodyFrameSource.OpenReader();
                bodyReader.FrameArrived += BodyReader_FrameArrived;

                //Call the color reader
                colorReader = sensor.ColorFrameSource.OpenReader();
                colorReader.FrameArrived += ColorReader_FrameArrived;


            }
        }

        //Performed when the window is closed
        private void Window_Closed(object sender, EventArgs e)
        {
            //Close all instances of IDisposable<> objects
            if (colorReader != null)
                colorReader.Dispose();
            if (bodyReader != null)
                bodyReader.Dispose();
            if (sensor != null)
                sensor.Close();
        }
        #endregion

        #region FrameArrived

        //Display the camera image of the person to the screen.
        private void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = ToBitmap(frame);
                }
            }
        }

        //Find the body, and track hand movements.
        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataRecieved = false;
            using (BodyFrame bFrame = e.FrameReference.AcquireFrame())
            {
                if (bFrame != null)
                {
                    bodies = new Body[bFrame.BodyCount];
                    bFrame.GetAndRefreshBodyData(bodies);
                    dataRecieved = true;
                }
                if(dataRecieved)
                {
                    foreach(Body b in bodies)
                    {
                        if (b.IsTracked)
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = b.Joints;

                            Joint handLeft = joints[JointType.HandLeft];
                            Joint handRight = joints[JointType.HandRight];

                            Point lhPoint = DrawHand(LeftHand, handLeft, sensor.CoordinateMapper);
                            Point rhPoint = DrawHand(RightHand, handRight, sensor.CoordinateMapper);

                            string rhs = getHandState(b.HandRightState);
                            string lhs = getHandState(b.HandLeftState);

                            RHInfo.Text = "RIGHT HAND \n" + rhs + "\nX: " + rhPoint.X.ToString("#.##") + "\nY: " + rhPoint.Y.ToString("#.##"); //RHInfo is a XAML UIElement!
                            LHInfo.Text = "LEFT HAND  \n" + lhs + "\nX: " + lhPoint.X.ToString("#.##") + "\nY: " + lhPoint.Y.ToString("#.##");

                            Joint thumbRight = joints[JointType.ThumbRight];
                            Joint thumbLeft = joints[JointType.ThumbLeft];

                            Point rtPoint = DrawThumb(RightThumb, thumbRight, sensor.CoordinateMapper);
                            Point ltPoint = DrawThumb(LeftThumb, thumbLeft, sensor.CoordinateMapper);

                            ThumbsInfo.Text = "THUMBS \n" + "R: (" + rtPoint.X.ToString("#.#") + ", " + rtPoint.Y.ToString("#.#") + ")\n" +
                                              "L: (" + ltPoint.X.ToString("#.#") + ", " + ltPoint.Y.ToString("#.#") + ")";

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
        private static Point DrawLimb(Ellipse ellipse, Joint hand, CoordinateMapper map)
        {
            if (hand.TrackingState == TrackingState.NotTracked) return new Point();

            Point point = Scale(hand, map);

            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            return point;
        }

        //Wrapper Function, draws circle around the thumb.
        public static Point DrawThumb(Ellipse ellipse, Joint thumb, CoordinateMapper map)
        {
           return DrawLimb(ellipse, thumb, map);
        }

        //Wrapper Function, draws circle around the hand.
        public static Point DrawHand(Ellipse ellipse, Joint hand, CoordinateMapper map)
        {
            return DrawLimb(ellipse, hand, map);
        }

        //Return the pixel XY coordinate of a Joint using CoordinateMapper
        public static Point Scale(Joint joint, CoordinateMapper map)
        {
            Point point = new Point();

            ColorSpacePoint cPoint = map.MapCameraPointToColorSpace(joint.Position);

            if (double.IsInfinity(cPoint.X)) point.X = 0;
            else point.X = cPoint.X;

            if (double.IsInfinity(cPoint.Y)) point.Y = 0;
            else point.Y = cPoint.Y;

            return point;
        }

        #endregion
    }

}
