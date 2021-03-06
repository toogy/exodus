using System;
using System.Collections.Generic;
using System.Linq;
using Exodus.GUI;
using Exodus.GUI.Components;
using Exodus.GUI.Components.Buttons;
using Exodus.GUI.Components.Buttons.MenuButtons;
using Exodus.GUI.Items;
using Exodus.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using Exodus.Network;
using Exodus.Network.ClientSide;
using Exodus.Network.ServerSide;

namespace Exodus
{
    public class Application : Microsoft.Xna.Framework.Game
    {
        TextBox login, pass;
        TextBox settings_login,
                settings_pass1,
                settings_pass2;
        TextBox ipJoin;
        OrangeMenuButton joinGameButton;
        readonly GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;
        ScrollingSelection _scrollingSelection;
        MenuState _gameLauncherMenu;
        OrangeMenuButton _gameSelectionButton;
        bool _searchingLAN = true;
        Stack<GameState> GameStates;
        PlayerInfos PlayerInfos;
        StatusBar statusBar;
        Label observer1 = null, observer2 = null, observer3 = null;
        PlayerInfosLaunching player1 = null, player2 = null;
        public void Push(GameState g)
        {
            g.Initialize();
            g.LoadContent();
            if (GameStates.Count > 0)
            {
                if (GameStates.Peek().GetType() == g.GetType())
                {
                    g.Music = GameStates.Peek().Music;
                }
                else
                {
                    if (GameStates.Peek().Music != null)
                        GameStates.Peek().Music.Pause();
                    if (g.Music != null)
                        g.Music.Play();
                }
            }
            GameStates.Push(g);
        }
        public GameState Peek()
        {
            return GameStates.Peek();
        }
        public GameState Pop()
        {
            GameState g = GameStates.Pop();
            if (GameStates.Count > 0)
            {
                if (GameStates.Peek().GetType() != g.GetType())
                {
                    if (g.Music != null)
                        g.Music.Stop();
                    if (GameStates.Peek().Music != null)
                        GameStates.Peek().Music.Resume();
                }
            }
            g.UnLoad();
            if (GameStates.Count == 0)
                Environment.Exit(0);
            _searchingLAN = true;
            statusBar.Text = "Welcome !";
            statusBar.Active = false;
            _gameSelectionButton.Text = "SEARCH INTERNET";
            return g;
        }
        public Application()
        {
            _graphics = new GraphicsDeviceManager(this)
                {
                    PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                    PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
                };
            _graphics.ToggleFullScreen();
            Content.RootDirectory = "Content";
        }
        protected override void Initialize()
        {
            Inputs.Initialise();
            Data.Window.WindowWidth = Window.ClientBounds.Width;
            Data.Window.WindowHeight = Window.ClientBounds.Height;
            Data.Window.ScreenCenter = new Point((int)(Data.Window.WindowWidth / 2), (int)(Data.Window.WindowHeight / 2));
            GameStates = new Stack<GameState>();
            Fonts.Initialize(Content);

            base.Initialize();
        }
        protected override void LoadContent()
        {
            Data.GameDisplaying.GraphicsDevice = GraphicsDevice;
            this.IsMouseVisible = false;
            Inputs.Initialise();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Data.Load();
            Audio.LoadAudio(Content);
            Textures.LoadParticles(Content);
            Textures.LoadGame(Content);
            Textures.LoadGameItems(Content);
            Textures.LoadGameUI(Content);
            Textures.LoadMenu(Content);
            Textures.LoadMiniGameItems(Content);
            Textures.LoadBigGameItems(Content);
            GameMouse.Initialize(Content);
            Network.ClientSide.Client.g = this;
            statusBar = new StatusBar(Data.Window.ScreenCenter.X - Textures.Menu["StatusBar"].Width / 2,
                                                Data.Window.ScreenCenter.Y - 117);
            ParticleEngine.ParticleEngine particleMenu = new ParticleEngine.Engine.StarsEngine(
                Data.Window.WindowWidth / 2, Data.Window.WindowHeight / 2, 0);

            #region Game Launching
            MenuState gameLaunchingServer = LaunchingGameMenu(true, particleMenu);
            MenuState gameLaunchingClient = LaunchingGameMenu(false, particleMenu);
            #endregion

            #region Game Launcher
            _gameLauncherMenu = BaseMenu(particleMenu);
            _gameLauncherMenu.Items.Add(new Passive(Textures.Menu["ScrollingSelection"], (Data.Window.WindowWidth - Textures.Menu["ScrollingSelection"].Width) / 2, Data.Window.ScreenCenter.Y - 20, 3 * Data.GameDisplaying.Epsilon));
            MenuHorizontal createGameMenu = new MenuHorizontal(Data.Window.ScreenCenter.X - 330, Data.Window.ScreenCenter.Y - 8, 5);
            OrangeMenuButton createGameButton = new OrangeMenuButton("CREATE A GAME");
            _gameSelectionButton = new OrangeMenuButton("SEARCH INTERNET");
            _gameSelectionButton.DoClick = SwitchSearchingMode;
            createGameButton.DoClick = LaunchGameMenu;
            createGameButton.SubMenu = gameLaunchingServer;
            createGameMenu.Create(new List<Component> { createGameButton, _gameSelectionButton });
            MenuHorizontal joinGameMenu = new MenuHorizontal(Data.Window.ScreenCenter.X + 175, Data.Window.ScreenCenter.Y + 250, 0);
            joinGameButton = new OrangeMenuButton("JOIN THIS GAME");
            joinGameButton.DoClick = JoinMultiPlay;
            joinGameButton.SubMenu = gameLaunchingClient;
            ipJoin = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, Data.GameDisplaying.Epsilon * 2);
            MenuHorizontal ipJoinMenu = new MenuHorizontal(Data.Window.ScreenCenter.X - 90, Data.Window.ScreenCenter.Y + 246, 0);
            ipJoinMenu.Create(new List<Component> { ipJoin });
            joinGameMenu.Create(new List<Component> { joinGameButton });
            MenuVertical refreshMenu = new MenuVertical(Data.Window.ScreenCenter.X + 175, Data.Window.ScreenCenter.Y - 8, 0);
            OrangeMenuButton refreshButton = new OrangeMenuButton("REFRESH");
            refreshButton.DoClick = RefreshServerLists;
            refreshMenu.Create(new List<Component> { refreshButton });

