using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Bitness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The Kinect Sensor That We're Getting Data From
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// The pixeldata for the color frame
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// The bitmap that we'll write the color frame to
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Array of current skeletons
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// Drawing group for skeleton render
        /// </summary>
        private DrawingGroup drawingGroup;

        private Human _human;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.drawingGroup = new DrawingGroup();

            Skeleton.Source = new DrawingImage(this.drawingGroup);

            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    // Get the first sensor
                    this.sensor = sensor;
                    break;
                }
            }

            // If we have a sensor picked out
            if (this.sensor != null)
            {
                // Enable the skeleton frame sensor
                this.sensor.SkeletonStream.Enable();

                // Add the event handler to the sensor listener
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Enable the sensor's color stream
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space for pixel data
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                    this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.Camera.Source = this.colorBitmap;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Init the sensor
                try
                {
                    this.sensor.Start();
                }
                catch(System.IO.IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    this.sensor = null;
                }
            }

        }

        /// <summary>
        /// Event handler for when the skeleton frame is ready
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    this.skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(this.skeletons);
                    this._human = new Human(this.skeletons[0]);
                }
                else
                {
                    this.skeletons = null;
                }
            }

            drawSkeletons();
        }
        
        /// <summary>
        /// Draws the skeletons on the screen
        /// </summary>
        private void drawSkeletons()
        {
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.colorBitmap.PixelWidth, 
                    this.colorBitmap.PixelHeight));

                if (skeletons != null)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.drawJoints(dc, skel);
                        }
                    }
                    //this._human.CalcAngles();
                }

            }
        }

        /// <summary>
        /// Draws the joints of the given skeleton on the screen
        /// </summary>
        /// <param name="dc">The drawing context of the screen</param>
        /// <param name="skel">The skeleton of the joints to draw</param>
        private void drawJoints(DrawingContext dc, Skeleton skel)
        {
            foreach (Joint joint in skel.Joints)
            {
                if(joint.TrackingState == JointTrackingState.Tracked)
                {
                    Brush drawBrush = new SolidColorBrush(Color.FromArgb(255, 100, 149, 237));

                    dc.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), 10.0, 10.0);
                }
            }
        }

        /// <summary>
        /// Converts a skeleton point to coordinates of that point on the screen
        /// </summary>
        /// <param name="skelpoint">Point to convert</param>
        /// <returns>Point on screen</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint,
                DepthImageFormat.Resolution640x480Fps30);

            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Event handler when the color frame is ready on the sensor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;

                // Copy the color frame data to the texture
                colorFrame.CopyPixelDataTo(this.colorPixels);
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels, this.colorBitmap.PixelWidth * sizeof(Int32), 0);
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Safely shutdown the sensor
            if (this.sensor != null)
            {
                this.sensor.Stop();
            }
        }


    }
}
