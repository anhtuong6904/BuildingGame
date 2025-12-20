using System;
using System.IO;
using Cyotek.Drawing.BitmapFont;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using TribeBuild;
using TribeBuild.Tasks;

namespace TribeBuild.UI
{
    public enum UIScreen
    {
        MainMenu,
        InGame,
        Paused,
        Settings
    }
    
    /// <summary>
    /// Myra UI Manager v1.5.10
    /// </summary>
    public class MyraUIManager
    {
        private Desktop desktop;

        static readonly Color BgDark = new(18, 18, 24);
        static readonly Color PanelDark = new(28, 28, 36);
        static readonly Color Accent = new(80, 180, 255);
        static readonly Color TextSoft = new(220, 220, 230);


        // Screens
        private FontAtlas fontAtlas;
        private Panel mainMenuScreen;
        private Panel gameHUDScreen;
        private Panel pauseMenuScreen;
        private Panel settingsScreen;
        
        // HUD elements
        private Label timeLabel;
        private Label resourceLabel;
        private Label taskLabel;
        
        public UIScreen CurrentScreen { get; private set; }
        
        public MyraUIManager(Game game)
        {
            MyraEnvironment.Game = game;
            desktop = new Desktop();
            
                    // Tạo FontSystem để quản lý font vector
            var fontSystem = new FontSystem();
            // Nạp trực tiếp từ file gốc (Baskic8.otf hoặc Baskic8.ttf)
            fontSystem.AddFont(File.ReadAllBytes("Content/Font/Baskic8.otf")); 

            // Lấy font với kích thước mong muốn (ví dụ: 18px)
            var _defaultFont = fontSystem.GetFont(18);


            Stylesheet.Current.LabelStyle.Font = _defaultFont;

            Stylesheet.Current.TextBoxStyle.Font = _defaultFont;

            Stylesheet.Current.CheckBoxStyle.LabelStyle.Font = _defaultFont;

            

            // Tạo các màn hình (Lúc này Myra đã biết font mặc định là gì rồi)
            CreateMainMenuScreen();
            CreateGameHUDScreen();
            CreatePauseMenuScreen();
            CreateSettingsScreen();

            ShowMainMenu();
        }
        

        // ==================== MAIN MENU ====================
        