            _scrollingSelection = new ScrollingSelection((Data.Window.WindowWidth - Textures.Menu["ScrollingSelection"].Width) / 2, Data.Window.ScreenCenter.Y - 20, new Tuple<string, string, string>("PLAYER", "IP                                MAP", "CREATED"), new List<int> { 10, 164, 590 });
            _scrollingSelection.Reset(new List<Tuple<string, string, string>>());
            _gameLauncherMenu.Items.Add(_scrollingSelection);
            _gameLauncherMenu.Items.Add(joinGameMenu);
            _gameLauncherMenu.Items.Add(ipJoinMenu);
            _gameLauncherMenu.Items.Add(createGameMenu);
            _gameLauncherMenu.Items.Add(refreshMenu);
            _gameLauncherMenu.Items.Add(statusBar);
            #endregion

            #region Settings
            MenuState settingsMenu = BaseMenu(particleMenu);
            settingsMenu.Items.Add(statusBar);
            Form settingsForm = new Form(Data.Window.ScreenCenter.X - 140, Data.Window.ScreenCenter.Y - 45,
                                           new Padding(14, 17), 8, 4 * Data.GameDisplaying.Epsilon);
            settingsForm.Components.Add(new JustTexture(Textures.Menu["Settings"], settingsForm.Area.X,
                                                           settingsForm.Area.Y, settingsForm.Depth));
            settingsForm.Components.Add(new Label(Fonts.Eurostile12, "CHANGE YOUR LOGIN", 0, 0, Data.GameDisplaying.Epsilon));
            settings_login = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, 2 * Data.GameDisplaying.Epsilon);
            settingsForm.Components.Add(settings_login);
            settingsForm.Components.Add(new Label(Fonts.Eurostile12, "CHANGE YOUR PASSWORD", 0, 0, Data.GameDisplaying.Epsilon));
            settings_pass1 = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, 2 * Data.GameDisplaying.Epsilon);
            settings_pass1.Hidden = true;
            settingsForm.Components.Add(settings_pass1);
            settings_pass2 = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, 2 * Data.GameDisplaying.Epsilon);
            settings_pass2.Hidden = true;
            settingsForm.Components.Add(settings_pass2);
            SoundButton soundButton = new SoundButton(0, 0, new Padding(14, -6), 2 * Data.GameDisplaying.Epsilon);
            soundButton.Progress = (float)Data.Config.LevelSound / 100f;
            soundButton.DoClick = SaveSound;
            settingsForm.Components.Add(new Label(Fonts.Eurostile12, "CHANGE SOUND LEVEL", 0, 0, Data.GameDisplaying.Epsilon));
            settingsForm.Components.Add(soundButton);
            ConnectionOrangeButton settingsFormSubmitter = new ConnectionOrangeButton("SAVE YOUR CHANGES")
            {
                SubMenu = null,
                DoClick = SaveSettings
            };
            settingsForm.SubmitterId = settingsForm.Components.Count;
            settingsForm.Components.Add(settingsFormSubmitter);
            settingsForm.Initialize();
            settingsMenu.Items.Add(settingsForm);
            #endregion

            #region Main Menu

            MenuState mainMenu = BaseMenu(particleMenu);
            MenuVertical rightMenu = new MenuVertical(Data.Window.ScreenCenter.X + 112, Data.Window.ScreenCenter.Y + 42,
                                                      7);
            MenuButton playOnline = new BlueMenuButton("PLAY ONLINE");
            playOnline.SubMenu = _gameLauncherMenu;
            playOnline.DoClick = LaunchLobby;
            MenuButton playSolo = new BlueMenuButton("PLAY AGAINST AI");
            playSolo.DoClick = PlaySinglePlayer;
            MenuButton mapEditor = new BlueMenuButton("MAP EDITOR");
            mapEditor.DoClick = Editor;
            MenuButton settings = new BlueMenuButton("SETTINGS");
            settings.SubMenu = settingsMenu;
            settings.DoClick = LaunchLobby;
            MenuButton credits = new BlueMenuButton("CREDITS");
            credits.DoClick = DoNothing;
            MenuButton exit = new BlueMenuButton("EXIT THE GAME");
            exit.DoClick = Exit;
            rightMenu.Create(new List<Component> { playOnline, playSolo, mapEditor, settings, credits, exit });
            mainMenu.Items.Add(rightMenu);
            mainMenu.Items.Add(statusBar);
            PlayerInfos = new PlayerInfos(Data.Window.ScreenCenter.X - 300, Data.Window.ScreenCenter.Y + 30, Data.GameDisplaying.Epsilon * 3);
            mainMenu.Items.Add(PlayerInfos);
            #endregion

            #region Connection Menu

            MenuState connectionMenu = BaseMenu(particleMenu);

            Form connectionForm = new Form(Data.Window.ScreenCenter.X - 140, Data.Window.ScreenCenter.Y - 25,
                                           new Padding(14, 17), 8, 4 * Data.GameDisplaying.Epsilon);
            connectionForm.Components.Add(new JustTexture(Textures.Menu["ConnectionBackground"], connectionForm.Area.X,
                                                          connectionForm.Area.Y, connectionForm.Depth));
            connectionForm.Components.Add(new Label(Fonts.Eurostile12, "LOGIN", 0, 0, Data.GameDisplaying.Epsilon));
            login = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, 2 * Data.GameDisplaying.Epsilon);
            connectionForm.Components.Add(login);
            connectionForm.Components.Add(new Label(Fonts.Eurostile12, "PASSWORD", 0, 0, Data.GameDisplaying.Epsilon));
            pass = new TextBox(0, 0, "", "ConnectionTextBox", new Padding(14, -6), 30, 2 * Data.GameDisplaying.Epsilon);
            pass.Hidden = true;
            connectionForm.Components.Add(pass);
            ConnectionOrangeButton connectionFormSubmitter = new ConnectionOrangeButton("CONNECT TO EXODUS ONLINE")
            {
                SubMenu = mainMenu,
                DoClick = ConnectionFormSubmit
            };
            connectionForm.SubmitterId = connectionForm.Components.Count;
            connectionForm.Components.Add(connectionFormSubmitter);
            ConnectionGreenButton connectionFormSignup = new ConnectionGreenButton("GET AN ACCOUNT FOR FREE");
            connectionFormSignup.DoClick = ConnectionForm;//FIX ME (lance une page html vers la page d'inscription du site)
            connectionForm.Components.Add(connectionFormSignup);
            connectionForm.Initialize();
            login.ResetValue(Data.Config.Login);
            pass.ResetValue(Data.Config.Pwd);
            connectionMenu.Items.Add(connectionForm);
            connectionMenu.Items.Add(statusBar);
            connectionMenu.Initialize();
            statusBar.Text = "Enter your logs";
            statusBar.Active = false;

            #endregion

            Push(connectionMenu);

            base.LoadContent();
        }
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            Data.Window.GameFocus = IsActive;
            GameMouse.Update(gameTime);
            Inputs.Update(gameTime);
            if (Inputs.KeyPress(Keys.F11))
                _graphics.ToggleFullScreen();
            /*if (Inputs.KeyPress(Keys.Escape))
            {
                if (GameStates.Count > 1)
                    GameStates.Pop().UnLoad();
                else
                    Environment.Exit(0);
            }*/
            if (ipJoin.Value == "" && joinGameButton.Text != "JOIN THIS GAME")
            {
                joinGameButton.Text = "JOIN THIS GAME";
                joinGameButton.SetPosition();
            }
            else if (ipJoin.Value != "" && joinGameButton.Text == "JOIN THIS GAME")
            {
                joinGameButton.Text = "JOIN THIS IP";
                joinGameButton.SetPosition();
            }
            GameStates.Peek().Update(gameTime);

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            #region On r�cup�re la totalit� des maps existantes et on met � jour pour l'affichage
            List<Network.Game> lg;
            if (_searchingLAN)
                lg = Client.ServerList;
            else
                lg = SyncClient.InternetGames;
            List<Tuple<string, string, string>> l = new List<Tuple<string, string, string>>();

            for (int i = 0, c = lg.Count; i < c; i++)
            {
                l.Add(new Tuple<string, string, string>(lg[i].HostName + " (" + lg[i].NbPlayers + ")", lg[i].IP + "  " + lg[i].Map, lg[i].CreationTime.ToShortTimeString()));
            }
            _scrollingSelection.Reset(l);
            #endregion
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            GameStates.Peek().Draw(_spriteBatch);
            GameMouse.Draw(_spriteBatch);
            _spriteBatch.End();
            base.Draw(gameTime);
        }
        #region subFuncs
        private void DoNothing(MenuState m, int i)
        {

        }
        private void LaunchLobby(MenuState m, int i)
        {
            statusBar.Text = "";
            statusBar.Active = false;
            RefreshServerLists(null, 0);
            Push(m);
        }
        private void Exit(MenuState m, int i)
        {
            Environment.Exit(0);
        }
        private void PlaySinglePlayer(MenuState m, int i)
        {
            GameState playState = new PlayState(this);
            Push(playState);
            Data.Network.SinglePlayer = true;
            Data.Network.Client = "";
            Data.Network.ServerIP = "";
            Data.Network.Server = "";
            Data.Network.Error = "";
            Data.Network.IdPlayer = 1;
            NetGame.Start("No");
        }
        private void JoinMultiPlay(MenuState m, int i)
        {
            statusBar.Text = "JOINING THE GAME";
            statusBar.Active = true;
            if (ipJoin.Value == "")
            {
                if (_scrollingSelection.SelectedItem >= 0 && _scrollingSelection.SelectedItem < _scrollingSelection.Entries.Count)
                {
                    Data.Config.currentMap = _scrollingSelection.Entries[_scrollingSelection.SelectedItem].Item2;
                    if (_scrollingSelection.SelectedItem != -1)
                    {
                        if (_searchingLAN)
                            Data.Network.LastIP = Client.ServerList[_scrollingSelection.SelectedItem].IP;
                        else
                            Data.Network.LastIP = SyncClient.InternetGames[_scrollingSelection.SelectedItem].IP;
                        //Data.Network.LastIP = "90.24.210.69";
                        Data.Network.SinglePlayer = false;
                        player2.Reset(Data.PlayerInfos.Name, Exodus.Player.avatarURL, Exodus.Player.rank, Exodus.Player.victories, Exodus.Player.defeats, true);
                        player1.Reset("", 0, 0, 0, false);
                        NetGame.Start("C");
                        Push(m);
                        statusBar.Active = true;
                        statusBar.Text = "WAITING";
                    }
                }
            }
            else
            {
                System.Net.IPAddress ip;
                if (System.Net.IPAddress.TryParse(ipJoin.Value, out ip))
                {
                    Data.Network.LastIP = ipJoin.Value;
                    Data.Network.SinglePlayer = false;
                    player2.Reset(Data.PlayerInfos.Name, Exodus.Player.avatarURL, Exodus.Player.rank, Exodus.Player.victories, Exodus.Player.defeats, true);
                    NetGame.Start("C");
                    Push(m);
                    statusBar.Active = true;
                    statusBar.Text = "WAITING FOR PLAYERS";
                }
                else
                {
                    statusBar.Active = false;
                    statusBar.Text = "NOT A VALID IP";
                }
            }
        }
        private void BeginGame(MenuState m, int i)
        {
            BeginGame();
        }
        private bool BeginGame()
        {
            GameState playState = new PlayState(this);
            if (Server.IsRunning)
                Server.RunGame();
            Client.RunGame();
            Push(playState);
            return true;
        }
        private void LaunchGame(MenuState m, int i)
        {
            if (Data.Network.ConnectedClients.Count > 1)
                Client.SendObject(new Network.Orders.LaunchGame());
        }
        private void Editor(MenuState m, int i)
        {
            GameState editorState = new MapEditorState(this);
            Push(editorState);

        }
        private void RefreshServerLists(MenuState m, int i)
        {
            if (_searchingLAN)
            {
                Client.RefreshLANServerList();
            }
            else
            {
                Client.RefreshInternetServerList();
            }
        }
        public GraphicsDevice GetGraphicDevice()
        {
            return GraphicsDevice;
        }
        private void SwitchSearchingMode(MenuState m, int i)
        {
            if (!_searchingLAN)
            {
                _gameSelectionButton.Text = "SEARCH INTERNET";
            }
            else
            {
                _gameSelectionButton.Text = "SEARCH LAN";
            }
            _gameSelectionButton.SetPosition();
            _searchingLAN = !_searchingLAN;
            RefreshServerLists(null, 0);
        }
        void ConnectionFormSubmit(MenuState m, int i)
        {
            _temp = m;
            new Thread(ConnectionFormSubmit).Start();
        }
        void ConnectionForm(MenuState m, int i)
        {
            System.Diagnostics.Process.Start("http://3ten.fr");
        }
        bool _isConnecting = false;
        MenuState _temp;
        private void ConnectionFormSubmit()
        {
            if (!_isConnecting)
            {
                _isConnecting = true;
                statusBar.Text = "Connecting";
                statusBar.Active = true;
                Data.PlayerInfos.InternetID = SyncClient.UserIsValid(login.Value, pass.Value);
                if (Player.ConnectionState == 1)
                {
                    Data.Config.Login = login.Value;
                    Data.Config.Pwd = pass.Value;
                    Data.SavePlayerConfig();
                    statusBar.Text = "loading";
                    try
                    {
                        PlayerInfos.Reset(
                            SyncClient.SendSQLRequest("SELECT `avatar` FROM `user` WHERE `id` = " + Data.PlayerInfos.InternetID)[0][0],
                            Int32.Parse(((string[][])SyncClient.SendSQLRequest("SELECT COUNT(*) FROM `user` WHERE `score` > (SELECT `score` FROM `user` WHERE `id` = " + Data.PlayerInfos.InternetID + ")"))[0][0]) + 1,
                            Int32.Parse(((string[][])SyncClient.SendSQLRequest("SELECT COUNT(*) FROM `game` WHERE `winnerID`=" + Data.PlayerInfos.InternetID))[0][0]),
                            Int32.Parse(((string[][])SyncClient.SendSQLRequest("SELECT COUNT(*) FROM `game` WHERE `winnerID`!=" + Data.PlayerInfos.InternetID + " AND (`P1ID`=" + Data.PlayerInfos.InternetID + " OR `P2ID`=" + Data.PlayerInfos.InternetID + ")"))[0][0])
                        );
                        Push(_temp);
                        statusBar.Text = "Welcome !";
                        statusBar.Active = false;
                    }
                    catch
                    {
                        Player.ConnectionState = 2;
                        statusBar.Text = "Restart the game";
                        statusBar.Active = false;
                    }
                }
                else
                {
                    statusBar.Text = "Connection Failed";
                    statusBar.Active = false;
                }
                _isConnecting = false;
            }
        }
        void SaveSettings(MenuState m, int i)
        {
            new Thread(SaveSettings).Start();
        }
        bool _isChangingSettings = false;
        private void SaveSettings()
        {
            if (!_isChangingSettings)
            {
                _isChangingSettings = true;
                bool pseudo = false,
                     pass = false;
                if (settings_login.Value != "" && settings_login.Value != Data.PlayerInfos.Name)
                {
                    statusBar.Active = true;
                    statusBar.Text = "CHECKING NEW PSEUDO";
                    if (settings_login.Value.Length > 12)
                        statusBar.Text = "TOO MANY LETTERS";
                    else
                    {
                        string[][] result = SyncClient.SendSQLRequest("SELECT id FROM `user` WHERE `name` = \"" + settings_login.Value + "\"");
                        if (result.Length == 0)
                        {
                            statusBar.Text = "CHANGING YOUR PSEUDO";
                            SyncClient.SendSQLOrder("UPDATE `user` SET `name` = \"" + settings_login.Value + "\" WHERE `name` = \"" + Data.PlayerInfos.Name + "\"");
                            statusBar.Text = "PSEUDO SAVED";
                            statusBar.Active = false;
                            Data.PlayerInfos.Name = settings_login.Value;
                            pseudo = true;
                            Thread.Sleep(100);
                        }
                    }
                }
                if (settings_pass1.Value != "")
                {
                    statusBar.Active = true;
                    statusBar.Text = "CHECKING NEW PWD";
                    if (settings_pass1.Value.Length < 5)
                        statusBar.Text = "PWD NEED AT LEAST 5 CHARS";
                    else if (settings_pass1.Value.Length > 24)
                        statusBar.Text = "PWD NEED LESS THAN 25 CHARS";
                    else if (settings_pass1.Value != settings_pass2.Value)
                        statusBar.Text = "PWD1 != PWD2";
                    else
                    {
                        statusBar.Text = "CHANGING PWD";
                        string newPass = Data.Security.SHA1(settings_pass1.Value);
                        SyncClient.SendSQLOrder("UPDATE `user` SET `password` = \"" + newPass + "\" WHERE `name` = \"" + Data.PlayerInfos.Name + "\"");
                        statusBar.Text = "PWD SAVED";
                        pass = true;
                    }
                    statusBar.Active = false;
                    Thread.Sleep(100);
                }
                if (pseudo && pass)
                    statusBar.Text = "INFOS SAVED";

                _isChangingSettings = false;
            }
        }
        void SaveSound(int i)
        {

            statusBar.Text = "SAVING SOUND";
            statusBar.Active = true;
            Data.Config.LevelSound = i;
            Data.SavePlayerConfig();
            statusBar.Text = "SOUND SAVED";
            statusBar.Active = false;
        }
        #endregion

        private MenuState BaseMenu(ParticleEngine.ParticleEngine particleMenu)
        {
            Texture2D t;
            MenuState baseMenuState = new MenuState(this);
            baseMenuState.Items.Add(new Background(Textures.Menu["MainBackground"]));
            baseMenuState.Items.Add(new GUI.Items.ParticleEngine(particleMenu));
            t = Textures.Menu["logo"];
            baseMenuState.Items.Add(new Passive(Textures.Menu["logo"], (Data.Window.WindowWidth - t.Width) / 2,
                                                Data.Window.ScreenCenter.Y / 2 - 177, 5 * Data.GameDisplaying.Epsilon));
            t = Textures.Menu["interface"];
            baseMenuState.Items.Add(new Passive(t, (Data.Window.WindowWidth - t.Width) / 2,
                                                Data.Window.ScreenCenter.Y - (t.Height / 2) + 128,
                                                5 * Data.GameDisplaying.Epsilon));
            return baseMenuState;
        }
        private MenuState BaseRedMenu(ParticleEngine.ParticleEngine particleMenu)
        {
            Texture2D t;
            MenuState baseMenuState = new MenuState(this);
            baseMenuState.Items.Add(new Background(Textures.Menu["BGLaunchGame"]));
            baseMenuState.Items.Add(new GUI.Items.ParticleEngine(particleMenu));
            t = Textures.Menu["logo"];
            baseMenuState.Items.Add(new Passive(t, (Data.Window.WindowWidth - t.Width) / 2,
                                                Data.Window.ScreenCenter.Y / 2 - 177, 5 * Data.GameDisplaying.Epsilon));
            t = Textures.Menu["UI"];
            baseMenuState.Items.Add(new Passive(t, (Data.Window.WindowWidth - t.Width) / 2,
                                                Data.Window.ScreenCenter.Y - (t.Height / 2) + 128,
                                                5 * Data.GameDisplaying.Epsilon));
            return baseMenuState;
        }
        private bool ResetObservers(string s1, string s2, string s3)
        {
            observer1.Txt = s1;
            observer2.Txt = s2;
            observer3.Txt = s3;
            observer1.Pos.X = (Data.Window.WindowWidth - GUI.Fonts.Eurostile12.MeasureString(s1).X) / 2;
            observer2.Pos.X = (Data.Window.WindowWidth - GUI.Fonts.Eurostile12.MeasureString(s2).X) / 2;
            observer3.Pos.X = (Data.Window.WindowWidth - GUI.Fonts.Eurostile12.MeasureString(s3).X) / 2;
            return true;
        }
        private MenuState LaunchingGameMenu(bool CanLaunch, ParticleEngine.ParticleEngine particleMenu)
        {
            MenuState gameLaunching = BaseRedMenu(particleMenu);
            Texture2D t = Textures.Menu["Observers"];
            gameLaunching.Items.Add(new Passive(t, (Data.Window.WindowWidth - t.Width) / 2, Data.Window.ScreenCenter.Y + 215, Data.GameDisplaying.Epsilon * 4));
            MenuButton m = new LaunchingOrangeButton("");
            m.DoClick = LaunchGame;
            MenuHorizontal launching = new MenuHorizontal(Data.Window.ScreenCenter.X - 118, Data.Window.ScreenCenter.Y + 180, 0);
            launching.Create(new List<Component> { m });
            if (observer1 == null)
                observer1 = new Label(GUI.Fonts.Eurostile12, "", Data.Window.ScreenCenter.X, Data.Window.ScreenCenter.Y + 231);
            if (observer2 == null)
                observer2 = new Label(GUI.Fonts.Eurostile12, "", Data.Window.ScreenCenter.X, Data.Window.ScreenCenter.Y + 272);
            if (observer3 == null)
                observer3 = new Label(GUI.Fonts.Eurostile12, "", Data.Window.ScreenCenter.X, Data.Window.ScreenCenter.Y + 313);
            if (Network.ClientSide.Client.resetObservers == null)
                Network.ClientSide.Client.resetObservers = ResetObservers;
            gameLaunching.Items.Add(new Container(new List<Component>
            {
                observer1, observer2, observer3
            }));
            if (CanLaunch)
                gameLaunching.Items.Add(launching);
            if (Client.RunGameFunc == null)
                Client.RunGameFunc = BeginGame;
            if (player1 == null)
            {
                player1 = new PlayerInfosLaunching(Data.Window.ScreenCenter.X - 385, Data.Window.ScreenCenter.Y, Data.GameDisplaying.Epsilon * 3, false);
                Client.player1 = player1;
            }
            if (player2 == null)
            {
                player2 = new PlayerInfosLaunching(Data.Window.ScreenCenter.X + 90, Data.Window.ScreenCenter.Y, Data.GameDisplaying.Epsilon * 3, true);
                Server.player2 = player2;
            }
            gameLaunching.Items.Add(player1);
            gameLaunching.Items.Add(player2);
            gameLaunching.Items.Add(statusBar);
            return gameLaunching;
        }
        private void LaunchGameMenu(MenuState m, int i)
        {
            Push(m);
            player1.Reset(Data.PlayerInfos.Name, Exodus.Player.avatarURL, Exodus.Player.rank, Exodus.Player.victories, Exodus.Player.defeats, true);
            player2.Reset("", 0, 0, 0, false);
            Data.Network.SinglePlayer = false;
            NetGame.Start("SC");
            statusBar.Active = true;
            statusBar.Text = "WAITING FOR PLAYERS";
        }
    }
}
