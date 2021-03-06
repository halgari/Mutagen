﻿using Loqui.Xml;
using Mutagen.Bethesda.Binary;
using Mutagen.Bethesda.Internals;
using Noggog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Skyrim
{
    namespace Internals
    {
        public partial class ListGroupBinaryCreateTranslation<T>
        {
            static partial void FillBinaryContainedRecordTypeCustom(
                MutagenFrame frame,
                IListGroup<T> item)
            {
                frame.Reader.Position += 4;
            }
        }

        public partial class ListGroupBinaryWriteTranslation
        {
            static partial void WriteBinaryContainedRecordTypeCustom<T>(
                MutagenWriter writer,
                IListGroupGetter<T> item)
                where T : class, ICellBlockGetter, IXmlItem, IBinaryItem
            {
                Mutagen.Bethesda.Binary.Int32BinaryTranslation.Instance.Write(
                    writer,
                    GroupRecordTypeGetter<T>.GRUP_RECORD_TYPE.TypeInt);
            }
        }

        public partial class ListGroupBinaryOverlay<T>
        {
            private AListGroup.GroupListOverlay<T>? _Records;
            public IReadOnlyList<T> Records => _Records!;

            partial void CustomFactoryEnd(
                OverlayStream stream,
                int finalPos,
                int offset)
            {
                _Records = AListGroup.GroupListOverlay<T>.Factory(
                    stream,
                    _data,
                    _package,
                    offset: offset,
                    objectType: Mutagen.Bethesda.Binary.ObjectType.Group);
            }
        }
    }
}
