﻿using Mutagen.Bethesda.Binary;
using Noggog;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mutagen.Bethesda.Skyrim
{
    public partial class DialogTopic
    {
        #region Interfaces
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string INamedRequiredGetter.Name => this.Name?.String ?? string.Empty;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string? INamedGetter.Name => this.Name?.String;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        TranslatedString ITranslatedNamedRequiredGetter.Name => this.Name ?? string.Empty;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string INamedRequired.Name
        {
            get => this.Name?.String ?? string.Empty;
            set => this.Name = new TranslatedString(value);
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        TranslatedString ITranslatedNamedRequired.Name
        {
            get => this.Name ?? string.Empty;
            set => this.Name = value;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string? INamed.Name
        {
            get => this.Name?.String;
            set => this.Name = value == null ? null : new TranslatedString(value);
        }
        #endregion

        [Flags]
        public enum TopicFlag
        {
            DoAllBeforeRepeating = 0x1
        }

        public enum CategoryEnum
        {
            Topic = 0,
            Favor = 1,
            Scene = 2,
            Combat = 3,
            Favors = 4,
            Detection = 5,
            Service = 6,
            Misc = 7,
        }

        public enum SubtypeEnum
        {
            Custom = 0,
            ForceGreet = 1,
            Rumors = 2,
            Intimidate = 4,
            Flatter = 5,
            Bribe = 6,
            AskGift = 7,
            Gift = 8,
            AskFavor = 9,
            Favor = 10,
            ShowRelationships = 11,
            Follow = 12,
            Reject = 13,
            Scene = 14,
            Show = 15,
            Agree = 16,
            Refuse = 17,
            ExitFavorState = 18,
            MoralRefusal = 19,
            FlyingMountLand = 20,
            FlyingMountCancelLand = 21,
            FlyingMountAcceptTarget = 22,
            FlyingMountRejectTarget = 23,
            FlyingMountNoTarget = 24,
            FlyingMountDestinationReached = 25,
            Attack = 26,
            PowerAttack = 27,
            Bash = 28,
            Hit = 29,
            Flee = 30,
            Bleedout = 31,
            AvoidThreat = 32,
            Death = 33,
            GroupStrategy = 34,
            Block = 35,
            Taunt = 36,
            AllyKilled = 37,
            Steal = 38,
            Yield = 39,
            AcceptYield = 40,
            PickpocketCombat = 41,
            Assault = 42,
            Murder = 43,
            AssaultNC = 44,
            MurderNC = 45,
            PickpocketNC = 46,
            StealFromNC = 47,
            TrespassAgainstNC = 48,
            Trespass = 49,
            WerewolfTransformCrime = 50,
            VoicePowerStartShort = 51,
            VoicePowerStartLong = 52,
            VoicePowerEndShort = 53,
            VoicePowerEndLong = 54,
            AlertIdle = 55,
            LostIdle = 56,
            NormalToAlert = 57,
            AlertToCombat = 58,
            NormalToCombat = 59,
            AlertToNormal = 60,
            CombatToNormal = 61,
            CombatToLost = 62,
            LostToNormal = 63,
            LostToCombat = 64,
            DetectFriendDie = 65,
            ServiceRefusal = 66,
            Repair = 67,
            Travel = 68,
            Training = 69,
            BarterExit = 70,
            RepairExit = 71,
            Recharge = 72,
            RechargeExit = 73,
            TrainingExit = 74,
            ObserveCombat = 75,
            NoticeCorpse = 76,
            TimeToGo = 77,
            Goodbye = 78,
            Hello = 79,
            SwingMeleeWeapon = 80,
            ShootBow = 81,
            ZKeyObject = 82,
            Jump = 83,
            KnockOverObject = 84,
            DestroyObject = 85,
            StandOnFurniture = 86,
            LockedObject = 87,
            PickpocketTopic = 88,
            PursueIdleTopic = 89,
            SharedInfo = 90,
            PlayerCastProjectileSpell = 91,
            PlayerCastSelfSpell = 92,
            PlayerShout = 93,
            Idle = 94,
            EnterSprintBreath = 95,
            EnterBowZoomBreath = 96,
            ExitBowZoomBreath = 97,
            ActorCollideWithActor = 98,
            PlayerInIronSights = 99,
            OutOfBreath = 100,
            CombatGrunt = 101,
            LeaveWaterBreath = 102,
        }
    }

    namespace Internals
    {
        public partial class DialogTopicCommon
        {
            partial void PostDuplicate(DialogTopic obj, DialogTopic rhs, Func<FormKey> getNextFormKey, IList<(IMajorRecordCommon Record, FormKey OriginalFormKey)>? duplicatedRecords)
            {
                obj.Responses.SetTo(rhs.Responses.Select((dia) => (DialogResponses)dia.Duplicate(getNextFormKey, duplicatedRecords)));
            }
        }

        public partial class DialogTopicBinaryCreateTranslation
        {
            static partial void CustomBinaryEndImport(MutagenFrame frame, IDialogTopicInternal obj)
            {
                if (frame.Reader.Complete) return;
                GroupHeader groupMeta = frame.GetGroup();
                if (!groupMeta.IsGroup) return;
                if (groupMeta.GroupType == (int)GroupTypeEnum.TopicChildren)
                {
                    obj.Timestamp = BinaryPrimitives.ReadInt32LittleEndian(groupMeta.LastModifiedSpan);
                    obj.Unknown = frame.GetInt32(offset: 20);
                    if (FormKey.Factory(frame.MetaData.MasterReferences!, BinaryPrimitives.ReadUInt32LittleEndian(groupMeta.ContainedRecordTypeSpan)) != obj.FormKey)
                    {
                        throw new ArgumentException("Dialog children group did not match the FormID of the parent.");
                    }
                }
                else
                {
                    return;
                }
                frame.Reader.Position += groupMeta.HeaderLength;
                obj.Responses.SetTo(Mutagen.Bethesda.Binary.ListBinaryTranslation<DialogResponses>.Instance.Parse(
                    frame: frame.SpawnWithLength(groupMeta.ContentLength),
                    transl: (MutagenFrame r, RecordType header, out DialogResponses listItem) =>
                    {
                        return LoquiBinaryTranslation<DialogResponses>.Instance.Parse(
                            frame: r,
                            item: out listItem);
                    }));
            }

            static partial void FillBinaryResponseCountCustom(MutagenFrame frame, IDialogTopicInternal item)
            {
                // Skip counter
                frame.ReadSubrecordFrame();
            }
        }

        public partial class DialogTopicBinaryWriteTranslation
        {
            static partial void WriteBinaryResponseCountCustom(MutagenWriter writer, IDialogTopicGetter item)
            {
                if (!item.Responses.TryGet(out var resp)
                    || resp.Count == 0)
                {
                    using (HeaderExport.ExportSubrecordHeader(writer, DialogTopic_Registration.TIFC_HEADER))
                    {
                        writer.WriteZeros(4);
                    }
                }
                else
                {
                    using (HeaderExport.ExportSubrecordHeader(writer, DialogTopic_Registration.TIFC_HEADER))
                    {
                        writer.Write(resp.Count);
                    }
                }
            }

            static partial void CustomBinaryEndExport(MutagenWriter writer, IDialogTopicGetter obj)
            {
                if (!obj.Responses.TryGet(out var resp)
                    || resp.Count == 0)
                {
                    return;
                }
                using (HeaderExport.ExportHeader(writer, Group_Registration.GRUP_HEADER, ObjectType.Group))
                {
                    FormKeyBinaryTranslation.Instance.Write(
                        writer,
                        obj.FormKey);
                    writer.Write((int)GroupTypeEnum.TopicChildren);
                    writer.Write(obj.Timestamp);
                    writer.Write(obj.Unknown);
                    Mutagen.Bethesda.Binary.ListBinaryTranslation<IDialogResponsesGetter>.Instance.Write(
                        writer: writer,
                        items: resp,
                        transl: (MutagenWriter subWriter, IDialogResponsesGetter subItem) =>
                        {
                            subItem.WriteToBinary(subWriter);
                        });
                }
            }
        }

        public partial class DialogTopicBinaryOverlay
        {
            #region Interfaces
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            string INamedRequiredGetter.Name => this.Name?.String ?? string.Empty;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            string? INamedGetter.Name => this.Name?.String;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            TranslatedString ITranslatedNamedRequiredGetter.Name => this.Name ?? string.Empty;
            #endregion

            public IReadOnlyList<IDialogResponsesGetter> Responses { get; private set; } = ListExt.Empty<IDialogResponsesGetter>();

            private ReadOnlyMemorySlice<byte>? _grupData;

            public int Timestamp => _grupData != null ? BinaryPrimitives.ReadInt32LittleEndian(_package.MetaData.Constants.Group(_grupData.Value).LastModifiedSpan) : 0;

            public int Unknown => BinaryPrimitives.ReadInt32LittleEndian(_grupData!.Value.Slice(20));

            partial void CustomEnd(BinaryMemoryReadStream stream, int finalPos, int offset)
            {
                if (stream.Complete) return;
                var startPos = stream.Position;
                var groupMeta = this._package.MetaData.Constants.GetGroup(stream);
                if (!groupMeta.IsGroup) return;
                if (groupMeta.GroupType != (int)GroupTypeEnum.TopicChildren) return;
                this._grupData = stream.ReadMemory(checked((int)groupMeta.TotalLength));
                var formKey = FormKey.Factory(_package.MetaData.MasterReferences!, BinaryPrimitives.ReadUInt32LittleEndian(groupMeta.ContainedRecordTypeSpan));
                if (formKey != this.FormKey)
                {
                    throw new ArgumentException("Dialog children group did not match the FormID of the parent.");
                }
                var contentSpan = this._grupData.Value.Slice(_package.MetaData.Constants.GroupConstants.HeaderLength);
                this.Responses = BinaryOverlayList<IDialogResponsesGetter>.FactoryByArray(
                    contentSpan,
                    _package,
                    getter: (s, p) => DialogResponsesBinaryOverlay.DialogResponsesFactory(new BinaryMemoryReadStream(s), p),
                    locs: ParseRecordLocations(
                        stream: new BinaryMemoryReadStream(contentSpan),
                        finalPos: contentSpan.Length,
                        trigger: DialogResponses_Registration.TriggeringRecordType,
                        constants: GameConstants.Skyrim.MajorConstants,
                        skipHeader: false));
            }
        }
    }
}
