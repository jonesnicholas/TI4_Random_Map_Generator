using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TI4_Random_Map_Generator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            GraphicsWindow window = new GraphicsWindow();
            RenderEngine renderer = new RenderEngine();
            GalaxyCrucible parallelizer = new GalaxyCrucible();
            window.renderer = renderer;
            window.parallelizer = parallelizer;
            renderer.parallelizer = parallelizer;
            Application.Run(window);
        }
    }
}
