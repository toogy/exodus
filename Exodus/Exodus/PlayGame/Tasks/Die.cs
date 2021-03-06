﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exodus.PlayGame.Tasks
{
    [Serializable]
    class Die : Task
    {
        public Die(Item parent)
            : base(parent, "Die", "Delete the selected item")
        {
        }

        public override void Initialize()
        {
            if (Parent != null)
            {
                Map.ListItems.Remove(Parent);
                Map.ListSelectedItems.Remove(Parent.PrimaryId);
                for (int x = Parent.pos.Value.X, mx = x + Parent.Width; x < mx; x++)
                    for (int y = Parent.pos.Value.Y, my = y + Parent.Width; y < my; y++)
                        Map.MapCells[x, y].ListItems.Remove(Parent);
                for (int i = 0; i < Parent.TasksList.Count;)
                {
                    if (i == 0)
                    {
                        Parent.TasksList[0].MustStop = true;
                        i++;
                    }
                    else
                    {
                        Parent.TasksList[i].BruteFinish();
                        Parent.TasksList.RemoveAt(i);
                    }
                }
                Finished = true;
            }
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
        }
    }
}
