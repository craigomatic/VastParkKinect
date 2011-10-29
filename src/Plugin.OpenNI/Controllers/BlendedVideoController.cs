﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using VastPark.PluginFramework.Controllers;
//using OpenNI;
//using System.Windows.Media.Imaging;
//using System.Windows.Media;
//using System.Drawing.Imaging;
//using System.Drawing;

//namespace Plugin.OpenNI.Controllers
//{
//    /// <summary>
//    /// Blends the depth video with the standard video to create a 3D video effect
//    /// </summary>
//    public class BlendedVideoController : IController
//    {
//        public Context Context { get; private set; }

//        public ITextureController TextureController { get; private set; }

//        private ImageGenerator _ImageGenerator;
//        private DepthGenerator _DepthGenerator;
//        private int[] _Histogram;
//        private Bitmap _DepthBitmap;
//        private Bitmap _ImageBitmap;

//        public BlendedVideoController(Context kinectContext, ITextureController textureController)
//        {
//            this.Context = kinectContext;
//            this.TextureController = textureController;

//            _DepthGenerator = Context.FindExistingNode(NodeType.Depth) as DepthGenerator;

//            if (_DepthGenerator == null)
//            {
//                throw new Exception("Viewer must have a depth node!");
//            }

//            _ImageGenerator = Context.FindExistingNode(NodeType.Image) as ImageGenerator;

//            if (_ImageGenerator == null)
//            {
//                throw new Exception("Viewer must have an image node!");
//            }

//            var imageMapMode = _ImageGenerator.GetMapOutputMode();
//            var depthMapMode = _DepthGenerator.GetMapOutputMode(); 
            
//            _ImageBitmap = new Bitmap((int)imageMapMode.nXRes, (int)imageMapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);           
//            _DepthBitmap = new Bitmap((int)depthMapMode.nXRes, (int)depthMapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
//            _Histogram = new int[_DepthGenerator.GetDeviceMaxDepth()];
//        }
        
//        public bool Enabled { get; set; }

//        public unsafe void Update()
//        {
//            var depthMetadata = _DepthGenerator.GetMetaData();

//            if (depthMetadata.IsDataNew && depthMetadata.DataSize != 0)
//            {
//                _CalculateHistogram(depthMetadata);

//                // copy bits.
//                Rectangle rect = new Rectangle(0, 0, _DepthBitmap.Width, _DepthBitmap.Height);

//                var bitmapData = _DepthBitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
//                var locked = true;

//                try
//                {
//                    ushort* pDepth = (ushort*)_DepthGenerator.GetDepthMapPtr().ToPointer();

//                    // set pixels
//                    for (int y = 0; y < depthMetadata.YRes; ++y)
//                    {
//                        byte* pDest = (byte*)bitmapData.Scan0.ToPointer() + y * bitmapData.Stride;
//                        for (int x = 0; x < depthMetadata.XRes; ++x, ++pDepth, pDest += 3)
//                        {
//                            byte pixel = (byte)_Histogram[*pDepth];
//                            pDest[0] = 0;
//                            pDest[1] = pixel;
//                            pDest[2] = pixel;
//                        }
//                    }

//                    var bmpSrc = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

//                    _DepthBitmap.UnlockBits(bitmapData);

//                    locked = false;

//                    var frame = BitmapFrame.Create(bmpSrc);

//                    if (frame.Format != PixelFormats.Bgra32)
//                    {
//                        var formatConverter = new FormatConvertedBitmap();
//                        formatConverter.BeginInit();
//                        formatConverter.DestinationFormat = PixelFormats.Bgra32;
//                        formatConverter.Source = frame;
//                        formatConverter.EndInit();

//                        frame = BitmapFrame.Create(formatConverter);
//                    }

//                    var stride = frame.PixelWidth * ((frame.Format.BitsPerPixel + 7) / 8);
//                    var pixelBuffer = new byte[frame.PixelHeight * stride];

//                    frame.CopyPixels(pixelBuffer, stride, 0);

//                    this.TextureController.Write(pixelBuffer, new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height));
//                }
//                catch
//                {
//                    if (locked)
//                    {
//                        _DepthBitmap.UnlockBits(bitmapData);
//                    }
//                }
//            }
//        }

//        public void Dispose()
//        {
//            this.TextureController.Dispose();
//            _DepthGenerator.Dispose();
//            _DepthBitmap.Dispose();
//        }

//        private unsafe void _CalculateHistogram(DepthMetaData depthMetadata)
//        {
//            // reset
//            for (int i = 0; i < _Histogram.Length; ++i)
//            {
//                _Histogram[i] = 0;
//            }

//            ushort* pDepth = (ushort*)depthMetadata.DepthMapPtr.ToPointer();

//            int points = 0;
//            for (int y = 0; y < depthMetadata.YRes; ++y)
//            {
//                for (int x = 0; x < depthMetadata.XRes; ++x, ++pDepth)
//                {
//                    ushort depthVal = *pDepth;
//                    if (depthVal != 0)
//                    {
//                        _Histogram[depthVal]++;
//                        points++;
//                    }
//                }
//            }

//            for (int i = 1; i < _Histogram.Length; i++)
//            {
//                _Histogram[i] += _Histogram[i - 1];
//            }

//            if (points > 0)
//            {
//                for (int i = 1; i < _Histogram.Length; i++)
//                {
//                    _Histogram[i] = (int)(256 * (1.0f - (_Histogram[i] / (float)points)));
//                }
//            }
//        }
//    }
//}
