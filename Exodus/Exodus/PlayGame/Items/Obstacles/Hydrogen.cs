﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exodus.PlayGame.Items.Obstacles
{
    [Serializable]
    class Hydrogen : Obstacle
    {
        public Hydrogen()
        {
            Name = "Hydrogen";
            maxLife = 666;
            maxShield = 666;
            Width = 2;
            base.Initialize(40, 24, 0, 0);
        }
    }
}