namespace FairyGUI.Utils
{
    /// <summary>
    ///     一个简单的Zip文件处理类。不处理解压。
    /// </summary>
    public class ZipReader
    {
        private int _index;
        private int _pos;

        private readonly ByteBuffer _stream;

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public ZipReader(byte[] data)
        {
            _stream = new ByteBuffer(data);
            _stream.littleEndian = true;

            var pos = _stream.length - 22;
            _stream.position = pos + 10;
            entryCount = _stream.ReadShort();
            _stream.position = pos + 16;
            _pos = _stream.ReadInt();
        }

        /// <summary>
        /// </summary>
        public int entryCount { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool GetNextEntry(ZipEntry entry)
        {
            if (_index >= entryCount)
                return false;

            _stream.position = _pos + 28;
            int len = _stream.ReadUshort();
            var len2 = _stream.ReadUshort() + _stream.ReadUshort();

            _stream.position = _pos + 46;
            var name = _stream.ReadString(len);
            name = name.Replace("\\", "/");

            entry.name = name;
            if (name[name.Length - 1] == '/') //directory
            {
                entry.isDirectory = true;
                entry.compress = 0;
                entry.crc = 0;
                entry.size = entry.sourceSize = 0;
                entry.offset = 0;
            }
            else
            {
                entry.isDirectory = false;
                _stream.position = _pos + 10;
                entry.compress = _stream.ReadUshort();
                _stream.position = _pos + 16;
                entry.crc = _stream.ReadUint();
                entry.size = _stream.ReadInt();
                entry.sourceSize = _stream.ReadInt();
                _stream.position = _pos + 42;
                entry.offset = _stream.ReadInt() + 30 + len;
            }

            _pos += 46 + len + len2;
            _index++;

            return true;
        }


        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] GetEntryData(ZipEntry entry)
        {
            var data = new byte[entry.size];
            if (entry.size > 0)
            {
                _stream.position = entry.offset;
                _stream.ReadBytes(data, 0, entry.size);
            }

            return data;
        }

        /// <summary>
        /// </summary>
        public class ZipEntry
        {
            public int compress;
            public uint crc;
            public bool isDirectory;
            public string name;
            public int offset;
            public int size;
            public int sourceSize;
        }
    }
}