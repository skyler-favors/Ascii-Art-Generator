﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;


namespace ASCII_Generator_Simple {
    class gifSupport {
        // Gdi+ constants absent from System.Drawing.
        const int PropertyTagFrameDelay = 0x5100;
        const int PropertyTagLoopCount = 0x5101;
        const short PropertyTagTypeLong = 4;
        const short PropertyTagTypeShort = 3;
        const int UintBytes = 4;


        public string Start(List<Bitmap> bitmaps) {
            var gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            var encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            var encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            // Params for the finalizing call.
            var encoderParamsFlush = new EncoderParameters(1);
            encoderParamsFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);

            // PropertyItem for the frame delay (apparently, no other way to create a fresh instance).
            var frameDelay = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            frameDelay.Id = PropertyTagFrameDelay;
            frameDelay.Type = PropertyTagTypeLong;
            // Length of the value in bytes.
            frameDelay.Len = bitmaps.Count * UintBytes;
            // The value is an array of 4-byte entries: one per frame.
            // Every entry is the frame delay in 1/100-s of a second, in little endian.
            frameDelay.Value = new byte[bitmaps.Count * UintBytes];
            // E.g., here, we're setting the delay of every frame to 1 second.
            var frameDelayBytes = BitConverter.GetBytes((uint)8);
            for (int j = 0; j < bitmaps.Count; ++j)
                Array.Copy(frameDelayBytes, 0, frameDelay.Value, j * UintBytes, UintBytes);

            // PropertyItem for the number of animation loops.
            var loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 2;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            string path = @"C:\Users\sky86\Pictures\gifs\test.gif";
            string tempPath = Path.GetTempPath() + "temp.gif";

            using (var stream = new FileStream(tempPath, FileMode.Create)) {
                bool first = true;
                Bitmap firstBitmap = null;
                // Bitmaps is a collection of Bitmap instances that'll become gif frames.
                foreach (var bitmap in bitmaps) {
                    if (first) {
                        firstBitmap = bitmap;
                        firstBitmap.SetPropertyItem(frameDelay);
                        firstBitmap.SetPropertyItem(loopPropertyItem);
                        firstBitmap.Save(stream, gifEncoder, encoderParams1);
                        first = false;
                    } else {
                        firstBitmap.SaveAdd(bitmap, encoderParamsN);
                    }
                }
                firstBitmap.SaveAdd(encoderParamsFlush);
            }
            return tempPath;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format) {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs) {
                if (codec.FormatID == format.Guid) {
                    return codec;
                }
            }
            return null;
        }

    }
}
