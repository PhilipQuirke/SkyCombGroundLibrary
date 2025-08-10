using SkyCombGround.CommonSpace;
using System.Drawing;

namespace SkyCombGroundLibrary.GroundLogic
{
    // Class to create a bitmap of the country side using OpenStreetMap. Needs internet access.
    public class OpenStreetMap
    {
        public const int LargeAreaZoom = 14;
        public const int SmallAreaZoom = 10;


        public Bitmap? Background { get; set; }

        public async Task Main(GlobalLocation center, GlobalLocation? range, int zoom)
        {
            Background = await GetMap(center, range, zoom).ConfigureAwait(false);
        }

        private async Task<Bitmap> GetMap(GlobalLocation center, GlobalLocation? range, int zoom)
        {
            int tileSize = 256;
            int tileWidth = 2;
            int tileHeight = 2;

            // Get global pixel coordinates
            double globalPixelX = (center.Longitude + 180.0) / 360.0 * (tileSize << zoom);
            double globalPixelY = (1.0 - Math.Log(Math.Tan(center.Latitude * Math.PI / 180.0) +
                                 1.0 / Math.Cos(center.Latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (tileSize << zoom);

            // Calculate tile coordinates
            int centerTileX = (int)(globalPixelX / tileSize);
            int centerTileY = (int)(globalPixelY / tileSize);

            // Calculate pixel offset within the full map
            double relativeX = globalPixelX - (centerTileX * tileSize);
            double relativeY = globalPixelY - (centerTileY * tileSize);

            Bitmap map = new(tileWidth * tileSize, tileHeight * tileSize);

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

                using (Pen redPen = new Pen(Color.Red, 2))
                {
                    if (range != null)
                    {
                        // Calculate rectangle dimensions based on range
                        double rangeWidth = range.Longitude / 360.0 * (tileSize << zoom);
                        double rangeHeight = range.Latitude / 360.0 * (tileSize << zoom);

                        int exactX = (int)(tileSize * (tileWidth / 2.0) + relativeX - rangeWidth / 2);
                        int exactY = (int)(tileSize * (tileHeight / 2.0) + relativeY - rangeHeight / 2);

                        g.DrawRectangle(redPen, exactX, exactY, (int)rangeWidth, (int)rangeHeight);
                    }
                    else
                    {
                        // Calculate the dimensions of the smaller map in pixels on the larger map
                        double zoomFactor = Math.Pow(2, SmallAreaZoom - LargeAreaZoom);

                        int smallerMapWidth = (int)(tileWidth * tileSize * zoomFactor);
                        int smallerMapHeight = (int)(tileHeight * tileSize * zoomFactor);

                        int rectangleX = (int)(tileSize * (tileWidth / 2.0) + relativeX - smallerMapWidth / 2);
                        int rectangleY = (int)(tileSize * (tileHeight / 2.0) + relativeY - smallerMapHeight / 2);

                        g.DrawRectangle(redPen, rectangleX, rectangleY, smallerMapWidth, smallerMapHeight);
                    }
                }

                using (Pen borderPen = new Pen(Color.Black, 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, map.Width - 1, map.Height - 1);
                }
            }

            return map;
        }


        // Get a larger and smaller view of the area around globalLocation
        public static (Bitmap?, Bitmap?) GetTwoMaps(
            GlobalLocation globalLocation, // centre of flight area
            GlobalLocation globalRange) // rectangle around the centre
        {
            OpenStreetMap map = new();

            map.Main(globalLocation, null, SmallAreaZoom).Wait();
            Bitmap? bitmap1 = new Bitmap(map.Background);

            map.Main(globalLocation, globalRange, LargeAreaZoom).Wait();
            Bitmap? bitmap2 = new Bitmap(map.Background);

            return (bitmap1, bitmap2);
        }
    }
}