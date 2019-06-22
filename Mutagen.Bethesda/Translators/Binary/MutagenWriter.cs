﻿using Mutagen.Bethesda.Internals;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Binary
{
    public class MutagenWriter : IDisposable
    {
        private bool dispose = true;
        public System.IO.BinaryWriter Writer;
        private static byte Zero = 0;
        public Stream BaseStream { get; private set; }
        public GameMode GameMode { get; private set; }

        public long Position
        {
            get => this.BaseStream.Position;
            set => this.BaseStream.Position = value;
        }

        public long Length
        {
            get => this.BaseStream.Length;
        }

        public MutagenWriter(string path, GameMode gameMode)
        {
            this.BaseStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            this.Writer = new BinaryWriter(this.BaseStream);
            this.GameMode = gameMode;
        }

        public MutagenWriter(Stream stream, GameMode gameMode, bool dispose = true)
        {
            this.dispose = dispose;
            this.BaseStream = stream;
            this.Writer = new BinaryWriter(stream);
            this.GameMode = gameMode;
        }

        public MutagenWriter(System.IO.BinaryWriter reader, GameMode gameMode)
        {
            this.Writer = reader;
            this.GameMode = gameMode;
        }

        public void Write(bool b)
        {
            this.Writer.Write(b);
        }

        public void Write(byte b)
        {
            this.Writer.Write(b);
        }

        public void Write(byte[] b)
        {
            this.Writer.Write(b);
        }

        public void Write(ReadOnlySpan<byte> b)
        {
            // ToDo
            // Remove ToArray call when direct span parameter added in future .NET
            this.Writer.Write(b.ToArray());
        }

        public void Write(ushort b)
        {
            this.Writer.Write(b);
        }

        public void Write(uint b)
        {
            this.Writer.Write(b);
        }

        public void Write(ulong b)
        {
            this.Writer.Write(b);
        }

        public void Write(sbyte s)
        {
            this.Writer.Write(s);
        }

        public void Write(short s)
        {
            this.Writer.Write(s);
        }

        public void Write(int i)
        {
            this.Writer.Write(i);
        }

        public void Write(long i)
        {
            this.Writer.Write(i);
        }

        public void Write(float i)
        {
            this.Writer.Write(i);
        }

        public void Write(double i)
        {
            this.Writer.Write(i);
        }

        public void Write(char c)
        {
            this.Writer.Write(c);
        }

        public void Write(char[] c)
        {
            this.Writer.Write(c);
        }

        public void WriteZeros(uint num)
        {
            for (uint i = 0; i < num; i++)
            {
                this.Write(Zero);
            }
        }

        public void Write(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                var c = str[i];
                bytes[i] = (byte)c;
            }
            this.Writer.Write(bytes);
        }

        public void Write(Color color)
        {
            this.Writer.Write(color.R);
            this.Writer.Write(color.G);
            this.Writer.Write(color.B);
        }

        public void Dispose()
        {
            if (dispose)
            {
                this.Writer.Dispose();
            }
        }
    }
}
