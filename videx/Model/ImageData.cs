using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace videx.Model
{
    public class ImageData
    {

        public int Id { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string? FilePath { get; set; }
        public BitmapImage? Bitmap { get; set; }

        public string? Class { get; set; }

        public int Frame { get; set; }

        public static List<string>? ObjClass = new List<string>();

        public static List<int>? objFrame = new List<int>();
    }
}
