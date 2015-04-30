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
    using System.Threading;
    using WpfAnimatedGif;

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

        private double[] ROCKET_X = new double[2] { 105, 105 };

        /// <summary>
        /// Refrence to left and right players 
        /// </summary>
        public Player redPlayer;
        public Player bluePlayer;

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

        private readonly int GOAL_NUM = 15;

        //Custom Colors For Bitness
        public Color red = Color.FromRgb(241, 128, 33);
        public Color blue = Color.FromRgb(16, 177, 232);
        public Color fuelOrange = Color.FromRgb(219, 131, 35);
        public Color fuelBlue = Color.FromRgb(35, 153, 219);

        //Integers used to calculate individual distances
        public double totalRedPlayerDistance;
        public double totalBluePlayerDistance;

        //Integers used to calculate team distances
        public double totalRedTeamDistance;
        public double totalBlueTeamDistance;
        public double maxTotalTeamJacks = 1800;
        public long maxTeamTotalDistance = 4670000000;

        //Bool for tutorial
        public bool tutorialPlaying = false;

        //At planet Bool for red
        public bool redAtMars = false;
        public bool redAtJupiter = false;
        public bool redAtSaturn = false;
        public bool redAtNeptune = false;
        public bool redAtUranus = false;
        public bool redAtPluto = false;

        //At planet Bool for blue
        public bool blueAtMars = false;
        public bool blueAtJupiter = false;
        public bool blueAtSaturn = false;
        public bool blueAtNeptune = false;
        public bool blueAtUranus = false;
        public bool blueAtPluto = false;

        private MediaElement[] redPlanetMovieArray;
        private MediaElement[] bluePlanetMovieArray;

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

            bluePlanetMovieArray = new MediaElement[] { blueMars, blueJupiter, blueSaturn, blueUranus, blueNeptune, bluePluto };
            redPlanetMovieArray = new MediaElement[] { redMars, redJupiter, redSaturn, redUranus, redNeptune, redPluto };

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
                                        //(Total # of Reps / Max Total Jacks needed To reach last planet) * (Exact Canvas Distance to last planet --> 1010px))
                                        double redDistanceToTravel = (redPlayer.Reps / (double)maxTotalTeamJacks) * 1010;
                                        double redStartingPoint = 105;
                                        ROCKET_X[0] = (redStartingPoint + redDistanceToTravel);
                                        //If the rocket reaches a certain planet fire event to show win condition
                                        if (ROCKET_X[0] > 250 && ROCKET_X[0] < 251)
                                        {
                                            //Reached Mars
                                            if (redAtMars == false)
                                            {
                                                redAtPlanet(0);
                                            }
                                        }

                                        if (ROCKET_X[0] > 395 && ROCKET_X[0] < 396)
                                        {
                                            //Reached Jupiter
                                            if (redAtJupiter == false)
                                            {
                                                redAtPlanet(1);
                                            }
                                        }

                                        if (ROCKET_X[0] > 590 && ROCKET_X[0] < 591)
                                        {
                                            //Reached Saturn
                                            if (redAtSaturn == false)
                                            {
                                                redAtPlanet(2);
                                            }
                                        }

                                        if (ROCKET_X[0] > 770 && ROCKET_X[0] < 771)
                                        {
                                            //Reached Neptune
                                            if (redAtNeptune == false)
                                            {
                                                redAtPlanet(3);
                                            }
                                        }

                                        if (ROCKET_X[0] > 935 && ROCKET_X[0] < 936)
                                        {
                                            //Reached Uranus
                                            if (redAtUranus == false)
                                            {
                                                redAtPlanet(4);
                                            }
                                        }

                                        if (ROCKET_X[0] > 1010 && ROCKET_X[0] < 1011)
                                        {
                                            //Reached Pluto
                                            if (redAtPluto == false)
                                            {
                                                redAtPlanet(5);
                                            }
                                        }

                                        //moves bar based off index

                                        if (redPlayer.Reps <= GOAL_NUM)
                                        {
                                            //Calculate Real Total Team Distance
                                            totalRedPlayerDistance = ((redPlayer.Reps / (double)maxTotalTeamJacks) * maxTeamTotalDistance) / 1000000000;
                                            totalRedTeamDistance += totalRedPlayerDistance;
                                            RedTeamDistanceTraveled.Content = Math.Round(totalRedTeamDistance, 2);
                                            RedDistanceTraveled.Content = Math.Round(totalRedPlayerDistance, 2);
                                            moveBar(1);
                                        }
                                        else
                                        {
                                            showLaunch(false);
                                            redPlayer.state = Player.State.COMPLETED;
                                        }
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
                                        //(Total # of Reps / Max Total Jacks needed To reach last planet) * (Exact Canvas Distance to last planet --> 1010px))
                                        double blueDistanceToTravel = (bluePlayer.Reps / (double)maxTotalTeamJacks) * 1010;
                                        double blueStartingPoint = 105;
                                        ROCKET_X[1] = (blueStartingPoint + blueDistanceToTravel);
                                        //If the rocket reaches a certain planet fire event to show win condition
                                        if (ROCKET_X[1] > 250 && ROCKET_X[1] < 251)
                                        {
                                            //Reached Mars
                                            if (blueAtMars == false)
                                            {
                                                blueAtPlanet(0);
                                            }
                                        }

                                        if (ROCKET_X[1] > 395 && ROCKET_X[1] < 396)
                                        {
                                            //Reached Jupiter
                                            if (blueAtJupiter == false)
                                            {
                                                blueAtPlanet(1);
                                            }
                                        }

                                        if (ROCKET_X[1] > 590 && ROCKET_X[1] < 591)
                                        {
                                            //Reached Saturn
                                            if (blueAtSaturn == false)
                                            {
                                                blueAtPlanet(2);
                                            }
                                        }

                                        if (ROCKET_X[1] > 770 && ROCKET_X[1] < 771)
                                        {
                                            //Reached Neptune
                                            if (blueAtNeptune == false)
                                            {
                                                blueAtPlanet(3);
                                            }
                                        }

                                        if (ROCKET_X[1] > 935 && ROCKET_X[1] < 936)
                                        {
                                            //Reached Uranus
                                            if (blueAtUranus == false)
                                            {
                                                blueAtPlanet(4);
                                            }
                                        }

                                        if (ROCKET_X[1] > 1010 && ROCKET_X[1] < 1011)
                                        {
                                            //Reached Pluto
                                            if (blueAtPluto == false)
                                            {
                                                blueAtPlanet(5);
                                            }
                                        }
                                        //moves bar based off index

                                        if (bluePlayer.Reps <= GOAL_NUM)
                                        {
                                            //Calculate Real Total Team Distance
                                            totalBluePlayerDistance = ((bluePlayer.Reps / (double)maxTotalTeamJacks) * maxTeamTotalDistance) / 1000000000;
                                            totalBlueTeamDistance += totalBluePlayerDistance;
                                            BlueTeamDistanceTraveled.Content = Math.Round(totalBlueTeamDistance, 2);
                                            BlueDistanceTraveled.Content = Math.Round(totalBluePlayerDistance, 2);
                                            moveBar(0);
                                        }
                                        else
                                        {
                                            showLaunch(true);
                                            bluePlayer.state = Player.State.COMPLETED;
                                        }
                                    }
                                }
                            }
                            #endregion
                            //If a body is bring tracked in the bodies[] add to the body counter.
                            bodyCounter++;

                            this.floor.DrawTopDownView(redPlayer, bluePlayer);

                            if (bluePlayer.state == Player.State.SYNCED && redPlayer.state == Player.State.SYNCED)
                            {
                                if (tutorialPlaying == false)
                                {
                                    playTutorial();
                                }
                            }

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
                    if (!bluePlayerDetected && bluePlayer.state != Player.State.NOT_SYNCED)
                    {
                        showIdle(true);
                        tutorialPlaying = false;
                        //resets bar for player blue
                        resetBars(0);
                    }

                    if (!redPlayerDetected && redPlayer.state != Player.State.NOT_SYNCED)
                    {
                        showIdle(false);
                        tutorialPlaying = false;
                        //resets bar for player red
                        resetBars(1);
                    }

                    String message = "Red: " + redPlayer.Reps + ". Blue: " + bluePlayer.Reps;

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
        private void resetBars(int index)
        {
            switch (index)
            {
                //resets the values for the player and resets the location of the rectangle
                case 0:
                    bluePlayer.Reps = 0;
                    bluePlayer.exercise.Reps = 0;
                    blueFuelBlock.Height = 0;
                    Canvas.SetTop(blueFuelTop, -103);
                    break;
                case 1:
                    redPlayer.Reps = 0;
                    redPlayer.exercise.Reps = 0;
                    redFuelBlock.Height = 0;
                    Canvas.SetTop(redFuelTop, -103);
                    break;
            }
        }
        private void showLaunch(bool blue)
        {
            Console.WriteLine("Showing launch");
            if (blue && bluePlayer.state != Player.State.COMPLETED)
            {
                blueSyncVideo.Visibility = Visibility.Hidden;
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                //Show standby screen, set Blue to not synced
                bluesideStandby.Visibility = Visibility.Hidden;

                BlastOffLeft.Visibility = Visibility.Visible;
                BlastOffLeft.Position = new TimeSpan(0);
                BlastOffLeft.Play();
            }
            else if (!blue && redPlayer.state != Player.State.COMPLETED)
            {
                redSyncVideo.Visibility = Visibility.Hidden;
                rightSideBarCanvas.Visibility = Visibility.Hidden;
                //Show standby screen, set Blue to not synced
                redsideStandby.Visibility = Visibility.Hidden;

                BlastOffRight.Visibility = Visibility.Visible;
                BlastOffRight.Position = new TimeSpan(0);
                BlastOffRight.Play();
            }

            //reset tutorial bool to false so it can play for next players
            tutorialPlaying = false;
        }

        private void showIdle(bool blue)
        {
            if (blue)
            {
                //Hide sidebar and sync video
                blueSyncVideo.Visibility = Visibility.Hidden;
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                //Show standby screen, set Blue to not synced
                bluesideStandby.Visibility = Visibility.Visible;
                bluePlayer.state = Player.State.NOT_SYNCED;
            }
            else
            {
                //Hide sidebar and sync video
                redSyncVideo.Visibility = Visibility.Hidden;
                rightSideBarCanvas.Visibility = Visibility.Hidden;
                //Show standby screen, set Blue to not synced
                redsideStandby.Visibility = Visibility.Visible;
                redPlayer.state = Player.State.NOT_SYNCED;
            }
        }

        private void showSync(bool blue)
        {
            if (blue)
            {
                //Show Sync Video
                blueSyncVideo.Visibility = Visibility.Visible;
                //Hide Sidebar and Standby
                leftSideBarCanvas.Visibility = Visibility.Hidden;
                bluesideStandby.Visibility = Visibility.Hidden;
            }
            else
            {
                //Show Sync Video
                redSyncVideo.Visibility = Visibility.Visible;
                //Hide Sidebar and Visibility
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
            int JUMPING_JACKS_REQUIRED = 15;
            int FUEL_INCREASE_AMOUNT = 22;
            //will raise the bar based off the index (Blue is 0 | Red is 1)
            //will not raise any more if the respective numRaise value is over a certain amount           			
            if (index == 0 && numRaiseLeft < JUMPING_JACKS_REQUIRED)
            {
                blueFuelBlock.Height = FUEL_INCREASE_AMOUNT * bluePlayer.Reps;
                Canvas.SetTop(blueFuelTop, (Canvas.GetTop(blueFuelTop) - FUEL_INCREASE_AMOUNT));
            }

            if (index == 1 && numRaiseRight < JUMPING_JACKS_REQUIRED)
            {
                redFuelBlock.Height = FUEL_INCREASE_AMOUNT * redPlayer.Reps;
                Canvas.SetTop(redFuelTop, (Canvas.GetTop(redFuelTop) - FUEL_INCREASE_AMOUNT));
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
            if (blueSyncVideo.IsVisible == true)
            {
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
            //Show Left Sidebar
            leftSideBarCanvas.Visibility = Visibility.Visible;
        }

        private void hidePlayerStats(bool blue)
        {
            if (blue)
            {
                BlueTeamInfo.Visibility = Visibility.Hidden;
                bluesideStandby.Visibility = Visibility.Visible;
            }
            else
            {
                RedTeamInfo.Visibility = Visibility.Hidden;
                redsideStandby.Visibility = Visibility.Visible;
            }
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
        private async void BlastOffLeft_MediaEnded(object sender, RoutedEventArgs e)
        {
            BlastOffLeft.Visibility = Visibility.Hidden;
            BlueTeamInfo.Visibility = Visibility.Visible;
            await Task.Delay(20000);
            hidePlayerStats(true);
        }

        private async void BlastOffRight_MediaEnded(object sender, RoutedEventArgs e)
        {
            BlastOffRight.Visibility = Visibility.Hidden;
            RedTeamInfo.Visibility = Visibility.Visible;
            var controller = ImageBehavior.GetAnimationController(RedFinished);
            controller.GotoFrame(0);
            controller.Play();
            await Task.Delay(20000);
            hidePlayerStats(false);
        }

        private void blueAtPlanet(int which)
        {
            bluePlanetMovieArray[which].Visibility = Visibility.Visible;
            bluePlanetMovieArray[which].Play();
        }

        private void redAtPlanet(int which)
        {
            redPlanetMovieArray[which].Visibility = Visibility.Visible;
            redPlanetMovieArray[which].Play();
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

        #region Red Reached Planet Events
        private void redMars_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtMars = true;
            mainGrid.Children.Remove(redMars);
        }

        private void redJupiter_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtJupiter = true;
            mainGrid.Children.Remove(redJupiter);
        }

        private void redSaturn_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtSaturn = true;
            mainGrid.Children.Remove(redSaturn);
        }

        private void redNeptune_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtNeptune = false;
            mainGrid.Children.Remove(redNeptune);
        }

        private void redUranus_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtUranus = false;
            mainGrid.Children.Remove(redUranus);
        }

        private void redPluto_MediaEnded(object sender, RoutedEventArgs e)
        {
            redAtPluto = false;
            mainGrid.Children.Remove(redPluto);
        }
        #endregion

        #region Blue Reached Planet Events
        private void blueMars_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtMars = true;
            mainGrid.Children.Remove(blueMars);
        }

        private void blueJupiter_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtJupiter = true;
            mainGrid.Children.Remove(blueJupiter);
        }

        private void blueSaturn_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtSaturn = true;
            mainGrid.Children.Remove(blueSaturn);
        }

        private void blueNeptune_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtNeptune = false;
            mainGrid.Children.Remove(blueNeptune);
        }

        private void blueUranus_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtUranus = false;
            mainGrid.Children.Remove(blueUranus);
        }

        private void bluePluto_MediaEnded(object sender, RoutedEventArgs e)
        {
            blueAtPluto = false;
            mainGrid.Children.Remove(bluePluto);
        }
        #endregion

        private void playTutorial()
        {
            Console.WriteLine("Showing Tutorial");
            tutorialPlaying = true;
            tutorialVideo.Visibility = Visibility.Visible;
            tutorialVideo.Position = new TimeSpan(0);
            tutorialVideo.Play();
        }

        private void tutorialVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            tutorialVideo.Visibility = Visibility.Hidden;
        }
    }
}
