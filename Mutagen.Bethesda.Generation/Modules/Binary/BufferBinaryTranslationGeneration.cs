﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loqui;
using Loqui.Generation;

namespace Mutagen.Bethesda.Generation
{
    public class BufferBinaryTranslationGeneration : BinaryTranslationGeneration
    {
        public override string GetTranslatorInstance(TypeGeneration typeGen, bool getter)
        {
            return $"ByteArrayBinaryTranslation.Instance";
        }

        public override void GenerateCopyIn(
            FileGeneration fg,
            ObjectGeneration objGen,
            TypeGeneration typeGen,
            Accessor readerAccessor,
            Accessor itemAccessor,
            Accessor errorMaskAccessor,
            Accessor translationMaskAccessor)
        {
            BufferType zero = typeGen as BufferType;
            fg.AppendLine($"{readerAccessor}.Position += {zero.Length};");
        }

        public override void GenerateCopyInRet(
            FileGeneration fg,
            ObjectGeneration objGen,
            TypeGeneration targetGen,
            TypeGeneration typeGen,
            Accessor readerAccessor,
            AsyncMode asyncMode,
            Accessor retAccessor,
            Accessor outItemAccessor,
            Accessor errorMaskAccessor,
            Accessor translationAccessor,
            Accessor mastersAccessor)
        {
            if (asyncMode == AsyncMode.Direct) throw new NotImplementedException();
            BufferType buf = typeGen as BufferType;
            fg.AppendLine($"{readerAccessor}.Position += {buf.Length};");
        }

        public override void GenerateWrite(
            FileGeneration fg,
            ObjectGeneration objGen,
            TypeGeneration typeGen,
            Accessor writerAccessor,
            Accessor itemAccessor,
            Accessor errorMaskAccessor,
            Accessor translationMaskAccessor,
            Accessor mastersAccessor)
        {
            BufferType zero = typeGen as BufferType;
            using (var args = new ArgsWrapper(fg,
                $"{this.Namespace}ByteArrayBinaryTranslation.Instance.Write"))
            {
                args.Add($"writer: {writerAccessor}");
                if (zero.Static)
                {
                    args.Add($"item: {objGen.CommonClassName(LoquiInterfaceType.IGetter, MaskType.Normal)}.{typeGen.Name}");
                }
                else
                {
                    args.Add($"item: {itemAccessor.PropertyOrDirectAccess}");
                }
            }
        }
        
        public override int? ExpectedLength(ObjectGeneration objGen, TypeGeneration typeGen)
        {
            BufferType buf = typeGen as BufferType;
            return buf.Length;
        }
    }
}
