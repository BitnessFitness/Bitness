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
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace Bitness
{
    /// <summary>
    /// Interaction logic for FloorWindow.xaml
    /// </summary>
    public partial class FloorWindow : Window
    {
        public static const double OFFSET = 1f;
        public static const double WIDTH_IN = 2f;
        public static const double HEIGHT_IN = 2f;

        public FloorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// given a camera space point it maps it to a pixel position on the floor
        /// </summary>
        /// <param name="p">CameraSpacePoint in distance of meters</param>
        public void MapDepthPointToFloor(CameraSpacePoint p)
        {
            if (p.Z > OFFSET &&
                p.Z < (OFFSET + HEIGHT_IN) &&
                p.X > -(WIDTH_IN / 2) &&
                p.X < (WIDTH_IN / 2))
            {
                double fx = p.Z - OFFSET;
                double fy = p.X + (WIDTH_IN);

                // convert into pixels
                double px = (this.Width / WIDTH_IN) * fx;
                double py = (this.Height / HEIGHT_IN) * fy;

                // this probably works.
            }
        }
    }
}
