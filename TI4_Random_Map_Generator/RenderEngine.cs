using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TI4_Random_Map_Generator
{
    class RenderEngine
    {
        public Galaxy galaxy;

        public void render(Form form, PaintEventArgs e)
        {
            //renderGalaxy(form, e, galaxy);
            RenderTile(form, e, TilesetGenerator.GetSystemTile(50), 20, 10, small: false);
        }

        public void renderGalaxy(Form form, PaintEventArgs e, Galaxy galaxy)
        {
            if (e==null || galaxy==null)
            {
                return;
            }
            Debug.WriteLine(Environment.CurrentDirectory);
            Bitmap bitmap = new Bitmap("..\\..\\Resources\\full\\tile01.png");
            Graphics dc = e.Graphics;
            dc.DrawImage(bitmap, 60, 10);
            Pen RedPen = new Pen(Color.Red, 1);
            dc.DrawRectangle(RedPen, 10, 10, form.ClientRectangle.Width - 20, form.ClientRectangle.Size.Height - 20);
        }

        public void RenderTile(Form form, PaintEventArgs e, SystemTile tile, int x, int y, bool small = false, double scale = 1.0)
        {
            if (e== null || form == null)
            {
                Debug.WriteLine("Render Tile missing needed field");
                return;
            }
            string pathString = $"..\\..\\Resources\\" +
                $"{(small ? "small\\small-" : "full\\")}" +
                $"tile{(tile.sysNum < 10 ? "0" : "")}{tile.sysNum}.png";
            Bitmap tileImage;
            try
            {
                tileImage = new Bitmap(pathString);
            }
            catch(Exception ex)
            {
                pathString = $"..\\..\\Resources\\" +
                $"{(small ? "small\\small-" : "full\\")}" +
                $"tilebw.png";
                tileImage = new Bitmap(pathString);
            }
            double w = (small ? 200 : 900) * scale;
            double h = (small ? 172 : 774) * scale;
            e.Graphics.DrawImage(tileImage, x, y, (int)w, (int)h);
        }
    }
}
