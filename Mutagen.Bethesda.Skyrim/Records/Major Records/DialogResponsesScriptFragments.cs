﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Skyrim
{
    public partial class DialogResponsesScriptFragments
    {
        [Flags]
        public enum Flag
        {
            OnBegin = 0x01,
            OnEnd = 0x02,
        }
    }
}
