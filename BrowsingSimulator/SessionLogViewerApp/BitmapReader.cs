using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitmapReader
{
    public class BitmapReader : IDisposable
    {
        private BinaryReader reader;
        private char[] header = "VidSee video thumbnails.        ".ToCharArray();
        private long bitmapDataStartOffset;
        private int bitmapSize;

        public int BitmapWidth  { get; private set; }
        public int BitmapHeight { get; private set; }
        public int BitmapFormat { get; private set; }
        public int BitmapStride { get { return 3; } }

        public int VideoCount   { get; private set; }
        public int ShotCount    { get; private set; }
        public int FrameCount   { get; private set; }
        
        public int[] VideoOffsets       { get; private set; }
        public int[] ShotOffsets        { get; private set; }
        public int[] VideoShotOffsets   { get; private set; }

        public int[] FrameToVideoId         { get; private set; }
        public int[] FrameToShotId          { get; private set; }
        public int[] FrameToVideoFrameId    { get; private set; }


        public BitmapReader(string filename)
        {
            reader = new BinaryReader(File.OpenRead(filename));
            ReadHeader();
        }

        public void Dispose()
        {
            reader.Dispose();
        }


        private void ReadHeader()
        {
            char[] headerCheck = reader.ReadChars(header.Length);
            BitmapWidth = reader.ReadInt32();
            BitmapHeight = reader.ReadInt32();
            BitmapFormat = reader.ReadInt32();
            VideoCount = reader.ReadInt32();
            ShotCount = reader.ReadInt32();
            FrameCount = reader.ReadInt32();
            
            VideoOffsets = new int[VideoCount];
            ShotOffsets = new int[ShotCount];
            VideoShotOffsets = new int[VideoCount];

            FrameToVideoId = new int[FrameCount];
            FrameToShotId = new int[FrameCount];
            FrameToVideoFrameId = new int[FrameCount];

            for (int i = 0; i < VideoCount; i++)
            {
                VideoOffsets[i] = reader.ReadInt32();
            }
            for (int i = 0; i < ShotCount; i++)
            {
                ShotOffsets[i] = reader.ReadInt32();
            }
            for (int i = 0; i < VideoCount; i++)
            {
                VideoShotOffsets[i] = reader.ReadInt32();
            }

            for (int i = 0; i < FrameCount; i++)
            {
                FrameToVideoId[i] = reader.ReadInt32();
            }
            for (int i = 0; i < FrameCount; i++)
            {
                FrameToShotId[i] = reader.ReadInt32();
            }
            for (int i = 0; i < FrameCount; i++)
            {
                FrameToVideoFrameId[i] = reader.ReadInt32();
            }

            int metadataSize =
                    header.Length
                    + 6 * sizeof(int)   // width, height, imageFormat, videoCount, shotCount, frameCount, 
                    + VideoOffsets.Length * sizeof(int)     // video offsets
                    + ShotCount * sizeof(int)               // shot offsets
                    + VideoOffsets.Length * sizeof(int)     // video-shot mapping offsets
                    + 3 * FrameCount * sizeof(int);         // mapping from frameID
            const int blockSize = 4096;
            bitmapDataStartOffset = ((metadataSize / blockSize) + 1) * blockSize;
            bitmapSize = BitmapWidth * BitmapHeight * BitmapStride;
        }


        public Bitmap ReadFrame(int frameId)
        {
            Bitmap bitmap = new Bitmap(BitmapWidth, BitmapHeight, PixelFormat.Format24bppRgb);

            Rectangle rect = new Rectangle(0, 0, BitmapWidth, BitmapHeight);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            
            // Get the address of the first line.
            IntPtr ptr = bitmapData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            reader.BaseStream.Seek(bitmapDataStartOffset + (long)frameId * bitmapSize, SeekOrigin.Begin);
            byte[] rgbValues = reader.ReadBytes(bitmapSize);

            // Copy the RGB values into the bitmap.
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bitmapSize);
            
            // Unlock the bits.
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        public Bitmap[] ReadVideo(int videoId)
        {
            int frameCount = GetFrameCount(videoId);
            Bitmap[] bitmaps = new Bitmap[frameCount];

            reader.BaseStream.Seek(bitmapDataStartOffset + (long)VideoOffsets[videoId] * bitmapSize, SeekOrigin.Begin);
            for (int i = 0; i < frameCount; i++)
            {
                bitmaps[i] = new Bitmap(BitmapWidth, BitmapHeight, PixelFormat.Format24bppRgb);

                Rectangle rect = new Rectangle(0, 0, BitmapWidth, BitmapHeight);
                BitmapData bitmapData = bitmaps[i].LockBits(rect, ImageLockMode.ReadWrite, bitmaps[i].PixelFormat);
                
                // Get the address of the first line.
                IntPtr ptr = bitmapData.Scan0;

                // Declare an array to hold the bytes of the bitmap.
                byte[] rgbValues = reader.ReadBytes(bitmapSize);

                // Copy the RGB values into the bitmap.
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bitmapSize);

                // Unlock the bits.
                bitmaps[i].UnlockBits(bitmapData);
            }
            return bitmaps;
        }

        public Bitmap[] ReadShot(int videoId, int shotId)
        {
            int frameCount = GetFrameCount(videoId, shotId);
            Bitmap[] bitmaps = new Bitmap[frameCount];

            int shotOffset = VideoShotOffsets[videoId] + shotId;
            int bitmapOffset = ShotOffsets[shotOffset];
            long offset = bitmapDataStartOffset + (long)bitmapOffset * bitmapSize;
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < frameCount; i++)
            {
                bitmaps[i] = new Bitmap(BitmapWidth, BitmapHeight, PixelFormat.Format24bppRgb);

                Rectangle rect = new Rectangle(0, 0, BitmapWidth, BitmapHeight);
                BitmapData bitmapData = bitmaps[i].LockBits(rect, ImageLockMode.ReadWrite, bitmaps[i].PixelFormat);

                // Get the address of the first line.
                IntPtr ptr = bitmapData.Scan0;

                // Declare an array to hold the bytes of the bitmap.
                byte[] rgbValues = reader.ReadBytes(bitmapSize);

                // Copy the RGB values into the bitmap.
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bitmapSize);

                // Unlock the bits.
                bitmaps[i].UnlockBits(bitmapData);
            }
            return bitmaps;
        }


        public int GetVideoId(int globalFrameId)
        {
            return FrameToVideoId[globalFrameId];
        }

        public int GetShotId(int globalFrameId)
        {
            return FrameToShotId[globalFrameId];
        }

        public int GetShotId(int videoId, int videoFrameId)
        {
            return FrameToShotId[GetGlobalFrameId(videoId, videoFrameId)];
        }

        public int GetVideoFrameId(int globalFrameId)
        {
            return FrameToVideoFrameId[globalFrameId];
        }


        public int GetGlobalFrameId(int videoId, int videoFrameId)
        {
            int frameOffset = VideoOffsets[videoId] + videoFrameId;
            return frameOffset;
        }

        public int GetGlobalShotFrameId(int videoId, int shotId)
        {
            int shotOffset = VideoShotOffsets[videoId] + shotId;
            return shotOffset;
        }

        public int GetGlobalVideoFrameId(int videoId)
        {
            return VideoOffsets[videoId];
        }


        public int GetFrameCount(int videoId)
        {
            if (videoId == VideoOffsets.Length - 1)
            {
                return FrameCount - VideoOffsets[videoId];
            }
            else
            {
                return VideoOffsets[videoId + 1] - VideoOffsets[videoId];
            }
        }

        public int GetFrameCount(int videoId, int shotId)
        {
            int shotOffset = VideoShotOffsets[videoId] + shotId;
            int featureOffset = ShotOffsets[shotOffset];

            // no features for this shot
            if (featureOffset == -1)
            {
                return 0;
            }

            // find the next shot offset
            int nextShotOffset = shotOffset + 1;
            while (ShotOffsets[nextShotOffset] == -1 && (nextShotOffset != ShotOffsets.Length))
            {
                nextShotOffset++;
            }

            if (shotOffset == ShotOffsets.Length - 1 || nextShotOffset == ShotOffsets.Length)
            {
                return FrameCount - featureOffset;
            }
            else
            {
                return ShotOffsets[nextShotOffset] - featureOffset;
            }
        }

        public int GetShotCount(int videoId)
        {
            int shotOffset = VideoShotOffsets[videoId];
            if (videoId == VideoShotOffsets.Length - 1)
            {
                return ShotCount - shotOffset;
            }
            else
            {
                return VideoShotOffsets[videoId + 1] - shotOffset;
            }
        }

    }
}
