using Loqui;
using Loqui.Generation;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Noggog;
using Mutagen.Bethesda.Internals;
using Mutagen.Bethesda.Binary;

namespace Mutagen.Bethesda.Generation
{
    public class GenderedType : WrapperType
    {
        public override bool HasDefault => false;
        public override bool CopyNeedsTryCatch => true;
        public override bool IsEnumerable => true;
        public override bool IsClass => true;
        public bool ItemHasBeenSet
        {
            get
            {
                if (MaleMarker.HasValue) return true;
                if (this.SubTypeGeneration is LoquiType loqui)
                {
                    return loqui.GetFieldData().HasTrigger;
                }
                return false;
            }
        }
        public override bool CanBeNullable(bool getter) => true;
        public RecordTypeConverter FemaleConversions;
        public RecordTypeConverter MaleConversions;

        public RecordType? MaleMarker;
        public RecordType? FemaleMarker;
        public bool MarkerPerGender;

        public override void GenerateClear(FileGeneration fg, Accessor accessorPrefix)
        {
            if (this.HasBeenSet)
            {
                fg.AppendLine($"{accessorPrefix.DirectAccess} = null;");
            }
            else
            {
                SubTypeGeneration.GenerateClear(fg, $"{accessorPrefix.DirectAccess}.Male");
                SubTypeGeneration.GenerateClear(fg, $"{accessorPrefix.DirectAccess}.Female");
            }
        }

        public override void GenerateForClass(FileGeneration fg)
        {
            fg.AppendLine($"public GenderedItem<{SubTypeGeneration.TypeName(getter: false, needsCovariance: true)}{SubTypeGeneration.NullChar}>{(this.HasBeenSet ? "?" : null)} {this.Name} {{ get; set; }}{(this.HasBeenSet ? null : $" = new GenderedItem<{SubTypeGeneration.TypeName(getter: false, needsCovariance: true)}{(this.ItemHasBeenSet ? "?" : null)}>({this.SubTypeGeneration.GetDefault(getter: false)}, {this.SubTypeGeneration.GetDefault(getter: false)});")}");
            fg.AppendLine($"IGenderedItemGetter<{SubTypeGeneration.TypeName(getter: true, needsCovariance: true)}{SubTypeGeneration.NullChar}>{(this.HasBeenSet ? "?" : null)} {this.ObjectGen.Interface(getter: true, internalInterface: true)}.{this.Name} => this.{this.Name};");
        }

        public override string GenerateACopy(string rhsAccessor)
        {
            throw new NotImplementedException();
        }

        public override void GenerateForCopy(FileGeneration fg, Accessor accessor, Accessor rhs, Accessor copyMaskAccessor, bool protectedMembers, bool deepCopy)
        {
            if (!deepCopy)
            {
                throw new NotImplementedException();
            }
            if (this.HasBeenSet)
            {
                fg.AppendLine($"if (!{rhs}.TryGet(out var rhs{this.Name}item))");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine($"{accessor} = null;");
                }
                fg.AppendLine("else");
                rhs = $"rhs{this.Name}item";
            }
            using (new BraceWrapper(fg, doIt: this.HasBeenSet))
            {
                using (var args = new ArgsWrapper(fg,
                    $"{accessor} = new GenderedItem<{this.SubTypeGeneration.TypeName(getter: false, needsCovariance: true)}{this.SubTypeGeneration.NullChar}>"))
                {
                    if (this.isLoquiSingle)
                    {
                        LoquiType loqui = this.SubTypeGeneration as LoquiType;
                        args.Add(subFg =>
                        {
                            loqui.GenerateTypicalMakeCopy(
                                subFg,
                                retAccessor: $"male: ",
                                rhsAccessor: $"{rhs}.Male{this.SubTypeGeneration.NullChar}",
                                copyMaskAccessor: $"{copyMaskAccessor}.Male",
                                deepCopy: deepCopy,
                                doTranslationMask: false);
                        });
                        args.Add(subFg =>
                        {
                            loqui.GenerateTypicalMakeCopy(
                                subFg,
                                retAccessor: $"female: ",
                                rhsAccessor: $"{rhs}.Female{this.SubTypeGeneration.NullChar}",
                                copyMaskAccessor: $"{copyMaskAccessor}.Female",
                                deepCopy: deepCopy,
                                doTranslationMask: false);
                        });
                    }
                    else
                    {
                        args.Add($"male: {this.SubTypeGeneration.GetDuplicate($"{rhs}.Male")}");
                        args.Add($"female: {this.SubTypeGeneration.GetDuplicate($"{rhs}.Female")}");
                    }
                }
            }
        }

