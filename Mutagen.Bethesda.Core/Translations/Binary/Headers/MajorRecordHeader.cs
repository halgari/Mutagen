using Mutagen.Bethesda.Binary;
using Noggog;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Binary
{
    /// <summary>
    /// A ref struct that overlays on top of bytes that is able to retrive Major Record header data on demand.
    /// </summary>
    public ref struct MajorRecordHeader
    {
        /// <summary>
        /// Game metadata to use as reference for alignment
        /// </summary>
        public GameConstants Meta { get; }
        
        /// <summary>
        /// Bytes overlaid onto
        /// </summary>
        public ReadOnlySpan<byte> Span { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="meta">Game metadata to use as reference for alignment</param>
        /// <param name="span">Span to overlay on, aligned to the start of the Major Record's header</param>
        public MajorRecordHeader(GameConstants meta, ReadOnlySpan<byte> span)
        {
            this.Meta = meta;
            this.Span = span.Slice(0, meta.MajorConstants.HeaderLength);
        }

        /// <summary>
        /// GameMode associated with header
        /// </summary>
        public GameMode GameMode => Meta.GameMode;
        
        /// <summary>
        /// The length that the header itself takes
        /// </summary>
        public sbyte HeaderLength => Meta.MajorConstants.HeaderLength;
        
        /// <summary>
        /// RecordType of the header
        /// </summary>
        public RecordType RecordType => new RecordType(BinaryPrimitives.ReadInt32LittleEndian(this.Span.Slice(0, 4)));
        
        /// <summary>
        /// The length explicitly contained in the length bytes of the header
        /// Note that for Major Records, this is equivalent to ContentLength
        /// </summary>
        public uint RecordLength => BinaryPrimitives.ReadUInt32LittleEndian(this.Span.Slice(4, this.Meta.MajorConstants.LengthLength));
        
        /// <summary>
        /// The length of the content of the Group, excluding the header bytes.
        /// </summary>
        public uint ContentLength => RecordLength;
        
        /// <summary>
        /// The integer representing a Major Record's flags enum.
        /// Since each game has its own flag Enum, this field is offered as an int that should
        /// be casted to the appropriate enum for use.
        /// </summary>
        public int MajorRecordFlags => BinaryPrimitives.ReadInt32LittleEndian(this.Span.Slice(8, 4));
        
        /// <summary>
        /// FormID of the Major Record
        /// </summary>
        public FormID FormID => FormID.Factory(BinaryPrimitives.ReadUInt32LittleEndian(this.Span.Slice(12, 4)));
        
        /// <summary>
        /// Total length of the Major Record, including the header and its content.
        /// </summary>
        public long TotalLength => this.HeaderLength + this.ContentLength;
        
        /// <summary>
        /// Whether the compression flag is on
        /// </summary>
        public bool IsCompressed => (this.MajorRecordFlags & Mutagen.Bethesda.Internals.Constants.CompressedFlag) > 0;

        /// <inheritdoc/>
        public override string ToString() => $"{RecordType} =>0x{ContentLength:X}";
    }

    /// <summary>
    /// A ref struct that overlays on top of bytes that is able to retrive Major Record header data on demand.
    /// It requires to be overlaid on writable bytes, so that values can also be set and modified on the source bytes.
    /// </summary>
    public ref struct MajorRecordHeaderWritable
    {
        /// <summary>
        /// Game metadata to use as reference for alignment
        /// </summary>
        public GameConstants Meta { get; }
        
        /// <summary>
        /// Bytes overlaid onto
        /// </summary>
        public Span<byte> Span { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="meta">Game metadata to use as reference for alignment</param>
        /// <param name="span">Span to overlay on, aligned to the start of the Major Record's header</param>
        public MajorRecordHeaderWritable(GameConstants meta, Span<byte> span)
        {
            this.Meta = meta;
            this.Span = span.Slice(0, meta.MajorConstants.HeaderLength);
        }

        /// <summary>
        /// GameMode associated with header
        /// </summary>
        public GameMode GameMode => Meta.GameMode;
        
        /// <summary>
        /// The length that the header itself takes
        /// </summary>
        public sbyte HeaderLength => Meta.MajorConstants.HeaderLength;
        
        /// <summary>
        /// RecordType of the header
        /// </summary>
        public RecordType RecordType
        {
            get => new RecordType(BinaryPrimitives.ReadInt32LittleEndian(this.Span.Slice(0, 4)));
            set => BinaryPrimitives.WriteInt32LittleEndian(this.Span.Slice(0, 4), value.TypeInt);
        }
        
        /// <summary>
        /// The length of the content of the Group, excluding the header bytes.
        /// </summary>
        public uint ContentLength
        {
            get => RecordLength;
            set => RecordLength = value;
        }
        
        /// <summary>
        /// The length explicitly contained in the length bytes of the header
        /// Note that for Major Records, this is equivalent to ContentLength
        /// </summary>
        public uint RecordLength
        {
            get => BinaryPrimitives.ReadUInt32LittleEndian(this.Span.Slice(4, 4));
            set => BinaryPrimitives.WriteUInt32LittleEndian(this.Span.Slice(4, 4), value);
        }
        
        /// <summary>
        /// The integer representing a Major Record's flags enum.
        /// Since each game has its own flag Enum, this field is offered as an int that should
        /// be casted to the appropriate enum for use.
        /// </summary>
        public int MajorRecordFlags
        {
            get => BinaryPrimitives.ReadInt32LittleEndian(this.Span.Slice(8, 4));
            set => BinaryPrimitives.WriteInt32LittleEndian(this.Span.Slice(8, 4), value);
        }
        
        /// <summary>
        /// FormID of the Major Record
        /// </summary>
        public FormID FormID
        {
            get => FormID.Factory(BinaryPrimitives.ReadUInt32LittleEndian(this.Span.Slice(12, 4)));
            set => BinaryPrimitives.WriteUInt32LittleEndian(this.Span.Slice(12, 4), value.Raw);
        }
        
        /// <summary>
        /// Total length of the Major Record, including the header and its content.
        /// </summary>
        public long TotalLength => this.HeaderLength + this.RecordLength;
        
        /// <summary>
        /// Whether the compression flag is on
        /// </summary>
        public bool IsCompressed
        {
            get => (this.MajorRecordFlags & Mutagen.Bethesda.Internals.Constants.CompressedFlag) > 0;
            set
            {
                if (value)
                {
                    this.MajorRecordFlags |= Mutagen.Bethesda.Internals.Constants.CompressedFlag;
                }
                else
                {
                    this.MajorRecordFlags &= ~Mutagen.Bethesda.Internals.Constants.CompressedFlag;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{RecordType} => 0x{ContentLength:X}";
    }
    
    /// <summary>
    /// A ref struct that overlays on top of bytes that is able to retrive Major Record data on demand.
    /// </summary>
    public ref struct MajorRecordFrame
    {
        /// <summary>
        /// Header ref struct for accessing header data
        /// </summary>
        public MajorRecordHeader Header { get; }
        
        /// <summary>
        /// Raw bytes of both header and content data
        /// </summary>
        public ReadOnlySpan<byte> HeaderAndContentData { get; }
        
        /// <summary>
        /// Raw bytes of the content data, excluding the header
        /// </summary>
        public ReadOnlySpan<byte> Content => HeaderAndContentData.Slice(this.Header.HeaderLength, checked((int)this.Header.ContentLength));

        /// <summary>
        /// Total length of the Major Record, including the header and its content.
        /// </summary>
        public long TotalLength => this.Header.HeaderLength + Content.Length;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="meta">Game metadata to use as reference for alignment</param>
        /// <param name="span">Span to overlay on, aligned to the start of the header</param>
        public MajorRecordFrame(GameConstants meta, ReadOnlySpan<byte> span)
        {
            this.Header = meta.MajorRecord(span);
            this.HeaderAndContentData = span.Slice(0, checked((int)this.Header.TotalLength));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="header">Existing MajorRecordHeader struct</param>
        /// <param name="span">Span to overlay on, aligned to the start of the header</param>
        public MajorRecordFrame(MajorRecordHeader header, ReadOnlySpan<byte> span)
        {
            this.Header = header;
            this.HeaderAndContentData = span.Slice(0, checked((int)this.Header.TotalLength));
        }

        /// <inheritdoc/>
        public override string ToString() => this.Header.ToString();
    }

    /// <summary>
    /// A ref struct that overlays on top of bytes that is able to retrive Major Record data on demand.
    /// Unlike MajorRecordFrame, this struct exposes its data members as MemorySlices instead of Spans
    /// </summary>
    public ref struct MajorRecordMemoryFrame
    {
        /// <summary>
        /// Header ref struct for accessing header data
        /// </summary>
        public MajorRecordHeader Header { get; }
        
        /// <summary>
        /// Raw bytes of both header and content data
        /// </summary>
        public ReadOnlyMemorySlice<byte> HeaderAndContentData { get; }

        /// <summary>
        /// Total length of the Major Record, including the header and its content.
        /// </summary>
        public long TotalLength => this.Header.HeaderLength + Content.Length;

        /// <summary>
        /// Raw bytes of the content data, excluding the header
        /// </summary>
        public ReadOnlyMemorySlice<byte> Content => HeaderAndContentData.Slice(this.Header.HeaderLength, checked((int)this.Header.ContentLength));

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="meta">Game metadata to use as reference for alignment</param>
        /// <param name="span">Span to overlay on, aligned to the start of the header</param>
        public MajorRecordMemoryFrame(GameConstants meta, ReadOnlyMemorySlice<byte> span)
        {
            this.Header = meta.MajorRecord(span);
            this.HeaderAndContentData = span.Slice(0, checked((int)this.Header.TotalLength));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="header">Existing MajorRecordHeader struct</param>
        /// <param name="span">Span to overlay on, aligned to the start of the header</param>
        public MajorRecordMemoryFrame(MajorRecordHeader header, ReadOnlyMemorySlice<byte> span)
        {
            this.Header = header;
            this.HeaderAndContentData = span.Slice(0, checked((int)this.Header.TotalLength));
        }
    }
}
