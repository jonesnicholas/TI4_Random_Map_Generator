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
            window.renderer = renderer;
            Shuffle shuffle = new Shuffle();
            renderer.galaxy = new Galaxy(GalaxyShape.Standard, shuffle, 3, 6);
            Application.Run(window);
        }
    }
}
