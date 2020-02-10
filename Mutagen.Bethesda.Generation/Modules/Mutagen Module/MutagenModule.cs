﻿using Loqui.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loqui;
using System.Xml.Linq;
using Noggog;
using Mutagen.Bethesda.Binary;

namespace Mutagen.Bethesda.Generation
{
    public class MutagenModule : GenerationModule
    {
        public override string RegionString => "Mutagen";

        public MutagenModule()
        {
            this.SubModules.Add(new TriggeringRecordModule());
            this.SubModules.Add(new GenericsModule());
            this.SubModules.Add(new SpecialParseModule());
            this.SubModules.Add(new DataTypeModule());
            this.SubModules.Add(new RecordTypeConverterModule());
            this.SubModules.Add(new NumFieldsModule());
            this.SubModules.Add(new CorrectnessModule());
            this.SubModules.Add(new ModModule());
            this.SubModules.Add(new ColorTypeModule());
            this.SubModules.Add(new ListTypeModule());
            this.SubModules.Add(new DictTypeModule());
            this.SubModules.Add(new LinkModule());
            this.SubModules.Add(new FolderExportModule());
            this.SubModules.Add(new ReactiveModule());
            this.SubModules.Add(new MajorRecordModule());
            this.SubModules.Add(new MajorRecordEnumerationModule());
        }

        public override async Task PostFieldLoad(ObjectGeneration obj, TypeGeneration field, XElement node)
        {
            var data = field.CustomData.TryCreateValue(Constants.DataKey, () => new MutagenFieldData(field)) as MutagenFieldData;
            data.Binary = node.GetAttribute<BinaryGenerationType>(Constants.Binary, BinaryGenerationType.Normal);
            data.BinaryOverlay = node.GetAttribute<BinaryGenerationType?>(Constants.BinaryOverlay, default);
            ModifyGRUPAttributes(field);
            await base.PostFieldLoad(obj, field, node);
            data.Length = node.GetAttribute<int?>(Constants.ByteLength, null);
            if (!data.Length.HasValue
                && !data.RecordType.HasValue
                && !(field is NothingType)
                && !(field is PrimitiveType)
                && !(field is FormLinkType)
                && !(field is ContainerType)
                && !(field is DictType))
            {
                throw new ArgumentException($"{obj.Name} {field.Name} have to define either length or record type.");
            }
        }

        private void ModifyGRUPAttributes(TypeGeneration field)
        {
            if (!(field is LoquiType loqui)) return;
            if (loqui.TargetObjectGeneration?.GetObjectType() != ObjectType.Group) return;
            loqui.SingletonType = SingletonLevel.Singleton;
            loqui.HasBeenSetProperty.OnNext(false);
            loqui.NotifyingProperty.OnNext(NotifyingType.None);
        }
    }
}
