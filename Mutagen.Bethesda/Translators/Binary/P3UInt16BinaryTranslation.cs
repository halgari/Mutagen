﻿using Noggog;
using System;
using System.IO;

namespace Mutagen.Bethesda.Binary
{
    public class P3UInt16BinaryTranslation : PrimitiveBinaryTranslation<P3UInt16>
    {
        public readonly static P3UInt16BinaryTranslation Instance = new P3UInt16BinaryTranslation();
        public override int? ExpectedLength => 1;

        public override P3UInt16 ParseValue(MutagenFrame reader)
        {
            return new P3UInt16(
                reader.Reader.ReadUInt16(),
                reader.Reader.ReadUInt16(),
                reader.Reader.ReadUInt16());
        }

        public override void WriteValue(MutagenWriter writer, P3UInt16 item)
        {
            writer.Write(item.X);
            writer.Write(item.Y);
            writer.Write(item.Z);
        }
    }
}
