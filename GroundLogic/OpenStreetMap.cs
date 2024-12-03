using System.Drawing;

namespace SkyCombGroundLibrary.GroundLogic
{
    public class OpenStreetMap
    {
        public Bitmap? Background { get; set; }

        public async Task Main(double centreLat, double centreLon, int zoom = 13, int tileWidth = 2, int tileHeight = 2, bool drawCenterCross = true)
        {
            Background = await GetMap(centreLat, centreLon, zoom, tileWidth, tileHeight, drawCenterCross).ConfigureAwait(false);
        }

        private async Task<Bitmap> GetMap(double centerLat, double centerLon, int zoom, int tileWidth, int tileHeight, bool drawCenterCross)
        {
            int tileSize = 256;

            // Get global pixel coordinates
            double globalPixelX = (centerLon + 180.0) / 360.0 * (tileSize << zoom);
            double globalPixelY = (1.0 - Math.Log(Math.Tan(centerLat * Math.PI / 180.0) +
                                 1.0 / Math.Cos(centerLat * Math.PI / 180.0)) / Math.PI) / 2.0 * (tileSize << zoom);

            // Calculate tile coordinates
            int centerTileX = (int)(globalPixelX / tileSize);
            int centerTileY = (int)(globalPixelY / tileSize);

            // Calculate pixel offset within the full map
            double relativeX = globalPixelX - (centerTileX * tileSize);
            double relativeY = globalPixelY - (centerTileY * tileSize);

            Bitmap map = new Bitmap(tileWidth * tileSize, tileHeight * tileSize);

            using (Graphics g = Graphics.FromImage(map))
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MapViewer/1.0)");

                // Draw map tiles
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
                    }
                }

                if (drawCenterCross)
                {
                    // Calculate the exact position in the final image
                    int exactX = (int)(tileSize * (tileWidth / 2.0) + relativeX);
                    int exactY = (int)(tileSize * (tileHeight / 2.0) + relativeY);
                    int crossSize = 20;

                    // Draw debug information
                    // using (Font debugFont = new Font("Arial", 8))
                    //using (Brush debugBrush = new SolidBrush(Color.Black))
                    //{
                    //    g.DrawString($"Zoom: {zoom}", debugFont, debugBrush, 10, 10);
                    //    g.DrawString($"Lat: {centerLat:F6}", debugFont, debugBrush, 10, 25);
                    //    g.DrawString($"Lon: {centerLon:F6}", debugFont, debugBrush, 10, 40);
                    //    g.DrawString($"Pixel: ({exactX}, {exactY})", debugFont, debugBrush, 10, 55);
                    //}

                    // Draw the cross
                    using (Pen redPen = new Pen(Color.Red, 2))
                    {
                        g.DrawLine(redPen, exactX - crossSize, exactY, exactX + crossSize, exactY);
                        g.DrawLine(redPen, exactX, exactY - crossSize, exactX, exactY + crossSize);
                    }
                }

                using (Pen borderPen = new Pen(Color.Black, 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, map.Width - 1, map.Height - 1);
                }
            }

            return map;
        }
    }
}