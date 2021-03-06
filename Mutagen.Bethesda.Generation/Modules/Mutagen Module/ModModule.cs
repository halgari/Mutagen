using Loqui;
using Loqui.Generation;
using Mutagen.Bethesda.Binary;
using Mutagen.Bethesda.Internals;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Generation
{
    public class ModModule : GenerationModule
    {
        public override async IAsyncEnumerable<string> RequiredUsingStatements(ObjectGeneration obj)
        {
            if (obj.GetObjectData().ObjectType == ObjectType.Mod)
            {
                yield return "System.Collections.Concurrent";
                yield return "System.Threading.Tasks";
                yield return "System.IO";
            }
        }

        public override async IAsyncEnumerable<(LoquiInterfaceType Location, string Interface)> Interfaces(ObjectGeneration obj)
        {
            if (obj.GetObjectType() != ObjectType.Mod) yield break;
            yield return (LoquiInterfaceType.IGetter, nameof(IModGetter));
            yield return (LoquiInterfaceType.ISetter, nameof(IMod));
        }

        public override async Task GenerateInClass(ObjectGeneration obj, FileGeneration fg)
        {
            if (obj.GetObjectData().ObjectType != ObjectType.Mod) return;

            // Gamemode member
            fg.AppendLine($"public override {nameof(GameMode)} GameMode => {nameof(GameMode)}.{obj.GetObjectData().GameMode};");

            // Interfaces
            fg.AppendLine($"IReadOnlyCache<T, {nameof(FormKey)}> {nameof(IModGetter)}.{nameof(IModGetter.GetGroupGetter)}<T>() => this.GetGroupGetter<T>();");
            fg.AppendLine($"ICache<T, {nameof(FormKey)}> {nameof(IMod)}.{nameof(IMod.GetGroup)}<T>() => this.GetGroup<T>();");
            fg.AppendLine($"void IModGetter.WriteToBinary(string path, {nameof(BinaryWriteParameters)}? param) => this.WriteToBinary(path, importMask: null, param: param);");
            fg.AppendLine($"void IModGetter.WriteToBinaryParallel(string path, {nameof(BinaryWriteParameters)}? param) => this.WriteToBinaryParallel(path, param);");

            // Localization enabled member
            fg.AppendLine($"public override bool CanUseLocalization => {(obj.GetObjectData().UsesStringFiles ? "true" : "false")};");

            // Master references member
            fg.AppendLine($"[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
            fg.AppendLine($"IList<MasterReference> IMod.MasterReferences => this.ModHeader.MasterReferences;");
            fg.AppendLine($"[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
            fg.AppendLine($"IReadOnlyList<IMasterReferenceGetter> IModGetter.MasterReferences => this.ModHeader.MasterReferences;");

            // NextObjectID member
            fg.AppendLine($"[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
            fg.AppendLine($"uint IMod.NextObjectID");
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"get => this.ModHeader.Stats.NextObjectID;");
                fg.AppendLine($"set => this.ModHeader.Stats.NextObjectID = value;");
            }

            fg.AppendLine($"public {obj.Name}({nameof(ModKey)} modKey)");
            using (new DepthWrapper(fg))
            {
                fg.AppendLine(": base(modKey)");
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine("this.ModHeader.Stats.NextObjectID = GetDefaultInitialNextObjectID();");
                await obj.GenerateInitializer(fg);
            }

            using (var args = new FunctionWrapper(fg,
                "public void AddRecords"))
            {
                args.Add($"{obj.Name} rhsMod");
                args.Add($"GroupMask? mask = null");
            }
            using (new BraceWrapper(fg))
            {
                foreach (var field in obj.IterateFields())
                {
                    if (!(field is LoquiType loqui)) continue;
                    if (loqui.TargetObjectGeneration.GetObjectType() != ObjectType.Group) continue;
                    fg.AppendLine($"if (mask?.{field.Name} ?? true)");
                    using (new BraceWrapper(fg))
                    {
                        if (loqui.TargetObjectGeneration.Name == "Group")
                        {
                            fg.AppendLine($"this.{field.Name}.RecordCache.Set(rhsMod.{field.Name}.RecordCache.Items);");
                        }
                        else
                        {
                            fg.AppendLine($"if (rhsMod.{field.Name}.Records.Count > 0)");
                            using (new BraceWrapper(fg))
                            {
                                fg.AppendLine("throw new NotImplementedException(\"Cell additions need implementing\");");
                            }
                        }
                    }
                }
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                $"public Dictionary<FormKey, {nameof(IMajorRecordCommon)}> CopyInDuplicate"))
            {
                args.Add($"{obj.Name} rhs");
                args.Add($"GroupMask? mask = null");
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"var duppedRecords = new List<({nameof(IMajorRecordCommon)} Record, FormKey OriginalFormKey)>();");
                foreach (var field in obj.IterateFields())
                {
                    if (!(field is LoquiType loqui)) continue;
                    if (loqui.TargetObjectGeneration.GetObjectType() != ObjectType.Group) continue;
                    var dictGroup = loqui.TargetObjectGeneration.Name == "Group";
                    fg.AppendLine($"if (mask?.{field.Name} ?? true)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"this.{field.Name}.{(dictGroup ? "RecordCache" : "Records")}.{(dictGroup ? "Set" : "AddRange")}(");
                        using (new DepthWrapper(fg))
                        {
                            fg.AppendLine($"rhs.{field.Name}.Records");
                            using (new DepthWrapper(fg))
                            {
                                fg.AppendLine($".Select(i => i.Duplicate(this.GetNextFormKey, duppedRecords))");
                                fg.AppendLine($".Cast<{loqui.GetGroupTarget().Name}>());");
                            }
                        }
                    }
                }
                fg.AppendLine($"var router = new Dictionary<FormKey, {nameof(IMajorRecordCommon)}>();");
                fg.AppendLine($"router.Set(duppedRecords.Select(dup => new KeyValuePair<FormKey, {nameof(IMajorRecordCommon)}>(dup.OriginalFormKey, dup.Record)));");
                fg.AppendLine($"var mapping = new Dictionary<FormKey, FormKey>();");
                fg.AppendLine($"var package = this.CreateLinkCache();");
                fg.AppendLine("foreach (var rec in router.Values)");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine($"rec.RemapLinks(mapping);");
                }
                fg.AppendLine($"return router;");
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                "public override void SyncRecordCount"))
            {
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine("this.ModHeader.Stats.NumRecords = GetRecordCount();");
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                "public int GetRecordCount"))
            {
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine("int count = this.EnumerateMajorRecords().Count();");
                foreach (var field in obj.IterateFields())
                {
                    if (!(field is LoquiType loqui)) continue;
                    if (loqui.TargetObjectGeneration.GetObjectType() != ObjectType.Group) continue;
                    if (loqui.TargetObjectGeneration.Name == "ListGroup")
                    {
                        fg.AppendLine($"count += {field.Name}.Records.Count > 0 ? 1 : 0;");
                    }
                    else
                    {
                        fg.AppendLine($"count += {field.Name}.RecordCache.Count > 0 ? 1 : 0;");
                    }
                }
                fg.AppendLine("GetCustomRecordCount((customCount) => count += customCount);");
                fg.AppendLine("return count;");
            }
            fg.AppendLine();

            fg.AppendLine("partial void GetCustomRecordCount(Action<int> setter);");
            fg.AppendLine();

            await base.GenerateInClass(obj, fg);
        }

        public override async Task GenerateInCtor(ObjectGeneration obj, FileGeneration fg)
        {
            if (obj.GetObjectType() != ObjectType.Mod) return;
            await base.GenerateInCtor(obj, fg);
        }

        public override async Task GenerateInVoid(ObjectGeneration obj, FileGeneration fg)
        {
            if (obj.GetObjectType() != ObjectType.Mod) return;
            using (new NamespaceWrapper(fg, obj.Namespace))
            {
                fg.AppendLine("public class GroupMask");
                using (new BraceWrapper(fg))
                {
                    foreach (var field in obj.IterateFields())
                    {
                        if (!(field is LoquiType loqui)) continue;
                        if (loqui.TargetObjectGeneration == null) continue;
                        if (loqui.TargetObjectGeneration.GetObjectType() != ObjectType.Group) continue;
                        fg.AppendLine($"public bool {loqui.Name};");
                    }

                    fg.AppendLine("public GroupMask()");
                    using (new BraceWrapper(fg))
                    {
                    }

                    fg.AppendLine("public GroupMask(bool defaultValue)");
                    using (new BraceWrapper(fg))
                    {
                        foreach (var field in obj.IterateFields())
                        {
                            if (!(field is LoquiType loqui)) continue;
                            if (loqui.TargetObjectGeneration == null) continue;
                            if (loqui.TargetObjectGeneration.GetObjectType() != ObjectType.Group) continue;
                            fg.AppendLine($"{loqui.Name} = defaultValue;");
                        }
                    }
                }
                fg.AppendLine();

                fg.AppendLine($"public interface I{obj.Name}DisposableGetter : {obj.Interface(getter: true, internalInterface: true)}, IModDisposeGetter");
                using (new BraceWrapper(fg))
                {
                }
            }
        }

        public override async Task GenerateInCommonMixin(ObjectGeneration obj, FileGeneration fg)
        {
            await base.GenerateInCommonMixin(obj, fg);

            if (obj.GetObjectType() != ObjectType.Mod) return;
            using (var args = new FunctionWrapper(fg,
                "public static IReadOnlyCache<T, FormKey> GetGroupGetter<T>"))
            {
                args.Wheres.Add($"where T : {nameof(IMajorRecordCommonGetter)}");
                args.Add($"this {obj.Interface(getter: true)} obj");
            }
            using (new BraceWrapper(fg))
            {
                using (var args = new ArgsWrapper(fg,
                    $"return (IReadOnlyCache<T, FormKey>){obj.CommonClassInstance("obj", LoquiInterfaceType.IGetter, CommonGenerics.Class, MaskType.Normal)}.GetGroup<T>"))
                {
                    args.AddPassArg("obj");
                }
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                "public static ICache<T, FormKey> GetGroup<T>"))
            {
                args.Wheres.Add($"where T : {nameof(IMajorRecordCommon)}");
                args.Add($"this {obj.Interface(getter: false)} obj");
            }
            using (new BraceWrapper(fg))
            {
                using (var args = new ArgsWrapper(fg,
                    $"return (ICache<T, FormKey>){obj.CommonClassInstance("obj", LoquiInterfaceType.IGetter, CommonGenerics.Class, MaskType.Normal)}.GetGroup<T>"))
                {
                    args.AddPassArg("obj");
                }
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                $"public static void WriteToBinaryParallel"))
            {
                args.Add($"this {obj.Interface(getter: true, internalInterface: false)} item");
                args.Add($"Stream stream");
                args.Add($"{nameof(BinaryWriteParameters)}? param = null");
            }
            using (new BraceWrapper(fg))
            {
                using (var args = new ArgsWrapper(fg,
                    $"{obj.CommonClass(LoquiInterfaceType.IGetter, CommonGenerics.Class, MaskType.Normal)}.WriteParallel"))
                {
                    args.AddPassArg("item");
                    args.AddPassArg("stream");
                    args.Add($"param: param ?? {nameof(BinaryWriteParameters)}.{nameof(BinaryWriteParameters.Default)}");
                    args.Add("modKey: item.ModKey");
                }
            }
            fg.AppendLine();

            using (var args = new FunctionWrapper(fg,
                $"public static void WriteToBinaryParallel"))
            {
                args.Add($"this {obj.Interface(getter: true, internalInterface: false)} item");
                args.Add($"string path");
                args.Add($"{nameof(BinaryWriteParameters)}? param = null");
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"param ??= {nameof(BinaryWriteParameters)}.{nameof(BinaryWriteParameters.Default)};");
                using (var args = new ArgsWrapper(fg,
                    $"var modKey = param.{nameof(BinaryWriteParameters.RunMasterMatch)}"))
                {
                    args.Add("mod: item");
                    args.AddPassArg("path");
                }
                if (obj.GetObjectData().UsesStringFiles)
                {
                    fg.AppendLine("bool disposeStrings = param.StringsWriter == null;");
                    fg.AppendLine("param.StringsWriter ??= EnumExt.HasFlag((int)item.ModHeader.Flags, Mutagen.Bethesda.Internals.Constants.LocalizedFlag) ? new StringsWriter(modKey, Path.Combine(Path.GetDirectoryName(path), \"Strings\")) : null;");
                }
                fg.AppendLine("using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))");
                using (new BraceWrapper(fg))
                {
                    using (var args = new ArgsWrapper(fg,
                        $"{obj.CommonClass(LoquiInterfaceType.IGetter, CommonGenerics.Class, MaskType.Normal)}.WriteParallel"))
                    {
                        args.AddPassArg("item");
                        args.AddPassArg("stream");
                        args.Add($"param: param");
                        args.AddPassArg("modKey");
                    }
                }
                if (obj.GetObjectData().UsesStringFiles)
                {
                    fg.AppendLine("if (disposeStrings)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine("param.StringsWriter?.Dispose();");
                    }
                }
            }
            fg.AppendLine();
        }

        public override async Task GenerateInCommon(ObjectGeneration obj, FileGeneration fg, MaskTypeSet maskTypes)
        {
            await base.GenerateInCommon(obj, fg, maskTypes);
            if (obj.GetObjectType() != ObjectType.Mod) return;
            if (!maskTypes.Applicable(LoquiInterfaceType.IGetter, CommonGenerics.Class, MaskType.Normal)) return;

            GenerateGetGroup(obj, fg);
            GenerateWriteParallel(obj, fg);
        }

        private void GenerateGetGroup(ObjectGeneration obj, FileGeneration fg)
        {
            using (var args = new FunctionWrapper(fg,
                "public object GetGroup<TMajor>"))
            {
                args.Wheres.Add($"where TMajor : {nameof(IMajorRecordCommonGetter)}");
                args.Add($"{obj.Interface(getter: true)} obj");
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine("switch (typeof(TMajor).Name)");
                using (new BraceWrapper(fg))
                {
                    foreach (var field in obj.IterateFields())
                    {
                        if (!(field is LoquiType loqui)) continue;
                        if (loqui.TargetObjectGeneration?.GetObjectData().ObjectType != ObjectType.Group) continue;
                        if (!loqui.TryGetSpecificationAsObject("T", out var subObj))
                        {
                            throw new ArgumentException();
                        }
                        fg.AppendLine($"case \"{subObj.Name}\":");
                        fg.AppendLine($"case \"{subObj.Interface(getter: true)}\":");
                        fg.AppendLine($"case \"{subObj.Interface(getter: false)}\":");
                        if (subObj.HasInternalGetInterface)
                        {
                            fg.AppendLine($"case \"{subObj.Interface(getter: true, internalInterface: true)}\":");
                        }
                        if (subObj.HasInternalSetInterface)
                        {
                            fg.AppendLine($"case \"{subObj.Interface(getter: false, internalInterface: true)}\":");
                        }
                        using (new DepthWrapper(fg))
                        {
                            if (loqui.TargetObjectGeneration.Name == "ListGroup")
                            {
                                fg.AppendLine($"return obj.{field.Name}.Records;");
                            }
                            else
                            {
                                fg.AppendLine($"return obj.{field.Name}.RecordCache;");
                            }
                        }
                    }
                    fg.AppendLine("default:");
                    using (new DepthWrapper(fg))
                    {
                        fg.AppendLine("throw new ArgumentException($\"Unknown major record type: {typeof(TMajor)}\");");
                    }
                }
            }
            fg.AppendLine();
        }

        private void GenerateWriteParallel(ObjectGeneration obj, FileGeneration fg)
        {
            LoquiType groupInstance = null;
            LoquiType listGroupInstance = null;
            var objData = obj.GetObjectData();
            fg.AppendLine("const int CutCount = 100;");
            using (var args = new FunctionWrapper(fg,
                "public static void WriteParallel"))
            {
                args.Add($"{obj.Interface(getter: true, internalInterface: false)} item");
                args.Add($"Stream stream");
                args.Add($"{nameof(BinaryWriteParameters)} param");
                args.Add($"ModKey modKey");
            }
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"var masterRefs = UtilityTranslation.ConstructWriteMasters(item, param);");
                fg.AppendLine($"var bundle = new {nameof(WritingBundle)}({nameof(GameConstants)}.{obj.GetObjectData().GameMode});");
                fg.AppendLine($"bundle.{nameof(WritingBundle.MasterReferences)} = masterRefs;");
                using (var args = new ArgsWrapper(fg,
                    $"{obj.ObjectName}BinaryWriteTranslation.WriteModHeader"))
                {
                    args.Add("item");
                    args.Add($"new MutagenWriter(stream, bundle)");
                    args.Add("modKey");
                }

                int groupCount = obj.IterateFields()
                    .Select(f => f as LoquiType)
                    .Where(l => l != null)
                    .Where(l => l.TargetObjectGeneration?.GetObjectData().ObjectType == ObjectType.Group)
                    .Count();

                fg.AppendLine($"Stream[] outputStreams = new Stream[{groupCount}];");
                fg.AppendLine($"List<Action> toDo = new List<Action>();");
                int i = 0;
                foreach (var field in obj.IterateFields())
                {
                    if (!(field is LoquiType loqui)) continue;
                    if (loqui.TargetObjectGeneration?.GetObjectData().ObjectType != ObjectType.Group) continue;
                    if (loqui.TargetObjectGeneration.Name == "ListGroup")
                    {
                        listGroupInstance = loqui;
                    }
                    else
                    {
                        groupInstance = loqui;
                    }
                    if (loqui.GetGroupTarget().GetObjectData().CustomBinaryEnd == CustomEnd.Off
                        && loqui.TargetObjectGeneration.Name != "ListGroup")
                    {
                        fg.AppendLine($"toDo.Add(() => WriteGroupParallel(item.{field.Name}, masterRefs, {i}, outputStreams{(objData.UsesStringFiles ? ", param.StringsWriter" : null)}));");
                    }
                    else
                    {
                        fg.AppendLine($"toDo.Add(() => Write{field.Name}Parallel(item.{field.Name}, masterRefs, {i}, outputStreams));");
                    }
                    i++;
                }
                fg.AppendLine("Parallel.Invoke(toDo.ToArray());");
                using (var args = new ArgsWrapper(fg,
                    $"{nameof(UtilityTranslation)}.{nameof(UtilityTranslation.CompileStreamsInto)}"))
                {
                    args.Add("outputStreams.NotNull()");
                    args.Add("stream");
                }
            }
            fg.AppendLine();

            if (groupInstance != null)
            {
                using (var args = new FunctionWrapper(fg,
                    $"public static void WriteGroupParallel<T>"))
                {
                    args.Add("IGroupGetter<T> group");
                    args.Add($"{nameof(MasterReferenceReader)} masters");
                    args.Add("int targetIndex");
                    args.Add("Stream[] streamDepositArray");
                    if (objData.UsesStringFiles)
                    {
                        args.Add($"{nameof(StringsWriter)}? stringsWriter");
                    }
                    args.Wheres.AddRange(groupInstance.TargetObjectGeneration.GenerateWhereClauses(LoquiInterfaceType.IGetter, groupInstance.TargetObjectGeneration.Generics));
                }
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine("if (group.RecordCache.Count == 0) return;");
                    fg.AppendLine($"var cuts = group.Records.Cut(CutCount).ToArray();");
                    fg.AppendLine($"Stream[] subStreams = new Stream[cuts.Length + 1];");
                    fg.AppendLine($"byte[] groupBytes = new byte[{nameof(GameConstants)}.{obj.GetObjectData().GameMode}.GroupConstants.HeaderLength];");
                    fg.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(groupBytes.AsSpan(), RecordTypes.GRUP.TypeInt);");
                    fg.AppendLine($"var groupByteStream = new MemoryStream(groupBytes);");
                    fg.AppendLine($"var bundle = new {nameof(WritingBundle)}({nameof(GameConstants)}.{obj.GetObjectData().GameMode})");
                    using (var prop = new PropertyCtorWrapper(fg))
                    {
                        prop.Add($"{nameof(WritingBundle.MasterReferences)} = masters");
                        if (objData.UsesStringFiles)
                        {
                            prop.Add($"{nameof(WritingBundle.StringsWriter)} = stringsWriter");
                        }
                    }
                    fg.AppendLine($"using (var stream = new MutagenWriter(groupByteStream, {nameof(GameConstants)}.{obj.GetObjectData().GameMode}, dispose: false))");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"stream.Position += 8;");
                        fg.AppendLine($"GroupBinaryWriteTranslation.WriteEmbedded<T>(group, stream);");
                    }
                    fg.AppendLine($"subStreams[0] = groupByteStream;");
                    fg.AppendLine($"Parallel.ForEach(cuts, (cutItems, state, counter) =>");
                    using (new BraceWrapper(fg) { AppendSemicolon = true, AppendParenthesis = true })
                    {
                        fg.AppendLine($"{nameof(MemoryTributary)} trib = new {nameof(MemoryTributary)}();");
                        fg.AppendLine($"using (var stream = new MutagenWriter(trib, bundle, dispose: false))");
                        using (new BraceWrapper(fg))
                        {
                            fg.AppendLine($"foreach (var item in cutItems)");
                            using (new BraceWrapper(fg))
                            {
                                fg.AppendLine($"item.WriteToBinary(stream);");
                            }
                        }
                        fg.AppendLine($"subStreams[(int)counter + 1] = trib;");
                    }
                    fg.AppendLine($"UtilityTranslation.CompileSetGroupLength(subStreams, groupBytes);");
                    fg.AppendLine($"streamDepositArray[targetIndex] = new CompositeReadStream(subStreams, resetPositions: true);");
                }
                fg.AppendLine();
            }
        }
    }
}
