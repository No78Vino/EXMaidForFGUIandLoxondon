using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class ByteBuffer
    {
        private static readonly byte[] temp = new byte[8];

        private static readonly List<GPathPoint> helperPoints = new();
        private byte[] _data;
        private int _offset;

        /// <summary>
        /// </summary>
        public bool littleEndian;

        /// <summary>
        /// </summary>
        public string[] stringTable;

        /// <summary>
        /// </summary>
        public int version;

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public ByteBuffer(byte[] data, int offset = 0, int length = -1)
        {
            _data = data;
            position = 0;
            _offset = offset;
            if (length < 0)
                this.length = data.Length - offset;
            else
                this.length = length;
            littleEndian = false;
        }

        /// <summary>
        /// </summary>
        public int position { get; set; }

        /// <summary>
        /// </summary>
        public int length { get; private set; }

        /// <summary>
        /// </summary>
        public bool bytesAvailable => position < length;

        /// <summary>
        /// </summary>
        public byte[] buffer
        {
            get => _data;
            set
            {
                _data = value;
                position = 0;
                _offset = 0;
                length = _data.Length;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Skip(int count)
        {
            position += count;
            return position;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            return _data[_offset + position++];
        }

        /// <summary>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="destIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadBytes(byte[] output, int destIndex, int count)
        {
            if (count > length - position)
                throw new ArgumentOutOfRangeException();

            Array.Copy(_data, _offset + position, output, destIndex, count);
            position += count;
            return output;
        }

        /// <summary>
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadBytes(int count)
        {
            if (count > length - position)
                throw new ArgumentOutOfRangeException();

            var result = new byte[count];
            Array.Copy(_data, _offset + position, result, 0, count);
            position += count;
            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public ByteBuffer ReadBuffer()
        {
            var count = ReadInt();
            var ba = new ByteBuffer(_data, position, count);
            ba.stringTable = stringTable;
            ba.version = version;
            position += count;
            return ba;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public char ReadChar()
        {
            return (char)ReadShort();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool ReadBool()
        {
            var result = _data[_offset + position] == 1;
            position++;
            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public short ReadShort()
        {
            var startIndex = _offset + position;
            position += 2;
            if (littleEndian)
                return (short)(_data[startIndex] | (_data[startIndex + 1] << 8));
            return (short)((_data[startIndex] << 8) | _data[startIndex + 1]);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public ushort ReadUshort()
        {
            return (ushort)ReadShort();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            var startIndex = _offset + position;
            position += 4;
            if (littleEndian)
                return _data[startIndex] | (_data[startIndex + 1] << 8) | (_data[startIndex + 2] << 16) |
                       (_data[startIndex + 3] << 24);
            return (_data[startIndex] << 24) | (_data[startIndex + 1] << 16) | (_data[startIndex + 2] << 8) |
                   _data[startIndex + 3];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public uint ReadUint()
        {
            return (uint)ReadInt();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public float ReadFloat()
        {
            var startIndex = _offset + position;
            position += 4;
            if (littleEndian == BitConverter.IsLittleEndian) return BitConverter.ToSingle(_data, startIndex);

            temp[3] = _data[startIndex];
            temp[2] = _data[startIndex + 1];
            temp[1] = _data[startIndex + 2];
            temp[0] = _data[startIndex + 3];
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public long ReadLong()
        {
            var startIndex = _offset + position;
            position += 8;
            if (littleEndian)
            {
                var i1 = _data[startIndex] | (_data[startIndex + 1] << 8) | (_data[startIndex + 2] << 16) |
                         (_data[startIndex + 3] << 24);
                var i2 = _data[startIndex + 4] | (_data[startIndex + 5] << 8) | (_data[startIndex + 6] << 16) |
                         (_data[startIndex + 7] << 24);
                return (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                var i1 = (_data[startIndex] << 24) | (_data[startIndex + 1] << 16) | (_data[startIndex + 2] << 8) |
                         _data[startIndex + 3];
                var i2 = (_data[startIndex + 4] << 24) | (_data[startIndex + 5] << 16) | (_data[startIndex + 6] << 8) |
                         _data[startIndex + 7];
                return (uint)i2 | ((long)i1 << 32);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public double ReadDouble()
        {
            var startIndex = _offset + position;
            position += 8;
            if (littleEndian == BitConverter.IsLittleEndian) return BitConverter.ToDouble(_data, startIndex);

            temp[7] = _data[startIndex];
            temp[6] = _data[startIndex + 1];
            temp[5] = _data[startIndex + 2];
            temp[4] = _data[startIndex + 3];
            temp[3] = _data[startIndex + 4];
            temp[2] = _data[startIndex + 5];
            temp[1] = _data[startIndex + 6];
            temp[0] = _data[startIndex + 7];
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            var len = ReadUshort();
            var result = Encoding.UTF8.GetString(_data, _offset + position, len);
            position += len;
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public string ReadString(int len)
        {
            var result = Encoding.UTF8.GetString(_data, _offset + position, len);
            position += len;
            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string ReadS()
        {
            int index = ReadUshort();
            if (index == 65534) //null
                return null;
            if (index == 65533)
                return string.Empty;
            return stringTable[index];
        }

        /// <summary>
        /// </summary>
        /// <param name="cnt"></param>
        /// <returns></returns>
        public string[] ReadSArray(int cnt)
        {
            var ret = new string[cnt];
            for (var i = 0; i < cnt; i++)
                ret[i] = ReadS();

            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="result"></param>
        public List<GPathPoint> ReadPath()
        {
            helperPoints.Clear();

            var len = ReadInt();
            if (len == 0)
                return helperPoints;

            for (var i = 0; i < len; i++)
            {
                var curveType = (GPathPoint.CurveType)ReadByte();
                switch (curveType)
                {
                    case GPathPoint.CurveType.Bezier:
                        helperPoints.Add(new GPathPoint(new Vector3(ReadFloat(), ReadFloat(), 0),
                            new Vector3(ReadFloat(), ReadFloat(), 0)));
                        break;

                    case GPathPoint.CurveType.CubicBezier:
                        helperPoints.Add(new GPathPoint(new Vector3(ReadFloat(), ReadFloat(), 0),
                            new Vector3(ReadFloat(), ReadFloat(), 0),
                            new Vector3(ReadFloat(), ReadFloat(), 0)));
                        break;

                    default:
                        helperPoints.Add(new GPathPoint(new Vector3(ReadFloat(), ReadFloat(), 0), curveType));
                        break;
                }
            }

            return helperPoints;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        public void WriteS(string value)
        {
            int index = ReadUshort();
            if (index != 65534 && index != 65533)
                stringTable[index] = value;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Color ReadColor()
        {
            var startIndex = _offset + position;
            var r = _data[startIndex];
            var g = _data[startIndex + 1];
            var b = _data[startIndex + 2];
            var a = _data[startIndex + 3];
            position += 4;

            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// </summary>
        /// <param name="indexTablePos"></param>
        /// <param name="blockIndex"></param>
        /// <returns></returns>
        public bool Seek(int indexTablePos, int blockIndex)
        {
            var tmp = position;
            position = indexTablePos;
            int segCount = _data[_offset + position++];
            if (blockIndex < segCount)
            {
                var useShort = _data[_offset + position++] == 1;
                int newPos;
                if (useShort)
                {
                    position += 2 * blockIndex;
                    newPos = ReadShort();
                }
                else
                {
                    position += 4 * blockIndex;
                    newPos = ReadInt();
                }

                if (newPos > 0)
                {
                    position = indexTablePos + newPos;
                    return true;
                }

                position = tmp;
                return false;
            }

            position = tmp;
            return false;
        }
    }
}