        private void CreateMainMenuScreen()
        {
            var container = new Panel
            {
                Background = new SolidBrush(new Color(15, 15, 20))
            };

            var grid = new Grid
            {
                RowSpacing = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 4; i++)
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var title = new Label
            {
                Text = "TRIBE BUILD",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.Scale = new Vector2(2f, 2f);
            Grid.SetRow(title, 0);
            grid.Widgets.Add(title);

            TextButton MakeButton(string text, Action onClick)
            {
                var btn = new TextButton
                {
                    Text = text,
                    Width = 260,
                    Height = 52,
                    Background = new SolidBrush(PanelDark),
                    OverBackground = new SolidBrush(new Color(50, 50, 70)),
                    PressedBackground = new SolidBrush(Accent),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                btn.Click += (_, __) => onClick();
                return btn;
            }


            var play = MakeButton("PLAY", () =>
            {
                ShowGame();
                GameManager.Instance?.StartGame();
            });
            Grid.SetRow(play, 1);
            grid.Widgets.Add(play);

            var settings = MakeButton("SETTINGS", ShowSettings);
            Grid.SetRow(settings, 2);
            grid.Widgets.Add(settings);

            var exit = MakeButton("EXIT", () => Environment.Exit(0));
            Grid.SetRow(exit, 3);
            grid.Widgets.Add(exit);

            container.Widgets.Add(grid);
            mainMenuScreen = container;
        }
        
        // ==================== GAME HUD ====================
        private void CreateGameHUDScreen()
        {
            var root = new HorizontalStackPanel
            {
                Margin = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = 10
            };

            

            timeLabel = new Label { Text = "Day 1 • 06:00" };

            resourceLabel = new Label { Text = "🌲 0   🪨 0   🍖 0" };


            taskLabel = new Label { Text = "Tasks: 0 / 0" };

            

            root.Widgets.Add(MakeHudCard("Time", timeLabel));
            root.Widgets.Add(MakeHudCard("Resources", resourceLabel));
            root.Widgets.Add(MakeHudCard("Tasks", taskLabel));

            gameHUDScreen = new Panel();
            gameHUDScreen.Widgets.Add(root);
        }

        private Panel MakeHudCard(string title, Widget content)
        {
            content.Scale = new Vector2(1.05f);

            var panel = new Panel
            {
                Background = new SolidBrush(new Color(20, 20, 28, 210)),
                Padding = new Thickness(12),
                Width = 220
            };

            var stack = new VerticalStackPanel { Spacing = 6 };

            stack.Widgets.Add(new Label
            {
                Text = title.ToUpper(),
                TextColor = Accent,
                Scale = new Vector2(0.9f),
                Opacity = 0.85f
            });

            stack.Widgets.Add(content);
            panel.Widgets.Add(stack);

            return panel;
        }



        
        // ==================== PAUSE MENU ====================
        
        private void CreatePauseMenuScreen()
        {
            var overlay = new Panel
            {
                Background = new SolidBrush(new Color(0, 0, 0, 200))
            };

           var box = new Panel
            {
                Background = new SolidBrush(PanelDark),
                Padding = new Thickness(28),
                Width = 320,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };



            var grid = new Grid { RowSpacing = 12 };

            for (int i = 0; i < 3; i++)
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var title = new Label
            {
                Text = "PAUSED",
                HorizontalAlignment = HorizontalAlignment.Center,
                Scale = new Vector2(1.5f)
            };

            var resume = new TextButton { Text = "RESUME", Width = 220, Height = 44 };
            resume.Click += (_, __) =>
            {
                ShowGame();
                GameManager.Instance?.ResumeGame();
            };

            var menu = new TextButton { Text = "MAIN MENU", Width = 220, Height = 44 };
            menu.Click += (_, __) => ShowMainMenu();

            Grid.SetRow(title, 0);
            Grid.SetRow(resume, 1);
            Grid.SetRow(menu, 2);

            grid.Widgets.Add(title);
            grid.Widgets.Add(resume);
            grid.Widgets.Add(menu);

            box.Widgets.Add(grid);
            overlay.Widgets.Add(box);

            pauseMenuScreen = overlay;
        }

        
        // ==================== SETTINGS ====================
        
        private void CreateSettingsScreen()
        {
            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8
            };
            
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            
            // Title
            var title = new Label
            {
                Text = "SETTINGS",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(title, 0);
            Grid.SetColumnSpan(title, 2);
            grid.Widgets.Add(title);

            settingsScreen = new Panel
            {
                Background = new SolidBrush(BgDark),
                Padding = new Thickness(40)
            };
            settingsScreen.Widgets.Add(grid);

            
            // Volume Label
            var volumeLabel = new Label
            {
                Text = "Volume:",
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(volumeLabel, 1);
            Grid.SetColumn(volumeLabel, 0);
            grid.Widgets.Add(volumeLabel);
            
            // Volume Slider
            var volumeSlider = new HorizontalSlider
            {
                Width = 300,
                Minimum = 0,
                Maximum = 100,
                Value = 50
            };
            Grid.SetRow(volumeSlider, 1);
            Grid.SetColumn(volumeSlider, 1);
            grid.Widgets.Add(volumeSlider);
            
            // Fullscreen Label
            var fullscreenLabel = new Label
            {
                Text = "Fullscreen:",
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(fullscreenLabel, 2);
            Grid.SetColumn(fullscreenLabel, 0);
            grid.Widgets.Add(fullscreenLabel);
            
            // Fullscreen Checkbox
            var fullscreenCheckbox = new CheckBox
            {
                IsChecked = false
            };
            Grid.SetRow(fullscreenCheckbox, 2);
            Grid.SetColumn(fullscreenCheckbox, 1);
            grid.Widgets.Add(fullscreenCheckbox);
            
            // Back Button
            var backButton = new TextButton
            {
                Text = "BACK",
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            backButton.Click += (s, e) => ShowMainMenu();
            Grid.SetRow(backButton, 3);
            Grid.SetColumnSpan(backButton, 2);
            grid.Widgets.Add(backButton);
            
            settingsScreen = new Panel();
            settingsScreen.Widgets.Add(grid);

            var box = new Panel
            {
                Background = new SolidBrush(PanelDark),
                Padding = new Thickness(24),
                Width = 420,
                Height = 420,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            box.Widgets.Add(grid);
            settingsScreen.Widgets.Add(box);


        }
        
        // ==================== SCREEN MANAGEMENT ====================
        
        public void ShowMainMenu()
        {
            desktop.Root = mainMenuScreen;
            CurrentScreen = UIScreen.MainMenu;
        }
        
        public void ShowGame()
        {
            desktop.Root = gameHUDScreen;
            CurrentScreen = UIScreen.InGame;
        }
        
        public void ShowPause()
        {
            desktop.Root = pauseMenuScreen;
            CurrentScreen = UIScreen.Paused;
        }
        
        public void ShowSettings()
        {
            desktop.Root = settingsScreen;
            CurrentScreen = UIScreen.Settings;
        }
        
        // ==================== UPDATE & DRAW ====================
        
        public void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
    {
                if (CurrentScreen == UIScreen.InGame)
                    ShowPause();
                else if (CurrentScreen == UIScreen.Paused)
                    ShowGame();
            }

            if (CurrentScreen == UIScreen.InGame)
                UpdateGameHUD();
        }
        
        private void UpdateGameHUD()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            
            // Update time
            timeLabel.Text = $"Day {gm.CurrentDay} - {gm.GetTimeString()}";
            
            // Update resources
           resourceLabel.Text =
                  $"🌲 {gm.WoodCollected}   🪨 {gm.StoneCollected}   🍖 {gm.FoodCollected}";


            
            // Update tasks
            int pending = TaskManager.Instance.GetTaskCount(TaskStatus.Pending);
            int active = TaskManager.Instance.GetTaskCount(TaskStatus.InProgress);
            taskLabel.Text = $"Tasks: {active} active | {pending} pending";
        }
        
        public void Draw()
        {
            desktop.Render();
        }
    }
}