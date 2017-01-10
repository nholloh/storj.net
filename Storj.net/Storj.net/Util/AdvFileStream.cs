using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    internal class AdvFileStream : FileStream
    {
        internal bool Closed { get; private set; }

        [DebuggerStepThrough]
        internal AdvFileStream(String Path, FileMode FileMode) : base(Path, FileMode)
        {
            this.Closed = false;
        }

        [DebuggerStepThrough]
        internal byte[] Read(long StartIndex, long Length)
        {
            byte[] _filePart = new byte[Length];
            this.Position = StartIndex;

            long _index = 0;
            byte[] _buffer;
            while (Length > Int32.MaxValue)
            {
                _buffer = new byte[Int32.MaxValue];
                base.Read(_buffer, 0, Int32.MaxValue);

                _buffer.CopyTo(_filePart, _index);

                _index += Int32.MaxValue;
                Length -= Int32.MaxValue;
            }
            _buffer = new byte[Length];
            base.Read(_buffer, 0, (int)Length);
            _buffer.CopyTo(_filePart, _index);

            return _filePart;
        }

        /*public override int Read(byte[] array, int offset, int count)
        {
            Parent.LastAccess = DateTime.Now;
            return base.Read(array, offset, count);
        }*/

        [DebuggerStepThrough]
        internal byte[] Read(long Length)
        {
            byte[] _filePart = new byte[Length];

            long _index = 0;
            byte[] _buffer;
            while (Length > Int32.MaxValue)
            {
                _buffer = new byte[Int32.MaxValue];
                base.Read(_buffer, 0, Int32.MaxValue);

                _buffer.CopyTo(_filePart, _index);

                _index += Int32.MaxValue;
                Length -= Int32.MaxValue;
            }
            _buffer = new byte[Length];
            base.Read(_buffer, 0, (int)Length);
            _buffer.CopyTo(_filePart, _index);

            return _filePart;
        }

        internal void Write(byte[] Data)
        {
            base.Write(Data, 0, Data.Length);
        }

        internal void Write(string Text)
        {
            byte[] _buffer = new byte[Text.Length + 1];
            Encoding.UTF8.GetBytes(Text, 0, Text.Length, _buffer, 0);
            _buffer[Text.Length] = 0;

            Write(_buffer);
        }

        /*public void Write(string Text)
        {
            Write(Text.Length);
            Write(Encoding.UTF8.GetBytes(Text));
        }*/

        internal void Write(short Short)
        {
            Write(BitConverter.GetBytes(Short));
        }

        internal void Write(int Integer)
        {
            Write(BitConverter.GetBytes(Integer));
        }

        internal void Write(long Long)
        {
            Write(BitConverter.GetBytes(Long));
        }

        internal void Write(object Object)
        {
            new BinaryFormatter().Serialize(this, Object);
        }

        [DebuggerStepThrough]
        internal short ReadShort()
        {
            return BitConverter.ToInt16(Read(2), 0);
        }

        [DebuggerStepThrough]
        internal int ReadInt()
        {
            return BitConverter.ToInt32(Read(4), 0);
        }

        [DebuggerStepThrough]
        internal long ReadLong()
        {
            return BitConverter.ToInt64(Read(8), 0);
        }

        [DebuggerStepThrough]
        internal string ReadString()
        {
            List<byte> _data = new List<byte>();

            byte _char = Read(1).First();
            while (_char != 0)
            {
                _data.Add(_char);
                _char = Read(1).First();
            }

            return new String(Encoding.UTF8.GetChars(_data.ToArray(), 0, _data.Count));
        }

        /*public string ReadString()
        {
            int length = ReadInt();
            byte[] stringBuffer = Read(length);
            return Encoding.UTF8.GetString(stringBuffer);
        }*/

        [DebuggerStepThrough]
        internal string ReadLine()
        {
            List<byte> _data = new List<byte>();

            byte _char = Read(1).First();
            while (_char != '\n')
            {
                _data.Add(_char);
                _char = Read(1).First();
            }

            return new String(Encoding.UTF8.GetChars(_data.ToArray(), 0, _data.Count));
        }

        [DebuggerStepThrough]
        internal char ReadChar()
        {
            return (char)Read(1).First();
        }

        internal object ReadObject()
        {
            return new BinaryFormatter().Deserialize(this);
        }

        public override void Close()
        {
            base.Close();
            Closed = true;
        }
    }
}