        public override void GenerateForEquals(FileGeneration fg, Accessor accessor, Accessor rhsAccessor)
        {
            fg.AppendLine($"if (!Equals({accessor.DirectAccess}, {rhsAccessor.DirectAccess})) return false;");
        }

        public override void GenerateForEqualsMask(FileGeneration fg, Accessor accessor, Accessor rhsAccessor, string retAccessor)
        {
            LoquiType loqui = this.SubTypeGeneration as LoquiType;

            if (this.HasBeenSet || loqui != null)
            {
                using (var args = new ArgsWrapper(fg,
                    $"ret.{this.Name} = {nameof(GenderedItem)}.{nameof(GenderedItem.EqualityMaskHelper)}"))
                {
                    args.Add($"lhs: {accessor.DirectAccess}");
                    args.Add($"rhs: {rhsAccessor.DirectAccess}");
                    if (loqui == null)
                    {
                        args.Add($"maskGetter: (l, r, i) => EqualityComparer<{this.SubTypeGeneration.TypeName(getter: true, needsCovariance: true)}{this.SubTypeGeneration.NullChar}>.Default.Equals(l, r)");
                    }
                    else
                    {
                        if (this.ItemHasBeenSet)
                        {
                            args.Add($"maskGetter: (l, r, i) => EqualsMaskHelper.EqualsHelper(l, r, (loqLhs, loqRhs, incl) => loqLhs.GetEqualsMask(loqRhs, incl), i)");
                        }
                        else
                        {
                            args.Add("maskGetter: (l, r, i) => l.GetEqualsMask(r, i)");
                        }
                    }
                    args.AddPassArg("include");
                }
            }
            else
            {
                using (var args = new ArgsWrapper(fg,
                    $"ret.{this.Name} = new GenderedItem<bool>"))
                {
                    args.Add($"male: {this.SubTypeGeneration.GenerateEqualsSnippet($"{accessor}.Male", $"{rhsAccessor}.Male")}");
                    args.Add($"female: {this.SubTypeGeneration.GenerateEqualsSnippet($"{accessor}.Female", $"{rhsAccessor}.Female")}");
                }
            }
        }

        public override void GenerateForHasBeenSetCheck(FileGeneration fg, Accessor accessor, string checkMaskAccessor)
        {
            if (this.HasBeenSet)
            {
                fg.AppendLine($"if ({checkMaskAccessor}?.Overall ?? false) return false;");
            }
            else if (this.ItemHasBeenSet)
            {
                fg.AppendLine($"throw new NotImplementedException();");
            }
        }

        public override void GenerateForHasBeenSetMaskGetter(FileGeneration fg, Accessor accessor, string retAccessor)
        {
            bool isLoqui = this.SubTypeGeneration is LoquiType;
            if (isLoqui)
            {
                using (var args = new ArgsWrapper(fg,
                    $"{retAccessor} = GenderedItem.HasBeenSet{(this.ItemHasBeenSet ? "Mask" : null)}Helper"))
                {
                    args.Add($"{accessor}");
                    args.Add($"(i) => i{this.SubTypeGeneration.NullChar}.GetHasBeenSetMask()");
                }
            }
            else if (this.HasBeenSet)
            {
                fg.AppendLine($"{retAccessor} = {accessor} == null ? null : new MaskItem<bool, GenderedItem<bool>?>(true, default);");
            }
            else if (this.ItemHasBeenSet)
            {
                fg.AppendLine($"{retAccessor} = new GenderedItem<bool>({accessor}.Male != null, {accessor}.Female != null);");
            }
            else
            {
                fg.AppendLine($"{retAccessor} = new GenderedItem<bool>(true, true);");
            }
        }

