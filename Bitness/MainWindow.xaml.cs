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


        public MainWindow()
        {
            InitializeComponent();
        }


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
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
