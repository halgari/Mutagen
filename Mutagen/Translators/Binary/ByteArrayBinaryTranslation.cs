﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Binary
{
    public class ByteArrayBinaryTranslation : TypicalBinaryTranslation<byte[]>
    {
        public readonly static ByteArrayBinaryTranslation Instance = new ByteArrayBinaryTranslation();

        protected override void WriteValue(BinaryWriter writer, byte[] item)
        {
            writer.Write(item);
        }

        protected override Byte[] ParseValue(BinaryReader reader, int length)
        {
            return reader.ReadBytes(length);
        }

        protected override byte[] ParseBytes(byte[] bytes)
        {
            return bytes;
        }
    }
}
