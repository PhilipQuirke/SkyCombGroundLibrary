using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net.Http;

namespace SkyCombGroundLibrary.GroundLogic
{
    public class OpenStreetMap
    {
        public Bitmap? Background { get; set; }
        public async Task Main(string[] args)
        {
            // Transform data
            double centreLat = Convert.ToDouble(args[0]);
            double centreLon = Convert.ToDouble(args[1]);

            // Map center and size
            int zoom = 13; // Zoom level
            int tileWidth = 2; // Number of tiles (width)
            int tileHeight = 2; // Number of tiles (height)

            // Download and stitch map
            Background = await GetMap(centreLat, centreLon, zoom, tileWidth, tileHeight).ConfigureAwait(false);
        }

        private async Task<Bitmap> GetMap(double centerLat, double centerLon, int zoom, int tileWidth, int tileHeight)
        {
            // Calculate center tile
            int centerTileX = LongToTileX(centerLon, zoom);
            int centerTileY = LatToTileY(centerLat, zoom);

            // Create bitmap for stitched map
            int tileSize = 256; // Tile size in pixels
            Bitmap map = new Bitmap(tileWidth * tileSize, tileHeight * tileSize);

            using (Graphics g = Graphics.FromImage(map))
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MapViewer/1.0)");
                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        int tileX = centerTileX - tileWidth / 2 + x;
                        int tileY = centerTileY - tileHeight / 2 + y;
                        string url = $"https://tile.openstreetmap.org/{zoom}/{tileX}/{tileY}.png";

                        HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            byte[] tileData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            using (MemoryStream ms = new MemoryStream(tileData))
                            {
                                Image tile = Image.FromStream(ms);
                                g.DrawImage(tile, x * tileSize, y * tileSize, tileSize, tileSize);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to fetch tile: {response.StatusCode} - {response.ReasonPhrase}");
                        }
                        /*
                        try
                        {
                            byte[] tileData = await client.GetByteArrayAsync(url);
                            using (MemoryStream ms = new MemoryStream(tileData))
                            {
                                Image tile = Image.FromStream(ms);
                                g.DrawImage(tile, x * tileSize, y * tileSize, tileSize, tileSize);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to download tile ({tileX}, {tileY}): {ex.Message}");
                        }
                        */
                    }
                }
            }

            return map;
        }

        private int LongToTileX(double lon, int zoom)
        {
            return (int)((lon + 180.0) / 360.0 * (1 << zoom));
        }

        private int LatToTileY(double lat, int zoom)
        {
            return (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
        }
    }
}

