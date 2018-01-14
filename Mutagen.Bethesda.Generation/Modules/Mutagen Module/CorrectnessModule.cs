﻿using Loqui.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Generation
{
    public class CorrectnessModule : GenerationModule
    {
        public override async Task PostLoad(ObjectGeneration obj)
        {
            var data = obj.GetObjectData();
            await data.WiringComplete.Task;
            Dictionary<string, TypeGeneration> triggerMapping = new Dictionary<string, TypeGeneration>();
            Dictionary<RecordType, TypeGeneration> triggerRecMapping = new Dictionary<RecordType, TypeGeneration>();
            foreach (var field in obj.IterateFields())
            {
                if (!field.TryGetFieldData(out var mutaData)) continue;
                if (!mutaData.HasTrigger) continue;
                foreach (var trigger in mutaData.TriggeringRecordAccessors)
                {
                    if (triggerMapping.TryGetValue(trigger, out var existingField))
                    {
                        throw new ArgumentException($"{obj.Name} cannot have two fields that have the same trigger {trigger}: {existingField.Name} AND {field.Name}");
                    }
                    triggerMapping[trigger] = field;
                }
                foreach (var triggerRec in mutaData.TriggeringRecordTypes)
                {
                    if (triggerRecMapping.TryGetValue(triggerRec, out var existingField))
                    {
                        throw new ArgumentException($"{obj.Name} cannot have two fields that have the same trigger record {triggerRec}: {existingField.Name} AND {field.Name}");
                    }
                    triggerRecMapping[triggerRec] = field;
                }
            }

            bool triggerEncountered = false;
            foreach (var field in obj.IterateFields(
                nonIntegrated: true,
                expandSets: SetMarkerType.ExpandSets.False))
            {
                if (field is SetMarkerType) continue;
                if (field.Derivative) continue;
                var hasTrigger = field.TryGetFieldData(out var fieldData)
                    && fieldData.HasTrigger;
                if (hasTrigger)
                {
                    triggerEncountered = true;
                }
                else if (triggerEncountered)
                {
                    throw new ArgumentException($"{obj.Name} cannot have an embedded field without a record type after ones with record types have been defined: {field.Name}");
                }
            }
            await base.PostLoad(obj);
        }
    }
}