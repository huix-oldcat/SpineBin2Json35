/**************************************************************************
    This file is part of SpineBin2Json.

    SpineBin2Json is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SpineBin2Json is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SpineBin2Json.  If not, see <https://www.gnu.org/licenses/>.
**************************************************************************/
using System;
using System.IO;
using System.Collections.Generic;

namespace SpineBin2Json35
{
    public class ColorTranslator
    {
        public static string RgbaToString(int r, int g, int b, int a) => ColorToString((r << 24) | (g << 16) | (b << 8) | a, 8);

        public static string RgbToString(int r, int g, int b) => ColorToString((r << 16) | (g << 8) | b, 6);

        public static string ColorToString(int color, int length) => color.ToString($"X{length}");
    }

    public partial class BinaryReader
    {
        private float[] ReadFloatArray(Stream input, int n, float scale)
        {
            float[] array = new float[n];
            if (scale == 1)
            {
                for (int i = 0; i < n; i++)
                    array[i] = ReadFloat(input);
            }
            else
            {
                for (int i = 0; i < n; i++)
                    array[i] = ReadFloat(input) * scale;
            }
            return array;
        }

        private int[] ReadShortArray(Stream input)
        {
            int n = ReadVarint(input, true);
            int[] array = new int[n];
            for (int i = 0; i < n; i++)
                array[i] = (input.ReadByte() << 8) | input.ReadByte();
            return array;
        }

        private static sbyte ReadSByte(Stream input)
        {
            int value = input.ReadByte();
            if (value == -1) throw new EndOfStreamException();
            return (sbyte)value;
        }

        private static bool ReadBoolean(Stream input)
        {
            return input.ReadByte() != 0;
        }

        private float ReadFloat(Stream input)
        {
            var buffer = new byte[4];
            buffer[3] = (byte)input.ReadByte();
            buffer[2] = (byte)input.ReadByte();
            buffer[1] = (byte)input.ReadByte();
            buffer[0] = (byte)input.ReadByte();
            return BitConverter.ToSingle(buffer, 0);
        }

        private static int ReadInt(Stream input)
        {
            return (input.ReadByte() << 24) + (input.ReadByte() << 16) + (input.ReadByte() << 8) + input.ReadByte();
        }

        private static int ReadVarint(Stream input, bool optimizePositive)
        {
            int b = input.ReadByte();
            int result = b & 0x7F;
            if ((b & 0x80) != 0)
            {
                b = input.ReadByte();
                result |= (b & 0x7F) << 7;
                if ((b & 0x80) != 0)
                {
                    b = input.ReadByte();
                    result |= (b & 0x7F) << 14;
                    if ((b & 0x80) != 0)
                    {
                        b = input.ReadByte();
                        result |= (b & 0x7F) << 21;
                        if ((b & 0x80) != 0) result |= (input.ReadByte() & 0x7F) << 28;
                    }
                }
            }
            return optimizePositive ? result : ((result >> 1) ^ -(result & 1));
        }

        private string ReadString(Stream input)
        {
            int byteCount = ReadVarint(input, true);
            switch (byteCount)
            {
                case 0:
                    return null;
                case 1:
                    return "";
            }
            byteCount--;
            byte[] buffer = new byte[byteCount];
            ReadFully(input, buffer, 0, byteCount);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, byteCount);
        }

        private static void ReadFully(Stream input, byte[] buffer, int offset, int length)
        {
            while (length > 0)
            {
                int count = input.Read(buffer, offset, length);
                if (count <= 0) throw new EndOfStreamException();
                offset += count;
                length -= count;
            }
        }

        private static int[] GetDefaultEdges(int hull)
        {
            hull >>= 1;
            hull -= 1;
            List<int> o = new List<int>();
            o.Add(0);
            for (int i = 1; i <= hull; ++i)
            {
                o.Add(i * 2);
                o.Add(i * 2);
            }
            o.Add(0);
            return o.ToArray();
        }
    }
}