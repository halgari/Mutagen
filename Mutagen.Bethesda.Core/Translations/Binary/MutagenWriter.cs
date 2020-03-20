using Mutagen.Bethesda.Internals;
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
        public Stream BaseStream { get; }
        public GameConstants Meta { get; }
        public MasterReferenceReader? MasterReferences { get; set; }

        public long Position
        {
            get => this.BaseStream.Position;
            set => this.BaseStream.Position = value;
        }

        public long Length
        {
            get => this.BaseStream.Length;
        }

        public MutagenWriter(string path, GameConstants meta, MasterReferenceReader? masterReferences = null)
        {
            this.BaseStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            this.Writer = new BinaryWriter(this.BaseStream);
            this.Meta = meta;
            this.MasterReferences = masterReferences;
        }

        public MutagenWriter(Stream stream, GameConstants meta, MasterReferenceReader? masterReferences = null, bool dispose = true)
        {
            this.dispose = dispose;
            this.BaseStream = stream;
            this.Writer = new BinaryWriter(stream);
            this.Meta = meta;
            this.MasterReferences = masterReferences;
        }

        public MutagenWriter(System.IO.BinaryWriter writer, GameConstants meta)
        {
            this.BaseStream = writer.BaseStream;
            this.Writer = writer;
            this.Meta = meta;
        }

        public void Write(bool b)
        {
            this.Writer.Write(b);
        }

        public void Write(bool? b)
        {
            if (!b.HasValue) return;
            this.Writer.Write(b.Value);
        }

        public void Write(byte b)
        {
            this.Writer.Write(b);
        }

        public void Write(byte? b)
        {
            if (!b.HasValue) return;
            this.Writer.Write(b.Value);
        }

        public void Write(byte[]? b)
        {
            if (b == null) return;
            this.Writer.Write(b);
        }

        public void Write(ReadOnlySpan<byte> b)
        {
            this.Writer.Write(b);
        }

        public void Write(ushort b)
        {
            this.Writer.Write(b);
        }

        public void Write(ushort? b)
        {
            if (!b.HasValue) return;
            this.Writer.Write(b.Value);
        }

        public void Write(uint b)
        {
            this.Writer.Write(b);
        }

        public void Write(uint? b)
        {
            if (!b.HasValue) return;
            this.Writer.Write(b.Value);
        }

        public void Write(ulong b)
        {
            this.Writer.Write(b);
        }

        public void Write(ulong? b)
        {
            if (!b.HasValue) return;
            this.Writer.Write(b.Value);
        }

        public void Write(sbyte s)
        {
            this.Writer.Write(s);
        }

        public void Write(sbyte? s)
        {
            if (!s.HasValue) return;
            this.Writer.Write(s.Value);
        }

        public void Write(short s)
        {
            this.Writer.Write(s);
        }

        public void Write(short? s)
        {
            if (!s.HasValue) return;
            this.Writer.Write(s.Value);
        }

        public void Write(int i)
        {
            this.Writer.Write(i);
        }

        public void Write(int? i)
        {
            if (!i.HasValue) return;
            this.Writer.Write(i.Value);
        }

        public void Write(long i)
        {
            this.Writer.Write(i);
        }

        public void Write(long? i)
        {
            if (!i.HasValue) return;
            this.Writer.Write(i.Value);
        }

        public void Write(float i)
        {
            this.Writer.Write(i);
        }

        public void Write(float? i)
        {
            if (!i.HasValue) return;
            this.Writer.Write(i.Value);
        }

        public void Write(double i)
        {
            this.Writer.Write(i);
        }

        public void Write(double? i)
        {
            if (!i.HasValue) return;
            this.Writer.Write(i.Value);
        }

        public void Write(char c)
        {
            this.Writer.Write(c);
        }

        public void Write(char? c)
        {
            if (!c.HasValue) return;
            this.Writer.Write(c.Value);
        }

        public void WriteZeros(uint num)
        {
            for (uint i = 0; i < num; i++)
            {
                this.Write(Zero);
            }
        }

        public void Write(ReadOnlySpan<char> str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                var c = str[i];
                bytes[i] = (byte)c;
            }
            this.Writer.Write(bytes);
        }

        public void Write(string? str)
        {
            if (str == null) return;
            Write(str.AsSpan());
        }

        public void Write(Color color, bool extraByte)
        {
            this.Writer.Write(color.R);
            this.Writer.Write(color.G);
            this.Writer.Write(color.B);
            if (extraByte)
            {
                this.Writer.Write(color.A);
            }
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