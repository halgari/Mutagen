using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Noggog;

namespace Mutagen.Bethesda.Generation
{
    public class EnumType : Loqui.Generation.EnumType
    {
        public int ByteLength { get; private set; }
        public int? HasBeenSetFallbackInt { get; private set; }

        public override async Task Load(XElement node, bool requireName = true)
        {
            await base.Load(node, requireName);
            ByteLength = node.GetAttribute<int>(Constants.ByteLength, 4);
            if (node.TryGetAttribute<int>("hasBeenSetBinaryFallback", out var i))
            {
                HasBeenSetFallbackInt = i;
            }
        }
    }
}
