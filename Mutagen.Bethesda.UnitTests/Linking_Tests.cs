using Loqui;
using Loqui.Internal;
using Mutagen.Bethesda.Binary;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Mutagen.Bethesda.UnitTests
{
    public class LinkingInit : IDisposable
    {
        public LinkingInit()
        {
            WarmupAll.Init();
        }

        public void Dispose()
        {
        }
    }

    public abstract class Linking_Abstract_Tests : IClassFixture<LinkingInit>
    {
        public static FormKey UnusedFormKey = new FormKey(Utility.ModKey, 123456);
        public static string PathToTestFile = "../../../test.esp";
        public static string PathToOverrideFile = "../../../override.esp";
        public static FormKey TestFileFormKey = new FormKey(ModKey.Factory("test.esp"), 0xD62);
        public static FormKey TestFileFormKey2 = new FormKey(ModKey.Factory("test.esp"), 0xD63);

        public abstract IDisposable ConvertMod(SkyrimMod mod, out ISkyrimModGetter getter);
        public abstract bool ReadOnly { get; }

        #region Direct Mod
        [Fact]
        public void Direct_Empty()
        {
            using var disp = ConvertMod(new SkyrimMod(Utility.ModKey), out var mod);
            var package = new DirectModLinkCache<ISkyrimModGetter>(mod);

            // Test query fails
            Assert.False(package.TryLookup(UnusedFormKey, out var _));
            Assert.False(package.TryLookup(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IObjectEffectGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IObjectEffectGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IObjectEffect>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IObjectEffect>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ObjectEffect>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ObjectEffect>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IEffectRecord>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IEffectRecord>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IEffectRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IEffectRecordGetter>(FormKey.Null, out var _));
        }

        [Fact]
        public void Direct_NoMatch()
        {
            var prototype = new SkyrimMod(Utility.ModKey);
            prototype.ObjectEffects.AddNew();
            using var disp = ConvertMod(prototype, out var mod);
            var package = new DirectModLinkCache<ISkyrimModGetter>(mod);

            // Test query fails
            Assert.False(package.TryLookup(UnusedFormKey, out var _));
            Assert.False(package.TryLookup(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IObjectEffectGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IObjectEffectGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IObjectEffect>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IObjectEffect>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ObjectEffect>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ObjectEffect>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IEffectRecord>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IEffectRecord>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IEffectRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IEffectRecordGetter>(FormKey.Null, out var _));
        }

        [Fact]
        public void Direct_Typical()
        {
            var prototype = new SkyrimMod(Utility.ModKey);
            var objEffect1 = prototype.ObjectEffects.AddNew();
            var objEffect2 = prototype.ObjectEffects.AddNew();
            using var disp = ConvertMod(prototype, out var mod);
            var package = new DirectModLinkCache<ISkyrimModGetter>(mod);

            // Do linked interfaces first, as this tests a specific edge case
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }

            {
                Assert.True(package.TryLookup(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            if (ReadOnly)
            {
                Assert.False(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var _));
            }
            else
            {
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
            }
        }

        [Fact]
        public void Direct_ReadOnlyMechanics()
        {
            var wrapper = Oblivion.OblivionMod.CreateFromBinaryOverlay(PathToTestFile);
            var package = wrapper.CreateLinkCache();
            {
                Assert.True(package.TryLookup<Oblivion.INpcGetter>(TestFileFormKey, out var rec));
            }
            {
                Assert.False(package.TryLookup<Oblivion.INpc>(TestFileFormKey, out var rec));
            }
            {
                Assert.False(package.TryLookup<Oblivion.Npc>(TestFileFormKey, out var rec));
            }
        }
        #endregion

        #region LoadOrder
        [Fact]
        public void LoadOrder_Empty()
        {
            var package = new LoadOrderLinkCache<SkyrimMod>(new LoadOrder<SkyrimMod>());

            // Test query fails
            Assert.False(package.TryLookup(UnusedFormKey, out var _));
            Assert.False(package.TryLookup(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(FormKey.Null, out var _));
        }

        [Fact]
        public void LoadOrder_NoMatch()
        {
            var prototype = new SkyrimMod(Utility.ModKey);
            prototype.Npcs.AddNew();
            using var disp = ConvertMod(prototype, out var mod);
            var loadOrder = new LoadOrder<ISkyrimModGetter>();
            loadOrder.Add(mod);
            var package = new LoadOrderLinkCache<ISkyrimModGetter>(loadOrder);

            // Test query fails
            Assert.False(package.TryLookup(UnusedFormKey, out var _));
            Assert.False(package.TryLookup(FormKey.Null, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<IMajorRecordCommonGetter>(FormKey.Null, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(UnusedFormKey, out var _));
            Assert.False(package.TryLookup<ISkyrimMajorRecordGetter>(FormKey.Null, out var _));
        }

        [Fact]
        public void LoadOrder_Single()
        {
            var prototype = new SkyrimMod(Utility.ModKey);
            var objEffect1 = prototype.ObjectEffects.AddNew();
            var objEffect2 = prototype.ObjectEffects.AddNew();
            using var disp = ConvertMod(prototype, out var mod);
            var loadOrder = new LoadOrder<ISkyrimModGetter>();
            loadOrder.Add(mod);
            var package = new LoadOrderLinkCache<ISkyrimModGetter>(loadOrder);

            // Test query successes

            // Do linked interfaces first, as this tests a specific edge case
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }

            {
                Assert.True(package.TryLookup(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            if (ReadOnly)
            {
                Assert.False(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var _));
            }
            else
            {
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
            }
        }

        [Fact]
        public void LoadOrder_OneInEach()
        {
            var prototype1 = new SkyrimMod(Utility.ModKey);
            var prototype2 = new SkyrimMod(new ModKey("Dummy2", true));
            var objEffect1 = prototype1.ObjectEffects.AddNew();
            var objEffect2 = prototype2.ObjectEffects.AddNew();
            using var disp1 = ConvertMod(prototype1, out var mod1);
            using var disp2 = ConvertMod(prototype2, out var mod2);
            var loadOrder = new LoadOrder<ISkyrimModGetter>();
            loadOrder.Add(mod1);
            loadOrder.Add(mod2);
            var package = new LoadOrderLinkCache<ISkyrimModGetter>(loadOrder);

            // Test query successes

            // Do linked interfaces first, as this tests a specific edge case
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }

            {
                Assert.True(package.TryLookup(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect1.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect1.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(objEffect2.FormKey, out var rec));
                Assert.Equal(rec.FormKey, objEffect2.FormKey);
            }
            if (ReadOnly)
            {
                Assert.False(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var _));
            }
            else
            {
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<ObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IObjectEffect>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect1.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect1.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(objEffect2.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, objEffect2.FormKey);
                }
            }
        }

        [Fact]
        public void LoadOrder_Overridden()
        {
            var prototype1 = new SkyrimMod(Utility.ModKey);
            var prototype2 = new SkyrimMod(new ModKey("Dummy2", true));
            var unoverriddenRec = prototype1.ObjectEffects.AddNew();
            var overriddenRec = prototype1.ObjectEffects.AddNew();
            var topModRec = prototype2.ObjectEffects.AddNew();
            var overrideRec = (ObjectEffect)overriddenRec.DeepCopy();
            prototype2.ObjectEffects.RecordCache.Set(overrideRec);
            using var disp1 = ConvertMod(prototype1, out var mod1);
            using var disp2 = ConvertMod(prototype2, out var mod2);
            var loadOrder = new LoadOrder<ISkyrimModGetter>
            {
                mod1,
                mod2
            };
            var package = new LoadOrderLinkCache<ISkyrimModGetter>(loadOrder);

            // Test query successes

            // Do linked interfaces first, as this tests a specific edge case
            {
                Assert.True(package.TryLookup<IEffectRecordGetter>(overriddenRec.FormKey, out var rec));
                Assert.Equal(rec.FormKey, overrideRec.FormKey);
                Assert.True(package.TryLookup<IEffectRecordGetter>(unoverriddenRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                Assert.True(package.TryLookup<IEffectRecordGetter>(topModRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, topModRec.FormKey);
            }

            {
                Assert.True(package.TryLookup(overriddenRec.FormKey, out var rec));
                Assert.Equal(rec.FormKey, overrideRec.FormKey);
                Assert.True(package.TryLookup(unoverriddenRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                Assert.True(package.TryLookup(topModRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, topModRec.FormKey);
            }
            {
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(overriddenRec.FormKey, out var rec));
                Assert.Equal(rec.FormKey, overrideRec.FormKey);
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(unoverriddenRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                Assert.True(package.TryLookup<IMajorRecordCommonGetter>(topModRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, topModRec.FormKey);
            }
            {
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(overriddenRec.FormKey, out var rec));
                Assert.Equal(rec.FormKey, overrideRec.FormKey);
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(unoverriddenRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                Assert.True(package.TryLookup<ISkyrimMajorRecordGetter>(topModRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, topModRec.FormKey);
            }
            {
                Assert.True(package.TryLookup<IObjectEffectGetter>(overriddenRec.FormKey, out var rec));
                Assert.Equal(rec.FormKey, overrideRec.FormKey);
                Assert.True(package.TryLookup<IObjectEffectGetter>(unoverriddenRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                Assert.True(package.TryLookup<IObjectEffectGetter>(topModRec.FormKey, out rec));
                Assert.Equal(rec.FormKey, topModRec.FormKey);
            }
            if (ReadOnly)
            {
                Assert.False(package.TryLookup<IObjectEffect>(overriddenRec.FormKey, out var _));
                Assert.False(package.TryLookup<IObjectEffect>(unoverriddenRec.FormKey, out _));
                Assert.False(package.TryLookup<IObjectEffect>(topModRec.FormKey, out _));
                Assert.False(package.TryLookup<ObjectEffect>(overriddenRec.FormKey, out var _));
                Assert.False(package.TryLookup<ObjectEffect>(unoverriddenRec.FormKey, out _));
                Assert.False(package.TryLookup<ObjectEffect>(topModRec.FormKey, out _));
                Assert.False(package.TryLookup<IEffectRecord>(overriddenRec.FormKey, out var _));
                Assert.False(package.TryLookup<IEffectRecord>(unoverriddenRec.FormKey, out _));
                Assert.False(package.TryLookup<IEffectRecord>(topModRec.FormKey, out _));
            }
            else
            {
                {
                    Assert.True(package.TryLookup<IObjectEffect>(overriddenRec.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, overrideRec.FormKey);
                    Assert.True(package.TryLookup<IObjectEffect>(unoverriddenRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                    Assert.True(package.TryLookup<IObjectEffect>(topModRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, topModRec.FormKey);
                }
                {
                    Assert.True(package.TryLookup<ObjectEffect>(overriddenRec.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, overrideRec.FormKey);
                    Assert.True(package.TryLookup<ObjectEffect>(unoverriddenRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                    Assert.True(package.TryLookup<ObjectEffect>(topModRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, topModRec.FormKey);
                }
                {
                    Assert.True(package.TryLookup<IEffectRecord>(overriddenRec.FormKey, out var rec));
                    Assert.Equal(rec.FormKey, overrideRec.FormKey);
                    Assert.True(package.TryLookup<IEffectRecord>(unoverriddenRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, unoverriddenRec.FormKey);
                    Assert.True(package.TryLookup<IEffectRecord>(topModRec.FormKey, out rec));
                    Assert.Equal(rec.FormKey, topModRec.FormKey);
                }
            }
        }

        [Fact]
        public void LoadOrder_ReadOnlyMechanics()
        {
            var wrapper = Oblivion.OblivionMod.CreateFromBinaryOverlay(PathToTestFile);
            var overrideWrapper = Oblivion.OblivionMod.CreateFromBinaryOverlay(PathToOverrideFile);
            var loadOrder = new LoadOrder<Oblivion.IOblivionModGetter>();
            loadOrder.Add(wrapper);
            loadOrder.Add(overrideWrapper);
            var package = loadOrder.CreateLinkCache();
            {
                Assert.True(package.TryLookup<Oblivion.INpcGetter>(TestFileFormKey, out var rec));
                Assert.True(package.TryLookup<Oblivion.INpcGetter>(TestFileFormKey2, out rec));
                Assert.True(rec.Name.TryGet(out var name));
                Assert.Equal("A Name", name);
            }
            {
                Assert.False(package.TryLookup<Oblivion.INpc>(TestFileFormKey, out var rec));
                Assert.False(package.TryLookup<Oblivion.INpc>(TestFileFormKey2, out rec));
            }
            {
                Assert.False(package.TryLookup<Oblivion.Npc>(TestFileFormKey, out var rec));
                Assert.False(package.TryLookup<Oblivion.Npc>(TestFileFormKey2, out rec));
            }
        }
        #endregion

        #region Direct FormLink Resolves
        [Fact]
        public void FormLink_Direct_TryResolve_NoLink()
        {
            FormLink<INpc> formLink = new FormLink<INpc>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_Direct_TryResolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var npc = mod.Npcs.AddNew();
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<INpc> formLink = new FormLink<INpc>(npc.FormKey);
            Assert.True(formLink.TryResolve(package, out var linkedRec));
            Assert.Same(npc, linkedRec);
        }

        [Fact]
        public void FormLink_Direct_TryResolve_DeepRecord_NoLink()
        {
            FormLink<IPlacedNpc> formLink = new FormLink<IPlacedNpc>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_Direct_TryResolve_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IPlacedNpc> placedFormLink = new FormLink<IPlacedNpc>(placedNpc.FormKey);
            Assert.True(placedFormLink.TryResolve(package, out var linkedPlacedNpc));
            Assert.Same(placedNpc, linkedPlacedNpc);
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.True(cellFormLink.TryResolve(package, out var linkedCell));
            Assert.Same(cell, linkedCell);
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.True(worldspaceFormLink.TryResolve(package, out var linkedWorldspace));
            Assert.Same(worldspace, linkedWorldspace);
        }

        [Fact]
        public void FormLink_Direct_Resolve_NoLink()
        {
            FormLink<INpc> formLink = new FormLink<INpc>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var npc = mod.Npcs.AddNew();
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<INpc> formLink = new FormLink<INpc>(npc.FormKey);
            Assert.Same(npc, formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_DeepRecord_NoLink()
        {
            FormLink<IPlacedNpc> formLink = new FormLink<IPlacedNpc>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IPlacedNpc> placedFormLink = new FormLink<IPlacedNpc>(placedNpc.FormKey);
            Assert.Same(placedNpc, placedFormLink.Resolve(package));
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.Same(cell, cellFormLink.Resolve(package));
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.Same(worldspace, worldspaceFormLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_TryResolve_MarkerInterface()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var spell = mod.Spells.AddNew();
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(spell.FormKey);
            Assert.True(formLink.TryResolve(package, out var linkedRec));
            Assert.Same(spell, linkedRec);
        }

        [Fact]
        public void FormLink_Direct_TryResolve_MarkerInterface_NoLink()
        {
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_Direct_TryResolve_MarkerInterface_DeepRecord_NoLink()
        {
            FormLink<IPlaced> formLink = new FormLink<IPlaced>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_Direct_TryResolve_MarkerInterface_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IPlaced> placedFormLink = new FormLink<IPlaced>(placedNpc.FormKey);
            Assert.True(placedFormLink.TryResolve(package, out var linkedPlacedNpc));
            Assert.Same(placedNpc, linkedPlacedNpc);
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.True(cellFormLink.TryResolve(package, out var linkedCell));
            Assert.Same(cell, linkedCell);
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.True(worldspaceFormLink.TryResolve(package, out var linkedWorldspace));
            Assert.Same(worldspace, linkedWorldspace);
        }

        [Fact]
        public void FormLink_Direct_Resolve_MarkerInterface()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var spell = mod.Spells.AddNew();
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(spell.FormKey);
            Assert.Same(spell, formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_MarkerInterface_NoLink()
        {
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_MarkerInterface_DeepRecord_NoLink()
        {
            FormLink<IPlaced> formLink = new FormLink<IPlaced>(UnusedFormKey);
            var package = new DirectModLinkCache<SkyrimMod>(new SkyrimMod(Utility.ModKey));
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_Direct_Resolve_MarkerInterface_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            FormLink<IPlaced> placedFormLink = new FormLink<IPlaced>(placedNpc.FormKey);
            Assert.Same(placedNpc, placedFormLink.Resolve(package));
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.Same(cell, cellFormLink.Resolve(package));
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.Same(worldspace, worldspaceFormLink.Resolve(package));
        }
        #endregion

        #region Load Order FormLink Resolves
        LoadOrder<SkyrimMod> TypicalLoadOrder()
        {
            return new LoadOrder<SkyrimMod>()
            {
                new SkyrimMod(Utility.ModKey),
                new SkyrimMod(Utility.ModKey2),
            };
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_NoLink()
        {
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            FormLink<INpc> formLink = new FormLink<INpc>(UnusedFormKey);
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var npc = mod.Npcs.AddNew();
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<INpc> formLink = new FormLink<INpc>(npc.FormKey);
            Assert.True(formLink.TryResolve(package, out var linkedRec));
            Assert.Same(npc, linkedRec);
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_DeepRecord_NoLink()
        {
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            FormLink<IPlacedNpc> formLink = new FormLink<IPlacedNpc>(UnusedFormKey);
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IPlacedNpc> placedFormLink = new FormLink<IPlacedNpc>(placedNpc.FormKey);
            Assert.True(placedFormLink.TryResolve(package, out var linkedPlacedNpc));
            Assert.Same(placedNpc, linkedPlacedNpc);
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.True(cellFormLink.TryResolve(package, out var linkedCell));
            Assert.Same(cell, linkedCell);
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.True(worldspaceFormLink.TryResolve(package, out var linkedWorldspace));
            Assert.Same(worldspace, linkedWorldspace);
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_NoLink()
        {
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            FormLink<INpc> formLink = new FormLink<INpc>(UnusedFormKey);
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var npc = mod.Npcs.AddNew();
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<INpc> formLink = new FormLink<INpc>(npc.FormKey);
            Assert.Same(npc, formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_DeepRecord_NoLink()
        {
            FormLink<IPlacedNpc> formLink = new FormLink<IPlacedNpc>(UnusedFormKey);
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IPlacedNpc> placedFormLink = new FormLink<IPlacedNpc>(placedNpc.FormKey);
            Assert.Same(placedNpc, placedFormLink.Resolve(package));
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.Same(cell, cellFormLink.Resolve(package));
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.Same(worldspace, worldspaceFormLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_MarkerInterface()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var spell = mod.Spells.AddNew();
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(spell.FormKey);
            Assert.True(formLink.TryResolve(package, out var linkedRec));
            Assert.Same(spell, linkedRec);
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_MarkerInterface_NoLink()
        {
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(UnusedFormKey);
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_MarkerInterface_DeepRecord_NoLink()
        {
            FormLink<IPlaced> formLink = new FormLink<IPlaced>(UnusedFormKey);
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            Assert.False(formLink.TryResolve(package, out var _));
        }

        [Fact]
        public void FormLink_LoadOrder_TryResolve_MarkerInterface_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IPlaced> placedFormLink = new FormLink<IPlaced>(placedNpc.FormKey);
            Assert.True(placedFormLink.TryResolve(package, out var linkedPlacedNpc));
            Assert.Same(placedNpc, linkedPlacedNpc);
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.True(cellFormLink.TryResolve(package, out var linkedCell));
            Assert.Same(cell, linkedCell);
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.True(worldspaceFormLink.TryResolve(package, out var linkedWorldspace));
            Assert.Same(worldspace, linkedWorldspace);
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_MarkerInterface()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var spell = mod.Spells.AddNew();
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(spell.FormKey);
            Assert.Same(spell, formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_MarkerInterface_NoLink()
        {
            FormLink<IEffectRecord> formLink = new FormLink<IEffectRecord>(UnusedFormKey);
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_MarkerInterface_DeepRecord_NoLink()
        {
            FormLink<IPlaced> formLink = new FormLink<IPlaced>(UnusedFormKey);
            var package = new LoadOrderLinkCache<SkyrimMod>(TypicalLoadOrder());
            Assert.Null(formLink.Resolve(package));
        }

        [Fact]
        public void FormLink_LoadOrder_Resolve_MarkerInterface_DeepRecord_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var worldspace = mod.Worldspaces.AddNew();
            var subBlock = new WorldspaceSubBlock();
            var cell = new Cell(mod.GetNextFormKey());
            subBlock.Items.Add(cell);
            var placedNpc = new PlacedNpc(mod.GetNextFormKey());
            cell.Temporary.Add(placedNpc);
            var block = new WorldspaceBlock();
            block.Items.Add(subBlock);
            worldspace.SubCells.Add(block);
            var loadOrder = new LoadOrder<SkyrimMod>()
            {
                mod,
                new SkyrimMod(Utility.ModKey2),
            };
            var package = new LoadOrderLinkCache<SkyrimMod>(loadOrder);
            FormLink<IPlaced> placedFormLink = new FormLink<IPlaced>(placedNpc.FormKey);
            Assert.Same(placedNpc, placedFormLink.Resolve(package));
            FormLink<ICell> cellFormLink = new FormLink<ICell>(cell.FormKey);
            Assert.Same(cell, cellFormLink.Resolve(package));
            FormLink<IWorldspace> worldspaceFormLink = new FormLink<IWorldspace>(worldspace.FormKey);
            Assert.Same(worldspace, worldspaceFormLink.Resolve(package));
        }
        #endregion

        #region EDIDLink Resolves
        [Fact]
        public void EDIDLink_TryResolve_NoLink()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var effect = mod.MagicEffects.AddNew();
            effect.EditorID = "NULL";
            EDIDLink<IMagicEffect> link = new EDIDLink<IMagicEffect>(new RecordType("LINK"));
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            Assert.False(link.TryResolve(package, out var _));
        }

        [Fact]
        public void EDIDLink_TryResolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var effect = mod.MagicEffects.AddNew();
            effect.EditorID = "LINK";
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            EDIDLink<IMagicEffect> link = new EDIDLink<IMagicEffect>(new RecordType("LINK"));
            Assert.True(link.TryResolve(package, out var linkedRec));
            Assert.Same(effect, linkedRec);
        }

        [Fact]
        public void EDIDLink_Resolve_NoLink()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var effect = mod.MagicEffects.AddNew();
            effect.EditorID = "NULL";
            EDIDLink<IMagicEffect> link = new EDIDLink<IMagicEffect>(new RecordType("LINK"));
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            Assert.Null(link.Resolve(package));
        }

        [Fact]
        public void EDIDLink_Resolve_Linked()
        {
            var mod = new SkyrimMod(Utility.ModKey);
            var effect = mod.MagicEffects.AddNew();
            effect.EditorID = "LINK";
            var package = new DirectModLinkCache<SkyrimMod>(mod);
            EDIDLink<IMagicEffect> link = new EDIDLink<IMagicEffect>(new RecordType("LINK"));
            Assert.Same(effect, link.Resolve(package));
        }
        #endregion
    }

    public class Linking_Direct_Tests : Linking_Abstract_Tests
    {
        public override bool ReadOnly => false;

        public override IDisposable ConvertMod(SkyrimMod mod, out ISkyrimModGetter getter)
        {
            getter = mod;
            return Disposable.Empty;
        }
    }

    public class Linking_Overlay_Tests : Linking_Abstract_Tests
    {
        public override bool ReadOnly => true;

        public override IDisposable ConvertMod(SkyrimMod mod, out ISkyrimModGetter getter)
        {
            var tempFile = new TempFile();
            mod.WriteToBinaryParallel(
                tempFile.File.Path,
                new BinaryWriteParameters()
                {
                     ModKeySync = BinaryWriteParameters.ModKeySyncOption.NoCheck,
                });
            var overlay = SkyrimMod.CreateFromBinaryOverlay(tempFile.File.Path, mod.ModKey);
            getter = overlay;
            return Disposable.Create(() =>
            {
                overlay.Dispose();
                tempFile.Dispose();
            });
        }
    }
}
