using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TI4_Random_Map_Generator
{
    public partial class GraphicsWindow : Form
    {
        internal RenderEngine renderer;

        public GraphicsWindow()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (renderer != null)
            {
                renderer.render(this, e);
            }
        }
    }
}
