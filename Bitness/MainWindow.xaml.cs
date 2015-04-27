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
        /// Refrence to left and right players 
        /// </summary>
        private Player redPlayer;
        private Player bluePlayer;

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
        public int numJacksLeft = 0;
        public int numRaiseLeft = 1;
        public int numJacksRight = 0;
        public int numRaiseRight = 1;

        //Custom Colors For Bitness
        public Color red = Color.FromRgb(241, 128, 33);
        public Color blue = Color.FromRgb(16, 177, 232);
        public Color fuelOrange = Color.FromRgb(219, 131, 35);

        private FloorWindow floor;

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

            redPlayer = new Player(null, new JumpingJack(joints));
            bluePlayer = new Player(null, new JumpingJack(joints));

            this.InitializeComponent();

            blueSyncVideo.Visibility = Visibility.Hidden;
            redSyncVideo.Visibility = Visibility.Hidden;

            leftSideBarCanvas.Visibility = Visibility.Hidden;
            rightSideBarCanvas.Visibility = Visibility.Hidden;

            //hides videos for blastoff
            BlastOffLeft.Visibility = Visibility.Hidden;
            BlastOffRight.Visibility = Visibility.Hidden;

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

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            //Stores the # of bodies on the screen
            int bodyCounter = 0;
            Body[] trackedBodies = new Body[2];

            #region InitTrails
            SolidColorBrush redBrush = new SolidColorBrush(red);
            SolidColorBrush blueBrush = new SolidColorBrush(blue);

            //Line Trail for red rocket
            Line redRocketTrail = new Line();
            redRocketTrail.Stroke = redBrush;
            redRocketTrail.X1 = 125;
            redRocketTrail.Y1 = 61;
            redRocketTrail.Y2 = 61;
            Canvas.SetZIndex(redRocketTrail, -1);

            //Line Trail for blue rocket
            Line blueRocketTrail = new Line();
            blueRocketTrail.Stroke = blueBrush;
            blueRocketTrail.X1 = 125;
            blueRocketTrail.Y1 = 90;
            blueRocketTrail.Y2 = 90;
            Canvas.SetZIndex(blueRocketTrail, -2);
            #endregion


            #region GetBodyData
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
            #endregion

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    List<int> counts = new List<int>();

                    this.floor.DrawTopDownView(this.bodies);

                    bool redPlayerDetected = false;
                    bool bluePlayerDetected = false;

                    #region LoopOverBodies
                    for (int i = 0; i < this.bodies.Length; i++)
                    {
                        Pen drawPen = new Pen(Brushes.Red, 6);
                        Body body = this.bodies[i];

                        if (body.IsTracked == true)
                        {
                            // if player is on red side
                            #region RedSide
                            if (body.Joints[JointType.SpineShoulder].Position.X > 0)
                            {
                                Console.WriteLine(redPlayer.state);
                                redPlayerDetected = true;
                                redPlayer.body = body;

                                if (redPlayer.state == Player.State.NOT_SYNCED)
                                {
                                    redPlayer.state = Player.State.SYNCING;
                                    showSync(false);
                                }
                                if (redPlayer.state == Player.State.SYNCED)
                                {
                                    bool repAdded = redPlayer.Update(body.Joints);
                                    if (repAdded)
                                    {
                                        ROCKET_X[0] = (105 + (redPlayer.Reps * 20));

                                        //moves bar based off index
                                        moveBar(0);
                                    }
                                }
                            }
                            #endregion
                            else
                            // if player is on blue side
                            #region BlueSide
                            {
                                bluePlayerDetected = true;
                                bluePlayer.body = body;
                                if (bluePlayer.state == Player.State.NOT_SYNCED)
                                {
                                    bluePlayer.state = Player.State.SYNCING;
                                    showSync(true);
                                }

                                if (bluePlayer.state == Player.State.SYNCED)
                                {

                                    bool repAdded = bluePlayer.Update(body.Joints);
                                    if (repAdded)
                                    {
                                        Console.WriteLine("Rep Added: " + bluePlayer.Reps);
                                        ROCKET_X[1] = (105 + (bluePlayer.Reps * 20));

                                        //moves bar based off index
                                        moveBar(1);
                                    }
                                }
                            }
                            #endregion
                            //If a body is bring tracked in the bodies[] add to the body counter.
                            bodyCounter++;

                            #region DrawJoints
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

                        }
                        #endregion
                    }
                    #endregion

                    // If player moved off screen, show idle for that side
                    if(!bluePlayerDetected && bluePlayer.state != Player.State.NOT_SYNCED)
                    {
                        showIdle(true);
                    }

                    if(!redPlayerDetected && redPlayer.state != Player.State.NOT_SYNCED)
                    {
                        showIdle(false);
                    }

                    String message = "red: " + redPlayer.Reps + ". Blue: " + bluePlayer.Reps;

                    
                    #region SetRocketPos 
                    Canvas.SetLeft(redRocket, ROCKET_X[0]);
                    //Change the 2nd X position for the trail and add it to the canvas
                    redRocketTrail.X2 = (ROCKET_X[0] + 10);
                    topBarCanvas.Children.Add(redRocketTrail);

                    Canvas.SetLeft(blueRocket, ROCKET_X[1]);
                    //Change the 2nd X position for the trail and add it to the canvas
                    blueRocketTrail.X2 = (ROCKET_X[1] + 10);
                    topBarCanvas.Children.Add(blueRocketTrail);

                    #endregion
                    this.StatusText = message;

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void showIdle(bool blue)
        {
            if (blue)
            {
                blueSyncVideo.Visibility = Visibility.Hidden;
                blueFuelTube.Visibility = Visibility.Hidden;
                blueFuelBottom.Visibility = Visibility.Hidden;
                // blue water gif and water
                rectangle_canvas_left.Visibility = Visibility.Hidden;
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                bluesideStandby.Visibility = Visibility.Visible;
                bluePlayer.state = Player.State.NOT_SYNCED;
            }
            else
            {
                redSyncVideo.Visibility = Visibility.Hidden;
                redFuelTube.Visibility = Visibility.Hidden;
                redFuelBottom.Visibility = Visibility.Hidden;
                // blue water gif and water
                rectangle_canvas_right.Visibility = Visibility.Hidden;
                rightSideBarCanvas.Visibility = Visibility.Hidden;
                redsideStandby.Visibility = Visibility.Visible;
                redPlayer.state = Player.State.NOT_SYNCED;
            }
        }

        private void showSync(bool blue)
        {
            if (blue)
            {
                blueSyncVideo.Visibility = Visibility.Visible;
                blueFuelTube.Visibility = Visibility.Hidden;
                blueFuelBottom.Visibility = Visibility.Hidden;
                // blue water gif and water
                rectangle_canvas_left.Visibility = Visibility.Hidden;
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                bluesideStandby.Visibility = Visibility.Hidden;
            }
            else
            {
                redSyncVideo.Visibility = Visibility.Visible;
                redFuelTube.Visibility = Visibility.Hidden;
                redFuelBottom.Visibility = Visibility.Hidden;
                // blue water gif and water
                rectangle_canvas_right.Visibility = Visibility.Hidden;
                rightSideBarCanvas.Visibility = Visibility.Hidden;
                redsideStandby.Visibility = Visibility.Hidden;
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

        void moveBar(int index)
        {
            SolidColorBrush fuelBrush = new SolidColorBrush(fuelOrange);
            System.Windows.Shapes.Rectangle fuelBarLeft;
            System.Windows.Shapes.Rectangle fuelBarRight;
            fuelBarLeft = new System.Windows.Shapes.Rectangle();
            fuelBarRight = new System.Windows.Shapes.Rectangle();

            //will raise the bar based off the index (left is 0 right is 1)
            //will not raise any more if the respective numRaise value is over a certain amount
            if (index == 1 && numRaiseRight < 42)
            {
                numJacksRight = numJacksRight + 13;
                // Add a rectangle Element
                fuelBarRight.Stroke = fuelBrush;
                fuelBarRight.Fill = fuelBrush;
                fuelBarRight.Width = 80;
                fuelBarRight.Height = numJacksRight;
                Canvas.SetLeft(fuelBarRight, -40);
                Canvas.SetBottom(fuelBarRight, 65);
                Canvas.SetBottom(testwater_right, (65 + (13 * numRaiseRight)));
                rightSideBarCanvas.Children.Add(fuelBarRight);
                numRaiseRight++;
            }

            if (index == 0 && numRaiseLeft < 42)
            {
                numJacksLeft = numJacksLeft + 13;
                // Add a rectangle Element
                fuelBarLeft.Stroke = fuelBrush;
                fuelBarLeft.Fill = fuelBrush;
                fuelBarLeft.Width = 80;
                fuelBarLeft.Height = numJacksLeft;
                Canvas.SetLeft(fuelBarLeft, -40);
                Canvas.SetBottom(fuelBarLeft, 65);
                Canvas.SetBottom(testwater_left, (65 + (13 * numRaiseLeft)));
                leftSideBarCanvas.Children.Add(fuelBarLeft);
                numRaiseLeft++;
            }
            if (numRaiseLeft >= 42)
            {
                //hides the rectangle and water gif for the left bar
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                bluesideStandby.Visibility = Visibility.Hidden;
                BlastOffLeft.Visibility = Visibility.Visible;
                BlastOffLeft.Play();
            }
            if (numRaiseRight >= 42)
            {
                //hides the rectangle and water gif for the right bar
                rightSideBarCanvas.Visibility = Visibility.Hidden;
                redsideStandby.Visibility = Visibility.Hidden;
                BlastOffRight.Visibility = Visibility.Visible;
                BlastOffRight.Play();
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

        private void blueSyncVideo_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine("Blue Sync Vid Visible?:" + blueSyncVideo.IsVisible);
            if (blueSyncVideo.IsVisible == true) 
            {
                Console.WriteLine("Now Syncing");
                blueSyncVideo.Position = new TimeSpan(0);
                bluePlayer.state = Player.State.SYNCING;
                blueSyncVideo.Play();
            }
        }

        private void blueSyncVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueSyncVideo.Visibility = Visibility.Hidden;
            bluePlayer.state = Player.State.SYNCED;
            showActiveBlue();
        }

        private void showActiveBlue()
        {
            Console.WriteLine("Show Active Blue");
            //Show Left Sidebar
            leftSideBarCanvas.Visibility = Visibility.Visible;
        }

        private void redSyncVideo_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (redSyncVideo.IsVisible == true)
            {
                redSyncVideo.Position = new TimeSpan(0);
                redSyncVideo.Play();
            }
        }

        private void redSyncVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            redSyncVideo.Visibility = Visibility.Hidden;
            redPlayer.state = Player.State.SYNCED;
            showActiveRed();
        }

        private void showActiveRed()
        {
            //Show Right Sidebar
            rightSideBarCanvas.Visibility = Visibility.Visible;
        }

        //gets rid of blastoff videos on end
        private void BlastOffLeft_MediaEnded(object sender, RoutedEventArgs e)
        {
            BlastOffLeft.Visibility = Visibility.Hidden;
            bluesideStandby.Visibility = Visibility.Visible;
        }

        private void BlastOffRight_MediaEnded(object sender, RoutedEventArgs e)
        {
            BlastOffRight.Visibility = Visibility.Hidden;
            redsideStandby.Visibility = Visibility.Visible;
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

    }
}
