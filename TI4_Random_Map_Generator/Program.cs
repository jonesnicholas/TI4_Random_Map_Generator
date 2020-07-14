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
            Application.Run(window);
            Galaxy bestGal = new Galaxy(GalaxyShape.Standard, new Shuffle(), 3, 6);
            RenderEngine renderer = new RenderEngine();
            renderer.renderGalaxy(window, bestGal);
        }
    }
}
