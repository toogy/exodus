﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exodus.PlayGame.Items.Obstacles
{
    [Serializable]
    class Gas : Obstacle
    {
        public int Hydrogen = 5000;
        public Gas()
        {
            Name = "Gas";
            maxLife = 666;
            maxShield = 666;
            Width = 2;
            base.Initialize(40, 24, 25, 24);
        }

        protected override void UpdateAnim()
        {
            anim = (int)(Hydrogen > 0 ? Animation.Anim : Animation.Stand);
        }
    }
}
