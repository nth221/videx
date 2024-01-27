using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using videx.Model.YOLOv5;

namespace videx.Model
{
    internal class Crop
    {
        public static Image cropImage(Image img, Rectangle cropArea)
        {
            try
            {
                Bitmap bmpImage = new Bitmap(img);
                Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
                return bmpCrop;
            }
            catch (OutOfMemoryException e)
            {
                return null;
            }

        }
    }
}
