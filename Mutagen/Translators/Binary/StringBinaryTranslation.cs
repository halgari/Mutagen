﻿using Noggog;
using System;
using System.IO;

namespace Mutagen.Binary
{
    public class StringBinaryTranslation : IBinaryTranslation<string, Exception>
    {
        public readonly static StringBinaryTranslation Instance = new StringBinaryTranslation();

        public TryGet<string> Parse(BinaryReader reader, int length, bool doMasks, out Exception errorMask)
        {
            try
            {
                errorMask = null;
                var str = new string(reader.ReadChars(length));
                str= str.TrimEnd('\0');
                return TryGet<string>.Succeed(str);
            }
            catch (Exception ex)
            {
                if (doMasks)
                {
                    errorMask = ex;
                    return TryGet<string>.Failure;
                }
                throw;
            }
        }

        public void Write(BinaryWriter writer, string item, bool doMasks, out Exception errorMask)
        {
            try
            {
                writer.Write(item.ToCharArray());
                writer.Write((byte)0);
                errorMask = null;
            }
            catch (Exception ex)
            when (doMasks)
            {
                errorMask = ex;
            }
        }

        public void Write(
            BinaryWriter writer, 
            string item, 
            RecordType header,
            bool nullable,
            bool doMasks,
            out Exception errorMask)
        {
            if (item == null)
            {
                if (nullable)
                {
                    errorMask = null;
                    return;
                }
                throw new ArgumentException("Non optional string was null.");
            }
            try
            {
                using (HeaderExport.ExportHeader(writer, header, ObjectType.Subrecord))
                {
                    this.Write(writer, item, doMasks, out errorMask);
                }
            }
            catch (Exception ex)
            when (doMasks)
            {
                errorMask = ex;
            }
        }
    }
}
