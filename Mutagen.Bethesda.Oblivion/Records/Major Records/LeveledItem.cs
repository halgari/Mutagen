using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loqui.Internal;
using Mutagen.Bethesda.Binary;
using Mutagen.Bethesda.Oblivion.Internals;
using Noggog;

namespace Mutagen.Bethesda.Oblivion
{
    namespace Internals
    {
        public partial class LeveledItemBinaryCreateTranslation
        {
            static partial void FillBinaryVestigialCustom(MutagenFrame frame, ILeveledItemInternal item)
            {
                var rec = HeaderTranslation.ReadNextSubrecordType(frame.Reader, out var length);
                if (length != 1)
                {
                    throw new ArgumentException($"Unexpected length: {length}");
                }
                if (ByteBinaryTranslation.Instance.Parse(
                    frame,
                    out var parseVal)
                    && parseVal > 0)
                {
                    if (!item.Flags.HasValue)
                    {
                        item.Flags = default(LeveledFlag);
                    }
                    item.Flags |= LeveledFlag.CalculateForEachItemInCount;
                }
            }
        }

        public partial class LeveledItemBinaryOverlay
        {
            private bool _vestigialMarker;

            private int? _FlagsLocation;
            bool GetFlagsIsSetCustom() => _FlagsLocation.HasValue || _vestigialMarker;
            public LeveledFlag? GetFlagsCustom()
            {
                var ret = _FlagsLocation.HasValue ? (LeveledFlag)HeaderTranslation.ExtractSubrecordSpan(_data.Span, _FlagsLocation.Value, _package.MetaData.Constants)[0] : default(LeveledFlag?);
                if (_vestigialMarker)
                {
                    if (ret.HasValue)
                    {
                        ret |= LeveledFlag.CalculateForEachItemInCount;
                    }
                    else
                    {
                        ret = LeveledFlag.CalculateForEachItemInCount;
                    }
                }
                return ret;
            }
            partial void FlagsCustomParse(OverlayStream stream, long finalPos, int offset)
            {
                _FlagsLocation = (ushort)(stream.Position - offset);
            }

            partial void VestigialCustomParse(OverlayStream stream, int offset)
            {
                var subMeta = stream.ReadSubrecord();
                if (subMeta.ContentLength != 1)
                {
                    throw new ArgumentException($"Unexpected length: {subMeta.ContentLength}");
                }
                if (stream.ReadUInt8() > 0)
                {
                    this._vestigialMarker = true;
                }
            }
        }
    }
}
