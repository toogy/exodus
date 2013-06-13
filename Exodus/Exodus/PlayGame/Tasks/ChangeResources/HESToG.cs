﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Exodus.PlayGame.Items.Units;

namespace Exodus.PlayGame.Tasks.ChangeResources
{
    [Serializable]
    class HESToG : ChangeResource
    {
        public HESToG(Item parent)
            : base(parent, 35000, new Resource(100, 0, 0, 20, 100), new Resource(0, 0, 50, 0, 0))
        {
        }
    }
}