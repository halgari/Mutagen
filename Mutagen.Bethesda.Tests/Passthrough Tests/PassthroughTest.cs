using Mutagen.Bethesda.Binary;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Oblivion.Internals;
using Mutagen.Bethesda.Preprocessing;
using Noggog;
using Noggog.Streams.Binary;
using Noggog.Utility;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mutagen.Bethesda.Tests
{
    public abstract class PassthroughTest
    {
        public string Nickname { get; }
        public FilePath FilePath { get; set; }
        public byte NumMasters { get; }
        public PassthroughSettings Settings { get; }
        public Target Target { get; }
        public string ExportFileName(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_NormalExport");
        public string ObservableExportFileName(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_ObservableExport");
        public string UncompressedFileName(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_Uncompressed");
        public string AlignedFileName(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_Aligned");
        public string OrderedFileName(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_Ordered");
        public string ProcessedPath(TempFolder tmp) => Path.Combine(tmp.Dir.Path, $"{this.Nickname}_Processed");
        public ModKey ModKey => ModKey.Factory(this.FilePath.Name);

        public abstract GameMode GameMode { get; }
        public readonly GameConstants Meta;
        protected abstract Processor ProcessorFactory();

        public PassthroughTest(TestingSettings settings, Target target)
        {
            this.FilePath = Path.Combine(settings.DataFolderLocations.Get(target.GameMode), target.Path);
            this.Nickname = target.Path;
            this.NumMasters = target.NumMasters;
            this.Settings = settings.PassthroughSettings;
            this.Target = target;
            this.Meta = GameConstants.Get(this.GameMode);
        }

        public abstract ModRecordAligner.AlignmentRules GetAlignmentRules();

        public async Task<TempFolder> SetupProcessedFiles()
        {
            var tmp = new TempFolder(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"Mutagen_Binary_Tests/{Nickname}")), deleteAfter: Settings.DeleteCachesAfter);

            var outputPath = ExportFileName(tmp);
            var observableOutputPath = ObservableExportFileName(tmp);
            var uncompressedPath = UncompressedFileName(tmp);
            var alignedPath = AlignedFileName(tmp);
            var orderedPath = OrderedFileName(tmp);
            var preprocessedPath = alignedPath;
            var processedPath = ProcessedPath(tmp);

            Mutagen.Bethesda.RecordInterest interest = null;
            if (this.Target.Interest != null)
            {
                interest = new Mutagen.Bethesda.RecordInterest(
                    this.Target.Interest.InterestingTypes
                        .Select(i => new RecordType(i)),
                    this.Target.Interest.UninterestingTypes
                        .Select(i => new RecordType(i)));
            }

            if (!Settings.ReuseCaches || !File.Exists(uncompressedPath))
            {
                try
                {
                    using var outStream = new FileStream(uncompressedPath, FileMode.Create, FileAccess.Write);
                    ModDecompressor.Decompress(
                        streamCreator: () => File.OpenRead(this.FilePath.Path),
                        gameMode: this.GameMode,
                        outputStream: outStream,
                        interest: interest);
                }
                catch (Exception)
                {
                    if (File.Exists(uncompressedPath))
                    {
                        File.Delete(uncompressedPath);
                    }
                    throw;
                }
            }

            if (Settings.ReorderRecords && (!Settings.ReuseCaches || !File.Exists(orderedPath)))
            {
                try
                {
                    using var outStream = new FileStream(orderedPath, FileMode.Create);
                    ModRecordSorter.Sort(
                        streamCreator: () => File.OpenRead(uncompressedPath),
                        outputStream: outStream,
                        gameMode: this.Target.GameMode);
                }
                catch (Exception)
                {
                    if (File.Exists(orderedPath))
                    {
                        File.Delete(orderedPath);
                    }
                    throw;
                }
            }

            if (!Settings.ReuseCaches || !File.Exists(alignedPath))
            {
                ModRecordAligner.Align(
                    inputPath: Settings.ReorderRecords ? orderedPath : uncompressedPath,
                    outputPath: alignedPath,
                    gameMode: this.GameMode,
                    alignmentRules: GetAlignmentRules(),
                    temp: tmp);
            }

            BinaryFileProcessor.Config instructions;
            if (!Settings.ReuseCaches || !File.Exists(processedPath))
            {
                instructions = new BinaryFileProcessor.Config();

                var processor = this.ProcessorFactory();
                if (processor != null)
                {
                    processor.Process(
                        tmpFolder: tmp,
                        sourcePath: this.FilePath.Path,
                        preprocessedPath: alignedPath,
                        outputPath: processedPath,
                        numMasters: this.NumMasters);
                }
            }

            return tmp;
        }

        protected abstract Task<IMod> ImportBinary(FilePath path);
        protected abstract Task<IModDisposeGetter> ImportBinaryOverlay(FilePath path);
        protected abstract Task<IMod> ImportCopyIn(FilePath file);

        public async IAsyncEnumerable<(string TestName, Exception ex)> BinaryPassthroughTest()
        {
            using var tmp = await SetupProcessedFiles();

            var outputPath = Path.Combine(tmp.Dir.Path, $"{this.Nickname}_NormalExport");
            var processedPath = ProcessedPath(tmp);
            var orderedPath = Path.Combine(tmp.Dir.Path, $"{this.Nickname}_Ordered");
            var binaryOverlayPath = Path.Combine(tmp.Dir.Path, $"{this.Nickname}_BinaryOverlay");
            var copyInPath = Path.Combine(tmp.Dir.Path, $"{this.Nickname}_CopyIn");
            var strsProcessedPath = Path.Combine(tmp.Dir.Path, "Strings/Processed");

            List<Exception> delayedExceptions = new List<Exception>();

            var writeParams = new BinaryWriteParameters()
            {
                ModKeySync = BinaryWriteParameters.ModKeySyncOption.NoCheck,
                MastersListSync = BinaryWriteParameters.MastersListSyncOption.NoCheck,
            };

            // Do normal
            if (Settings.TestNormal)
            {
                var strsWriteDir = Path.Combine(tmp.Dir.Path, "Strings", $"{this.Nickname}_Normal");
                bool doStrings = false;
                yield return await TestBattery.RunTest(
                    "Binary Normal Passthrough",
                    this.Target,
                    async () =>
                    {
                        var mod = await ImportBinary(this.FilePath.Path);
                        doStrings = mod.CanUseLocalization;

                        foreach (var record in mod.EnumerateMajorRecords())
                        {
                            record.IsCompressed = false;
                        }

                        using var stringsWriter = new StringsWriter(mod.ModKey, strsWriteDir);
                        writeParams.StringsWriter = stringsWriter;
                        mod.WriteToBinary(outputPath, writeParams);
                        GC.Collect();

                        using var stream = new MutagenBinaryReadStream(processedPath, this.GameMode);

                        AssertFilesEqual(
                            stream,
                            outputPath,
                            amountToReport: 15);
                    });
                if (doStrings)
                {
                    await foreach(var item in AssertStringsEqual(
                        "Binary Normal",
                        strsProcessedPath,
                        strsWriteDir))
                    {
                        yield return item;
                    }
                }
            }

            if (Settings.TestBinaryOverlay)
            {
                var strsWriteDir = Path.Combine(tmp.Dir.Path, "Strings", $"{this.Nickname}_Overlay");
                bool doStrings = false;
                yield return await TestBattery.RunTest(
                    "Binary Overlay Passthrough",
                    this.Target,
                    async () =>
                    {
                        using (var wrapper = await ImportBinaryOverlay(this.FilePath.Path))
                        {
                            doStrings = wrapper.CanUseLocalization;
                            using var stringsWriter = new StringsWriter(wrapper.ModKey, strsWriteDir);
                            writeParams.StringsWriter = stringsWriter;
                            wrapper.WriteToBinary(binaryOverlayPath, writeParams);
                        }

                        using var stream = new MutagenBinaryReadStream(processedPath, this.GameMode);

                        PassthroughTest.AssertFilesEqual(
                            stream,
                            binaryOverlayPath,
                            amountToReport: 15);
                    });
                if (doStrings)
                {
                    await foreach (var item in AssertStringsEqual(
                        "Binary Overlay",
                        strsProcessedPath,
                        strsWriteDir))
                    {
                        yield return item;
                    }
                }
            }

            if (Settings.TestCopyIn)
            {
                var strsWriteDir = Path.Combine(tmp.Dir.Path, "Strings", $"{this.Nickname}_CopyIn");
                bool doStrings = false;
                yield return await TestBattery.RunTest(
                    "Copy In Passthrough",
                    this.Target,
                    async () =>
                    {
                        var copyIn = await ImportCopyIn(this.FilePath.Path);
                        using var stringsWriter = new StringsWriter(copyIn.ModKey, strsWriteDir);
                        writeParams.StringsWriter = stringsWriter;
                        copyIn.WriteToBinary(copyInPath, writeParams);

                        using var stream = new MutagenBinaryReadStream(processedPath, this.GameMode);

                        PassthroughTest.AssertFilesEqual(
                            stream,
                            copyInPath,
                            amountToReport: 15);
                    });
                if (doStrings)
                {
                    await foreach (var item in AssertStringsEqual(
                        "Copy In",
                        strsProcessedPath,
                        strsWriteDir))
                    {
                        yield return item;
                    }
                }
            }

            if (delayedExceptions.Count > 0)
            {
                throw new AggregateException(delayedExceptions);
            }
        }

        //public async Task<(Exception Exception, IEnumerable<RangeInt64> Sections, bool HadMore)> XmlFolderPassthroughTest()
        //{
        //    async Task CreateXmlFolder(string sourcePath, DirectoryPath dir)
        //    {
        //        var mod = await ImportBinary(sourcePath);
        //        await WriteXmlFolder(mod, dir);
        //    }

        //    using (var processedTmp = await this.SetupProcessedFiles())
        //    {
        //        using (var tmp = new TempFolder($"Mutagen_{this.Nickname}_XmlFolder", deleteAfter: false))
        //        {
        //            ModKey modKey = ModKey.Factory(this.FilePath.Name);
        //            var sourcePath = this.ProcessedPath(processedTmp);
        //            await CreateXmlFolder(sourcePath, tmp.Dir);
        //            GC.Collect();
        //            var reimport = await ImportXmlFolder(
        //                dir: tmp.Dir);
        //            GC.Collect();
        //            var reexportPath = Path.Combine(tmp.Dir.Path, "Reexport");
        //            reimport.WriteToBinary(
        //                reexportPath,
        //                modKeyOverride: this.ModKey);
        //            using (var stream = new BinaryReadStream(sourcePath))
        //            {
        //                return PassthroughTest.AssertFilesEqual(
        //                    stream,
        //                    reexportPath,
        //                    amountToReport: 15);
        //            }
        //        }
        //    }
        //}

        public async Task TestImport()
        {
            await ImportBinary(this.FilePath.Path);
        }

        public static PassthroughTest Factory(TestingSettings settings, Target target)
        {
            return target.GameMode switch
            {
                GameMode.Oblivion => new OblivionPassthroughTest(settings, target),
                GameMode.Skyrim => new SkyrimPassthroughTest(settings, target),
                _ => throw new NotImplementedException(),
            };
        }

        public static void AssertFilesEqual(
            Stream stream,
            string path2,
            RangeCollection ignoreList = null,
            ushort amountToReport = 5)
        {
            using var reader2 = new BinaryReadStream(path2);
            Stream compareStream = new ComparisonStream(
                stream,
                reader2);

            if (ignoreList != null)
            {
                compareStream = new BasicSubstitutionRangeStream(
                    compareStream,
                    ignoreList,
                    toSubstitute: 0);
            }

            var errs = GetDifferences(compareStream)
                .First(amountToReport)
                .ToArray();
            if (errs.Length > 0)
            {
                throw new DidNotMatchException(path2, errs, stream);
            }
            if (stream.Position != stream.Length)
            {
                throw new MoreDataException(path2, stream.Position);
            }
            if (reader2.Position != reader2.Length)
            {
                throw new UnexpectedlyMoreData(path2, reader2.Position);
            }
        }

        public async IAsyncEnumerable<(string TestName, Exception ex)> AssertStringsEqual(
            string nickname,
            DirectoryPath processedDir, 
            DirectoryPath writeDir)
        {
            foreach (var source in EnumExt.GetValues<StringsSource>())
            {
                var stringsFileName = StringsUtility.GetFileName(this.ModKey, Language.English, source);
                var sourcePath = Path.Combine(processedDir.Path, stringsFileName);
                var pathToTest = Path.Combine(writeDir.Path, stringsFileName);
                bool sourceExists = File.Exists(sourcePath);
                bool targetExists = File.Exists(pathToTest);
                yield return await TestBattery.RunTest($"{nickname} {source} Strings Passthrough",
                    async () =>
                    {
                        if (sourceExists != targetExists)
                        {
                            throw new ArgumentException($"Strings file presence did not match for source: {source}");
                        }
                        if (!sourceExists) return;
                        AssertFilesEqual(
                            new FileStream(sourcePath, FileMode.Open),
                            pathToTest);
                    });
            }
        }

        public static IEnumerable<RangeInt64> GetDifferences(Stream reader)
        {
            byte[] buf = new byte[4096];
            bool inRange = false;
            long startRange = 0;
            var len = reader.Length;
            long pos = 0;
            while (pos < len)
            {
                var read = reader.Read(buf, 0, buf.Length);
                for (int i = 0; i < read; i++)
                {
                    if (buf[i] != 0)
                    {
                        if (!inRange)
                        {
                            startRange = pos + i;
                            inRange = true;
                        }
                    }
                    else
                    {
                        if (inRange)
                        {
                            var sourceRange = new RangeInt64(startRange, pos + i);
                            yield return sourceRange;
                            inRange = false;
                        }
                    }
                }
                pos += read;
            }
        }
    }
}
