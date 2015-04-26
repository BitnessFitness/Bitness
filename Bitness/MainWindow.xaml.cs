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
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage bodySource;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// The body frame reader for skeletons and stuff
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        private int[] ROCKET_X = new int[2] { 105, 105 };

        /// <summary>
        /// List of all of our bodies.
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Small bit of text in the bottom right corner of the screen
        /// </summary>
        private string statusText = "Nothing has happened yet!";

        /// <summary>
        /// ints used to raise rectngles and gifs for now
        /// </summary>
        public int numJacks = 0;
        public int numRaise = 1;
        public int numJacks2 = 0;
        public int numRaise2= 1;

        private FloorWindow floor;

        private List<Action> exercises;

        /// <summary>
        /// Gets the skeleton points to display
        /// </summary>
        public ImageSource BodySource
        {
            get
            {
                return this.bodySource;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public string StatusText
        {
            get
            {
                return this.statusText;
            }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.floor = new FloorWindow();
            this.floor.Show();

            this.sensor = KinectSensor.GetDefault();
            this.colorFrameReader = this.sensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            FrameDescription colorFrameDescription = this.sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            FrameDescription depthFrameDescription = this.sensor.DepthFrameSource.FrameDescription;
            this.displayWidth = depthFrameDescription.Width;
            this.displayHeight = depthFrameDescription.Height;

            this.bodyFrameReader = this.sensor.BodyFrameSource.OpenReader();
            this.coordinateMapper = this.sensor.CoordinateMapper;
            this.drawingGroup = new DrawingGroup();
            this.bodySource = new DrawingImage(this.drawingGroup);

            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.sensor.Open();

            List<JointType> joints = new List<JointType>()
            {
                JointType.Head
            };

            // use the window object as the view model in this simple example
            this.DataContext = this;
            this.exercises = new List<Action>();
           
            this.InitializeComponent();

        }

        public void PlayVideo(object sender, RoutedEventArgs e)
        {
            // testVideo.Visibility = Visibility.Visible;
            // testVideo.Play();
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.sensor != null)
            {
                this.sensor.Close();
                this.sensor = null;
            }
        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            //Stores the # of bodies on the screen
            int bodyCounter = 0;


            //Line Trail for red rocket
            Line redRocketTrail = new Line();
            redRocketTrail.Stroke = System.Windows.Media.Brushes.OrangeRed;
            redRocketTrail.X1 = 125;
            redRocketTrail.Y1 = 61;
            redRocketTrail.Y2 = 61;
            Canvas.SetZIndex(redRocketTrail, -1);

            //Line Trail for blue rocket
            Line blueRocketTrail = new Line();
            blueRocketTrail.Stroke = System.Windows.Media.Brushes.Blue;
            blueRocketTrail.X1 = 125;
            blueRocketTrail.Y1 = 90;
            blueRocketTrail.Y2 = 90;
            Canvas.SetZIndex(blueRocketTrail, -2);

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    List<int> counts = new List<int>();

                    this.floor.DrawTopDownView(this.bodies);

                    for (int i = 0; i < this.bodies.Length; i++)
                    {
                        Pen drawPen = new Pen(Brushes.Red, 6);
                        Body body = this.bodies[i];

                        if (this.bodies[i].IsTracked == true)
                        {
                            //If a body is bring tracked in the bodies[] add to the body counter.
                            bodyCounter++;
                        }

                        if (bodyCounter == 2)
                        {
                            //Hide Standby Videos
                            redsideStandby.Visibility = Visibility.Hidden;
                            bluesideStandby.Visibility = Visibility.Hidden;
                            //Fuel Tubes
                            redFuelTube.Visibility = Visibility.Visible;
                            blueFuelTube.Visibility = Visibility.Visible;
                            redFuelBottom.Visibility = Visibility.Visible;
                            blueFuelBottom.Visibility = Visibility.Visible;

                            
                            //show the water gifs
                            gif_canvas_left.Visibility = Visibility.Visible;
                            gif_canvas_right.Visibility = Visibility.Visible;
                        }
                        else if(bodyCounter == 1)
                        {
                            //Red
                            redsideStandby.Visibility = Visibility.Visible;
                            redFuelTube.Visibility = Visibility.Hidden;
                            redFuelBottom.Visibility = Visibility.Hidden;
                            //Blue
                            bluesideStandby.Visibility = Visibility.Hidden;
                            blueFuelTube.Visibility = Visibility.Visible;
                            blueFuelBottom.Visibility = Visibility.Visible;

                        }
                        else
                        {
                            //Show Standby Videos
                            redsideStandby.Visibility = Visibility.Visible;
                            bluesideStandby.Visibility = Visibility.Visible;
                            //Fuel Tubes
                            redFuelTube.Visibility = Visibility.Hidden;
                            blueFuelTube.Visibility = Visibility.Hidden;
                            redFuelBottom.Visibility = Visibility.Hidden;
                            blueFuelBottom.Visibility = Visibility.Hidden;
                            //hides the gifs
                            //show the water gifs
                            gif_canvas_left.Visibility = Visibility.Hidden;
                            gif_canvas_right.Visibility = Visibility.Hidden;
                        }

                        if (i >= this.exercises.Count)
                        {
                            List<JointType> joints = new List<JointType>()
                            {
                                JointType.Head
                            };

                            this.exercises.Add(new JumpingJack(joints));
                        }

                        Action exercise = this.exercises[i];

                        if (body.IsTracked)
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;

                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            // here is where we check the exercise
                            bool repAdded = exercise.Update(body.Joints);
                            counts.Add(exercise.Reps);

                            if (repAdded && i < 2)
                            {
                                ROCKET_X[i] = (105 + (counts[i] * 20));
                                
                                
                                moveBar(i);
                            }

                        }

                    }

                    String message = "Jumping jacks: ";

                    for (int i = 0; i < counts.Count; i++)
                    {
                        message += "player #" + i + ": " + counts[i] + ". ";
                        //Set a new red rocket X for each jump
                        if (i == 0)
                        {
                            Canvas.SetLeft(redRocket, ROCKET_X[i]);

                            //Change the 2nd X position for the trail and add it to the canvas
                            redRocketTrail.X2 = (ROCKET_X[i] + 10);
                            topBarCanvas.Children.Add(redRocketTrail);

                        }
                        else
                        {
                            Canvas.SetLeft(blueRocket, ROCKET_X[i]);

                            //Change the 2nd X position for the trail and add it to the canvas
                            blueRocketTrail.X2 = (ROCKET_X[i] + 10);
                            topBarCanvas.Children.Add(blueRocketTrail);
                        }
                    }

                    this.StatusText = message;

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], 3, 3);
                }
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        private void PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine((sender as FrameworkElement).Tag + " Preview");
        }
        
        private void MouseDown(object sender, MouseButtonEventArgs e)
        {

            moveBar(1);
            moveBar(0);
            
        }
        void moveBar(int index)
        {
            System.Windows.Shapes.Rectangle rect;
            System.Windows.Shapes.Rectangle rect2;
            rect = new System.Windows.Shapes.Rectangle();
            rect2 = new System.Windows.Shapes.Rectangle();


            if (index == 0)
            {
                numJacks = numJacks + 13;
                // Add a rectangle Element
                rect.Stroke = new SolidColorBrush(Colors.LightBlue);
                rect.Fill = new SolidColorBrush(Colors.LightBlue);
                rect.Width = 80;
                rect.Height = numJacks;
                Canvas.SetLeft(rect, 0);
                Canvas.SetBottom(rect, 0);
                left_canvas.Children.Add(rect);
                gif_canvas_left.Margin = new Thickness(0, -13 * numRaise, 0, 0);
                numRaise++;
            }

            if (index == 1)
            {
                numJacks2 = numJacks2 + 13;
                // Add a rectangle Element
                rect2.Stroke = new SolidColorBrush(Colors.Orange);
                rect2.Fill = new SolidColorBrush(Colors.Orange);
                rect2.Width = 80;
                rect2.Height = numJacks2;
                Canvas.SetLeft(rect2, 0);
                Canvas.SetBottom(rect2, 0);
                right_canvas.Children.Add(rect2);
                gif_canvas_right.Margin = new Thickness(0, -13 * numRaise2, 0, 0);
                numRaise2++;
            }
            

           
            
            
        }


        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
        }
    }
}
