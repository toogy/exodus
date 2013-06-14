﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exodus.PlayGame.Items.Units
{
    [Serializable]
    class Worker : Unit
    {
        public Worker(int idPlayer)
        {
            Name = "Zinedine ZiTrap";
            maxLife = 4242;
            maxShield = 4242;
            Speed = 200;
            AttackStrength = 500;
            AttackDelayMax = 2000;
            IdPlayer = idPlayer;
            Initialize(40, 76, 9, 10);
            this.ItemsProductibles.Add(typeof(PlayGame.Items.Buildings.Habitation));
            //this.ItemsProductibles.Add(typeof(PlayGame.Items.Buildings.University));
            this.ItemsProductibles.Add(typeof(PlayGame.Items.Buildings.Laboratory));
            this.ItemsProductibles.Add(typeof(PlayGame.Items.Buildings.HydrogenExtractor));
            this.TasksOnMenu.Add(MenuTask.Build);
        }
    }
}
