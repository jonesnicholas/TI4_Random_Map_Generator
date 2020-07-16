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
        internal GalaxyCrucible parallelizer;
        public Galaxy galaxy;
        Dictionary<string, Bitmap> images;
        double scale = 0.15;

        public RenderEngine()
        {
            images = new Dictionary<string, Bitmap>();
        }

        public void render(Form form, PaintEventArgs e)
        {
            galaxy = parallelizer?.bestGal;
            renderGalaxy(form, e, galaxy);
            //RenderTile(form, e, TilesetGenerator.GetSystemTile(50), 20, 10, small: false);
        }

        public void renderGalaxy(Form form, PaintEventArgs e, Galaxy galaxy)
        {
            if (galaxy == null)
            {
                return;
            }
            bool small = false;
            Point tileSize = new Point((int)((small ? 200 : 900) * scale), (int)((small ? 172 : 774) * scale));
            for (int y = 0; y < galaxy.tiles.Length; y++)
            {
                for (int x = 0; x < galaxy.tiles[y].Length; x++)
                {
                    SystemTile tile = galaxy.tiles[y][x];
                    bool isHS = galaxy.HSLocations.Contains(new Tuple<int, int>(x, y));
                    if (tile.sysNum == 0 && !isHS)
                    {
                        continue;
                    }
                    Point tilePos = new Point((int)(0.75*tileSize.X)*x, (int)(tileSize.Y*(y + 0.5*(x-3))));
                    RenderSystemTile(form, e, tile, tilePos.X, tilePos.Y, tileSize.X, tileSize.Y, small);
                }
            }
        }

        public void RenderSystemTile(Form form, PaintEventArgs e, SystemTile tile, int x, int y, int w, int h, bool small = false)
        {
            if (e== null || form == null)
            {
                Debug.WriteLine("Render Tile missing needed field");
                return;
            }
            string pathString = $"..\\..\\Resources\\" +
                $"{(small ? "small\\small-" : "full\\")}";

            if (tile.sysNum == 0 && tile.playerNum != -1)
            {
                pathString += "tilehome.png";
            }
            else
            {
                pathString += $"tile{(tile.sysNum < 10 ? "0" : "")}{tile.sysNum}.png";
            }
            Bitmap tileImage;
            if (images.ContainsKey(pathString))
            {
                tileImage = images[pathString];
            }
            else
            {
                try
                {
                    tileImage = new Bitmap(pathString);
                }
                catch (Exception ex)
                {
                    pathString = $"..\\..\\Resources\\" +
                    $"{(small ? "small\\small-" : "full\\")}" +
                    $"tilebw.png";
                    tileImage = new Bitmap(pathString);
                }
                images.Add(pathString, tileImage);
            }
            e.Graphics.DrawImage(tileImage, x, y, w, h);
        }

        public void scroll(double delta)
        {
            scale *= Math.Pow(2, delta / 120 / 4);
            Debug.WriteLine(scale);
        }
    }
}