        public override void GenerateForHash(FileGeneration fg, Accessor accessor, string hashResultAccessor)
        {
            if (this.HasBeenSet)
            {
                fg.AppendLine($"if ({accessor}.TryGet(out var {this.Name}item))");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine($"{hashResultAccessor}.Add(HashCode.Combine({this.Name}item.Male, {this.Name}item.Female));");
                }
            }
            else
            {
                fg.AppendLine($"{hashResultAccessor}.Add(HashCode.Combine({accessor}.Male, {accessor}.Female));");
            }
        }

        public override void GenerateForInterface(FileGeneration fg, bool getter, bool internalInterface)
        {
            if (getter)
            {
                fg.AppendLine($"IGenderedItemGetter<{SubTypeGeneration.TypeName(getter: true, needsCovariance: true)}{SubTypeGeneration.NullChar}>{(this.HasBeenSet ? "?" : null)} {this.Name} {{ get; }}");
            }
            else
            {
                fg.AppendLine($"new GenderedItem<{SubTypeGeneration.TypeName(getter: false, needsCovariance: true)}{SubTypeGeneration.NullChar}>{(this.HasBeenSet ? "?" : null)} {this.Name} {{ get; set; }}");
            }
        }

        public override void GenerateGetNth(FileGeneration fg, Accessor identifier)
        {
            throw new NotImplementedException();
        }

        public override void GenerateSetNth(FileGeneration fg, Accessor accessor, Accessor rhs, bool internalUse)
        {
            throw new NotImplementedException();
        }

        public override void GenerateSetNthHasBeenSet(FileGeneration fg, Accessor identifier, string onIdentifier)
        {
            throw new NotImplementedException();
        }

        public override void GenerateToString(FileGeneration fg, string name, Accessor accessor, string fgAccessor)
        {
            fg.AppendLine($"{accessor}{(this.HasBeenSet ? "?" : null)}.ToString({fgAccessor}, \"{name}\");");
        }

        public override void GenerateUnsetNth(FileGeneration fg, Accessor identifier)
        {
            throw new NotImplementedException();
        }

        public override string SkipCheck(Accessor copyMaskAccessor, bool deepCopy)
        {
            throw new NotImplementedException();
        }

        public override string TypeName(bool getter, bool needsCovariance = false)
        {
            return $"{(getter ? "IGenderedItemGetter" : "GenderedItem")}<{SubTypeGeneration.TypeName(getter, needsCovariance: true)}{this.SubTypeGeneration.NullChar}>";
        }

        public override string GetDuplicate(Accessor accessor)
        {
            throw new NotImplementedException();
        }

        public override async Task Load(XElement node, bool requireName = true)
        {
            await base.Load(node, requireName);

            if (node.TryGetAttribute<string>("maleMarker", out var maleMarker))
            {
                MaleMarker = new RecordType(maleMarker);
            }

            if (node.TryGetAttribute<string>("femaleMarker", out var femaleMarker))
            {
                FemaleMarker = new RecordType(femaleMarker);
            }

            if (MaleMarker.HasValue != FemaleMarker.HasValue)
            {
                throw new ArgumentException("Both submarkers must be set at once.");
            }

            this.MarkerPerGender = node.GetAttribute<bool>("markerPerGender");

            if (MaleMarker.HasValue)
            {
                this.SubTypeGeneration.HasBeenSetProperty.OnNext((true, true));
            }

            FemaleConversions = RecordTypeConverterModule.GetConverter(node.Element(XName.Get("FemaleTypeOverrides", LoquiGenerator.Namespace)));
            MaleConversions = RecordTypeConverterModule.GetConverter(node.Element(XName.Get("MaleTypeOverrides", LoquiGenerator.Namespace)));
        }

        public override void GenerateInRegistration(FileGeneration fg)
        {
            base.GenerateInRegistration(fg);
            RecordTypeConverterModule.GenerateConverterMember(fg, this.ObjectGen, this.FemaleConversions, $"{this.Name}Female");
            RecordTypeConverterModule.GenerateConverterMember(fg, this.ObjectGen, this.MaleConversions, $"{this.Name}Male");
        }
    }
}
