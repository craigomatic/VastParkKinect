using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.PluginFramework.Controllers;
using OpenNI;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Interop;

namespace Plugin.OpenNI.Controllers
{
    public class VideoController : IController
    {
        public bool Enabled { get; set; }

        public Context Context { get; private set; }
        public ITextureController TextureController { get; private set; }

        private ImageGenerator _ImageGenerator;
        private Bitmap _Bitmap;

        public VideoController(Context kinectContext, ITextureController textureController)
        {
            this.Context = kinectContext;
            this.TextureController = textureController;

            _ImageGenerator = Context.FindExistingNode(NodeType.Image) as ImageGenerator;

            if (_ImageGenerator == null)
            {
                throw new Exception("Viewer must have an image node!");
            }

            _Bitmap = new Bitmap((int)_ImageGenerator.MapOutputMode.XRes, (int)_ImageGenerator.MapOutputMode.YRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }

        public unsafe void Update()
        {
            var imageMetadata = _ImageGenerator.GetMetaData();

            if (imageMetadata.IsDataNew)
            {
                // copy bits.
                Rectangle rect = new Rectangle(0, 0, _Bitmap.Width, _Bitmap.Height);

                var bitmapData = _Bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                try
                {
                    byte* pSrc = (byte*)_ImageGenerator.ImageMapPtr.ToPointer();
                    for (int y = 0; y < imageMetadata.YRes; ++y)
                    {
                        byte* pDest = (byte*)bitmapData.Scan0.ToPointer() + y * bitmapData.Stride;
                        for (int x = 0; x < imageMetadata.XRes; ++x, pSrc += 3, pDest += 3)
                        {
                            pDest[0] = pSrc[2];
                            pDest[1] = pSrc[1];
                            pDest[2] = pSrc[0];
                        }
                    }

                    var bmpSrc = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);


                    _Bitmap.UnlockBits(bitmapData);

                    var frame = BitmapFrame.Create(bmpSrc);

                    if (frame.Format != PixelFormats.Bgra32)
                    {
                        var formatConverter = new FormatConvertedBitmap();
                        formatConverter.BeginInit();
                        formatConverter.DestinationFormat = PixelFormats.Bgra32;
                        formatConverter.Source = frame;
                        formatConverter.EndInit();

                        frame = BitmapFrame.Create(formatConverter);
                    }

                    var stride = frame.PixelWidth * ((frame.Format.BitsPerPixel + 7) / 8);
                    var pixelBuffer = new byte[frame.PixelHeight * stride];

                    frame.CopyPixels(pixelBuffer, stride, 0);

                    this.TextureController.Write(pixelBuffer, new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                }
                catch { }
            }
        }

        public void Dispose()
        {
            this.TextureController.Dispose();
            _ImageGenerator.Dispose();
            _Bitmap.Dispose();
        }
    }
}
