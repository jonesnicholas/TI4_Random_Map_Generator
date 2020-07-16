using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TI4_Random_Map_Generator
{
    public partial class GraphicsWindow : Form
    {
        internal RenderEngine renderer;
        internal GalaxyCrucible parallelizer;
        readonly Timer frameTimer = new Timer();

        public GraphicsWindow()
        {
            InitializeComponent();
            this.frameTimer.Tick += new EventHandler(this.Frame_Timer_Tick);
            double targetFPS = 120;
            this.frameTimer.Interval = (int)Math.Ceiling(1000.0 / targetFPS);
            this.frameTimer.Enabled = true;
            this.frameTimer.Start();
            DoubleBuffered = true;
            this.MouseWheel += formWindow_MouseWheel;
            Application.Idle += HandleApplicationIdle;
            this.Focus();
        }

        void Frame_Timer_Tick(object sender, EventArgs e)
        {
            //this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (renderer != null)
            {
                renderer.render(this, e);
            }
        }

        void HandleApplicationIdle(object sender, EventArgs e)
        {
            parallelizer.generateGalaxies();
            this.Invalidate();
        }

        bool IsApplicationIdle()
        {
            return PeekMessage(out NativeMessage result, IntPtr.Zero, (uint)0, (uint)0, (uint)0) == 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);


        private void formWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            renderer.scroll(e.Delta);
            this.Invalidate();
        }
    }
}
