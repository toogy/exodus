﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exodus.GUI.Components.Buttons.GameButtons;
using Exodus.GUI.Components.Buttons.MenuButtons;
using Exodus.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Exodus.PlayGame;
using Exodus.PlayGame.Items.Buildings;
using Exodus.PlayGame.Items.Units;

namespace Exodus.GUI.Items
{
    internal class TasksDisplayer : Item
    {
        private Stack<List<Component>> stackComponents;
        private List<PlayGame.Item> canProduceUnits;
        private List<Type> unitProductibles;
        private List<PlayGame.Item> canProduceBuildings;
        private List<Type> buildingsProductibles;
        private bool[] taskPossibles;
        private int _step = 5;

        private void Pop()
        {
            stackComponents.Pop();
            Components = stackComponents.Peek();
        }
        private void Push(List<Component> l)
        {
            stackComponents.Push(l);
            Components = l;
        }
        public TasksDisplayer(int x, int y)
        {
            Area = new Rectangle(x, y, Textures.GameUI["actions"].Width, Textures.GameUI["actions"].Height);
            stackComponents = new Stack<List<Component>>();
            taskPossibles = new bool[Enum.GetValues(typeof(MenuTask)).Length];
            canProduceBuildings = new List<PlayGame.Item>();
            canProduceUnits = new List<PlayGame.Item>();
            unitProductibles = new List<Type>();
            buildingsProductibles = new List<Type>();
            Reset();
        }
        public override void Update(GameTime gameTime)
        {
            if (Inputs.KeyPress(Microsoft.Xna.Framework.Input.Keys.Escape) && stackComponents.Count > 1)
                Pop();
            base.Update(gameTime);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Textures.GameUI["actions"], Area, null,
                             Color.White, 0f, Vector2.Zero, SpriteEffects.None, 41 * Data.GameDisplaying.Epsilon);
            base.Draw(spriteBatch);
        }
        private void Hold(Type t)
        {
            PlayGame.Item c;
            if (Data.Network.SinglePlayer)
            {
                for (int i = 0; i < Map.ListSelectedItems.Count; i++)
                {
                    c = Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]);
                    if (c is Unit)
                        c.AddTask(new PlayGame.Tasks.Hold(c), true, false);
                }
            }
            else
            {
                for (int i = 0; i < Map.ListSelectedItems.Count; i++)
                    if (Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]) is Unit)
                        Network.ClientSide.Client.SendObject(
                            new Network.Orders.Tasks.Hold(Map.ListSelectedItems[i], true)
                        );
            }
        }
        public void Reset()
        {
            stackComponents.Clear();
            canProduceUnits.Clear();
            canProduceBuildings.Clear();
            unitProductibles.Clear();
            buildingsProductibles.Clear();
            FindTasksPossibles();
            int currentX = Area.X + 9;
            List<Component> display = new List<Component>();
            Texture2D t;
            if (taskPossibles[(int)MenuTask.Attack])
            {
                t = Textures.GameUI["Attack"];
                display.Add(
                    new Mini(Attack, default(Type), t, Textures.GameUI["Attackhover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.Hold])
            {
                t = Textures.GameUI["Hold"];
                display.Add(
                    new Mini(Hold, default(Type), t, Textures.GameUI["Holdhover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.Patrol])
            {
                t = Textures.GameUI["Patrol"];
                display.Add(
                    new Mini(Patrol, default(Type), t, Textures.GameUI["Patrolhover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.Build])
            {
                t = Textures.GameUI["Build"];
                display.Add(
                    new Mini(AddBuildings, default(Type), t, Textures.GameUI["Buildhover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.ProductUnits])
            {
                t = Textures.GameUI["Build"];
                display.Add(
                    new Mini(AddUnits, default(Type), t, Textures.GameUI["Buildhover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.Research])
            {
                t = Textures.GameUI["Research"];
                display.Add(
                     new Mini(AddResearchs, default(Type), t, Textures.GameUI["Researchhover"], currentX, Area.Y + 14)
                     );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.Die])
            {
                t = Textures.GameUI["Die"];
                display.Add(
                    new Mini(DoNothing, default(Type), t, Textures.GameUI["Diehover"], currentX, Area.Y + 14)
                    );
                currentX += t.Width + _step;
            }
            if (taskPossibles[(int)MenuTask.ChangeResources])
            {
                t = Textures.GameUI["Research"];
                display.Add(
                    new Mini(AddRessourcesChange, default(Type), t, Textures.GameUI["Researchhover"], currentX, Area.Y + 14)
                );
                currentX += t.Width + _step;
            }
            if (display.Count == 1)
            {
                if (taskPossibles[(int)MenuTask.ProductUnits])
                    AddUnits(default(Type));
                else if (taskPossibles[(int)MenuTask.Build])
                    AddBuildings(default(Type));
                else if (taskPossibles[(int)MenuTask.ProductUnits])
                    AddResearchs(default(Type));
                else if (taskPossibles[(int)MenuTask.ChangeResources])
                    AddRessourcesChange(default(Type));
            }
            else
                Push(display);
        }
        public void FindTasksPossibles()
        {
            int falseLeft = taskPossibles.Length;
            int i;
            for (i = 0; i < falseLeft; i++)
                taskPossibles[i] = false;
            i = 0;
            PlayGame.Item c;
            while (i < Map.ListSelectedItems.Count && falseLeft > 0)
            {
                c = Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]);
                for (int j = 0; j < c.TasksOnMenu.Count; j++)
                    if (!taskPossibles[(int)c.TasksOnMenu[j]])
                    {
                        if (c.TasksOnMenu[j] == MenuTask.ProductUnits)
                        {
                            canProduceUnits.Add(c);
                            foreach (Type t in c.ItemsProductibles)
                                if (!unitProductibles.Contains(t))
                                    unitProductibles.Add(t);
                        }
                        else if (c.TasksOnMenu[j] == MenuTask.Build)
                        {
                            canProduceBuildings.Add(c);
                            foreach (Type t in c.ItemsProductibles)
                                if (!buildingsProductibles.Contains(t))
                                    buildingsProductibles.Add(t);
                        }
                        taskPossibles[(int)c.TasksOnMenu[j]] = true;
                        falseLeft--;
                    }
                i++;
            }

        }
        private void AddBuildings(Type t)
        {
            List<Component> result = new List<Component>();
            int currentX = Area.X + 9;
            Texture2D te;
            foreach (Type ty in buildingsProductibles)
                //if (ty == typeof(Habitation) || ty == typeof(University))
                {
                    te = Textures.MiniGameItems[ty];
                    result.Add(new Mini(CreateBuilding, ty, te, te, currentX, Area.Y + 14));
                    currentX += te.Width + _step;
                }
            Push(result);
        }
        private void AddUnits(Type t)
        {
            List<Component> result = new List<Component>();
            int currentX = Area.X + 9;
            Texture2D te;
            foreach (Type ty in unitProductibles)
                //if (ty == typeof(Worker) || ty == typeof(Gunner))
                {
                    te = Textures.MiniGameItems[ty];
                    result.Add(new Mini(CreateUnit, ty, te, te, currentX, Area.Y + 14));
                    currentX += te.Width + _step;
                }
            Push(result);
        }
        private void AddResearchs(Type t)
        {

        }
        private void AddRessourcesChange(Type t)
        {
            List<Component> result = new List<Component>();
            int currentX = Area.X + 9;
            Texture2D te;
            te = Textures.GameUI["LabElectrecity"];
            result.Add(new Mini(ChangeResource, typeof(PlayGame.Tasks.ChangeResources.HToE), te, Textures.GameUI["LabElectrecityHover"], currentX, Area.Y + 14));
            currentX += te.Width + _step;
            te = Textures.GameUI["LabSteel"];
            result.Add(new Mini(ChangeResource, typeof(PlayGame.Tasks.ChangeResources.HEIToS), te, Textures.GameUI["LabSteelHover"], currentX, Area.Y + 14));
            currentX += te.Width + _step;
            te = Textures.GameUI["LabGraphene"];
            result.Add(new Mini(ChangeResource, typeof(PlayGame.Tasks.ChangeResources.HESToG), te, Textures.GameUI["LabGrapheneHover"], currentX, Area.Y + 14));
            currentX += te.Width + _step;
            Push(result);
        }
        private void ChangeResource(Type t)
        {
            PlayGame.Item item = null,
                          c;
            // = Map.ListSelectedItems.FirstOrDefault(s => s.ItemsProductibles.Exists(ty => ty == t));
            for (int i = 0; i < Map.ListSelectedItems.Count; i++)
            {
                c = Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]);
                if (c.TasksOnMenu.Exists(task => task == MenuTask.ChangeResources) &&
                    (item == null || item.TasksList.Count > c.TasksList.Count))
                    item = c;
            }
            if (item != null)
            {
                if (t == typeof(PlayGame.Tasks.ChangeResources.HToE))
                    item.AddTask(
                        new PlayGame.Tasks.ChangeResources.HToE(item),
                        false,
                        false
                    );
                else if (t == typeof(PlayGame.Tasks.ChangeResources.HEIToS))
                    item.AddTask(
                        new PlayGame.Tasks.ChangeResources.HEIToS(item),
                        false,
                        false
                    );
                else if (t == typeof(PlayGame.Tasks.ChangeResources.HESToG))
                    item.AddTask(
                        new PlayGame.Tasks.ChangeResources.HESToG(item),
                        false,
                        false
                    );
            }
        }
        private void CreateBuilding(Type t)
        {
            Data.GameInfos.item = Map.ListItems.Find(v => v.PrimaryId == Map.ListSelectedItems.FirstOrDefault(s => Map.ListItems.Find(u => u.PrimaryId == s).ItemsProductibles.Exists(ty => ty == t)));
            if (Data.GameInfos.item != null)
            {
                Data.GameInfos.currentMode = Data.GameInfos.ModeGame.Building;
                Data.GameInfos.type = t;
            }
        }
        private void CreateUnit(Type t)
        {
            PlayGame.Item item = null,
                                 c;
            // = Map.ListSelectedItems.FirstOrDefault(s => s.ItemsProductibles.Exists(ty => ty == t));
            for (int i = 0; i < Map.ListSelectedItems.Count; i++)
            {
                c = Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]);
                if (c.ItemsProductibles.Exists(ty => ty == t) &&
                    (item == null || item.TasksList.Count > c.TasksList.Count))
                    item = c;
            }
            if (item != null)
            {
                if (!(PlayGame.Map.PlayerResources >= Data.GameInfos.CostsItems[t]))
                {
                    Network.ClientSide.Client.chat.InsertMsg("Not enough resources: you are missing" + (Data.GameInfos.CostsItems[t] - PlayGame.Map.PlayerResources).ToString());
                }
                else
                {
                    PlayGame.Map.PlayerResources -= Data.GameInfos.CostsItems[t];
                    if (Data.Network.SinglePlayer)
                    {
                        item.AddTask(
                            new PlayGame.Tasks.ProductItem(item, Data.GameInfos.timeCreatingItem[t],
                                                           PlayGame.Items.Loader.LoadUnit(t, item.IdPlayer), item.pos.Value, true, true, false),
                            false, false);
                    }
                    else
                    {
                        Network.ClientSide.Client.SendObject(
                            new Network.Orders.Tasks.ProductItem(
                                item.PrimaryId,
                                false,
                                t,
                                item.pos.Value.X,
                                item.pos.Value.Y,
                                true,
                                true,
                                false)
                        );
                    }
                }
            }
        }
        public static void Die(Type t)
        {
            PlayGame.Item c;
            if (Data.Network.SinglePlayer)
            {
                for (int i = 0; i < Map.ListSelectedItems.Count; i++)
                {
                    c = Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]);
                    if (c is Unit)
                        c.AddTask(new PlayGame.Tasks.Die(c), true, false);
                }
            }
            else
            {
                for (int i = 0; i < Map.ListSelectedItems.Count; i++)
                {
                    if (Map.ListItems.Find(u => u.PrimaryId == Map.ListSelectedItems[i]) is Unit)
                        Network.ClientSide.Client.SendObject(
                            new Network.Orders.Tasks.Die(Map.ListSelectedItems[i], true)
                        );
                }
            }
        }
        private void DoNothing(Type t)
        {

        }
        private void Patrol(Type t)
        {

        }
        public static void Harvest(Type t)
        {
            int ironIndex = 0;
            while (ironIndex < Map.ListPassiveItems.Count && (!Map.ListPassiveItems[ironIndex].Intersect(Inputs.MouseState.X, Inputs.MouseState.Y)))
            {
                ironIndex++;
            }
            if (ironIndex < Map.ListPassiveItems.Count)
            {
                PlayGame.Item c;
                if (Data.Network.SinglePlayer)
                {
                    foreach (int selected in Map.ListSelectedItems)
                    {
                        c = Map.ListItems.Find(u => u.PrimaryId == selected);
                        c.AddTask(new PlayGame.Tasks.HarvestIron(c, Map.ListPassiveItems[ironIndex]), false, false);
                    }
                }
                else
                {
                    foreach (int item in Map.ListSelectedItems)
                    {
                        Network.ClientSide.Client.SendObject(
                            new Network.Orders.Tasks.HarvestIron(item, Map.ListPassiveItems[ironIndex].PrimaryId, true)
                        );
                    }
                }
            }
        }
        public static void Attack(Type t)
        {
            int enemyIndex = 0;
            while (enemyIndex < Map.ListItems.Count && (!(Map.ListItems[enemyIndex].Intersect(Inputs.MouseState.X, Inputs.MouseState.Y)) || Map.ListItems[enemyIndex].IdPlayer == Data.Network.IdPlayer))
            {
                enemyIndex++;
            }
            if (enemyIndex < Map.ListItems.Count)
            {
                PlayGame.Item c;
                bool b = !(Inputs.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Inputs.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift));
                if (Data.Network.SinglePlayer)
                {
                    foreach (int selected in Map.ListSelectedItems)
                    {
                        c = Map.ListItems.Find(u => u.PrimaryId == selected);
                        c.AddTask(new PlayGame.Tasks.Attack(c, Map.ListItems[enemyIndex], 0), b, false);
                    }
                }
                else
                {
                    foreach (int item in Map.ListSelectedItems)
                    {
                        Network.ClientSide.Client.SendObject(
                            new Network.Orders.Tasks.Attack(item, Map.ListItems[enemyIndex].PrimaryId, b, 0)
                        );
                    }
                }
            }
        }
    }
}
