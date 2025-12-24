using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TribeBuild.Player;
using TribeBuild.World;

namespace TribeBuild.UI
{
    public enum UIScreen
    {
        MainMenu,
        InGame,
        Paused,
        Settings,
        DaySummary
    }
    
    /// <summary>
    /// ✅ FIXED: Modern Myra UI with Custom Progress Bars
    /// </summary>
    public class MyraUIManager
    {
        private Desktop desktop;
        
        // Color Palette
        static readonly Color BgDark = new Color(12, 12, 18);
        static readonly Color PanelDark = new Color(24, 24, 32);
        static readonly Color PanelLight = new Color(32, 32, 42);
        static readonly Color Accent = new Color(88, 166, 255);
        static readonly Color AccentDark = new Color(52, 120, 200);
        static readonly Color TextPrimary = new Color(240, 240, 245);
        static readonly Color TextSecondary = new Color(160, 160, 170);
        static readonly Color Success = new Color(80, 220, 100);
        static readonly Color Warning = new Color(255, 180, 60);
        static readonly Color Danger = new Color(255, 80, 80);
        
        // Screens
        private Panel mainMenuScreen;
        private Panel gameHUDScreen;
        private Panel pauseMenuScreen;
        private Panel settingsScreen;
        
        // HUD Components
        private RPGHudPanel rpgHud;
        private HotbarPanel hotbar;
        private InventoryPanel inventory;
        private Label timeLabel;
        private Label dayLabel;
        private Label woodLabel;
        private Label stoneLabel;
        private Label foodLabel;
        
        // State
        public UIScreen CurrentScreen { get; private set; }
        private KeyboardState previousKeyState;
        private bool showInventory = false;
        
        public MyraUIManager(Game game)
        {
            MyraEnvironment.Game = game;
            desktop = new Desktop();
            
            // Load font
            var fontSystem = new FontSystem();
            fontSystem.AddFont(File.ReadAllBytes("Content/Font/Baskic8.otf"));
            var defaultFont = fontSystem.GetFont(18);
            
            // Apply font
            Myra.Graphics2D.UI.Styles.Stylesheet.Current.LabelStyle.Font = defaultFont;
            Myra.Graphics2D.UI.Styles.Stylesheet.Current.TextBoxStyle.Font = defaultFont;
            Myra.Graphics2D.UI.Styles.Stylesheet.Current.CheckBoxStyle.LabelStyle.Font = defaultFont;
            
            CreateAllScreens();
            ShowMainMenu();
        }
        
        private void CreateAllScreens()
        {
            CreateMainMenuScreen();
            CreateGameHUDScreen();
            CreatePauseMenuScreen();
            CreateSettingsScreen();
        }
        
        // ==================== MAIN MENU ====================
        
        private void CreateMainMenuScreen()
        {
            var root = new Panel
            {
                Background = new SolidBrush(BgDark)
            };
            
            var container = new VerticalStackPanel
            {
                Spacing = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Title
            var titlePanel = new Panel
            {
                Width = 500,
                Height = 120,
                Background = new SolidBrush(PanelDark),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var title = new Label
            {
                Text = "TRIBE BUILD",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = Accent
            };
            title.Scale = new Vector2(2.5f);
            titlePanel.Widgets.Add(title);
            container.Widgets.Add(titlePanel);
            
            // Subtitle
            var subtitle = new Label
            {
                Text = "Build • Survive • Thrive",
                TextColor = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            subtitle.Scale = new Vector2(1.2f);
            container.Widgets.Add(subtitle);
            
            // Buttons
            var buttonPanel = new VerticalStackPanel
            {
                Spacing = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            buttonPanel.Widgets.Add(CreateModernButton("▶  NEW GAME", () =>
            {
                ShowGame();
                GameManager.Instance?.StartGame();
            }, Accent));
            
            buttonPanel.Widgets.Add(CreateModernButton("⚙  SETTINGS", ShowSettings, AccentDark));
            buttonPanel.Widgets.Add(CreateModernButton("✕  EXIT", () => Environment.Exit(0), Danger));
            
            container.Widgets.Add(buttonPanel);
            
            // Version
            var version = new Label
            {
                Text = "v1.0.0 Alpha",
                TextColor = TextSecondary * 0.6f,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 20, 20)
            };
            
            root.Widgets.Add(container);
            root.Widgets.Add(version);
            
            mainMenuScreen = root;
        }
        
        // ==================== GAME HUD ====================
        
        private void CreateGameHUDScreen()
        {
            var root = new Panel();
            
            // Top bar
            var topBar = CreateTopBar();
            topBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            topBar.VerticalAlignment = VerticalAlignment.Top;
            topBar.Margin = new Thickness(16);
            root.Widgets.Add(topBar);
            
            // Left - Player stats
            rpgHud = new RPGHudPanel();
            rpgHud.HorizontalAlignment = HorizontalAlignment.Left;
            rpgHud.VerticalAlignment = VerticalAlignment.Top;
            rpgHud.Margin = new Thickness(16, 100, 0, 0);
            root.Widgets.Add(rpgHud);
            
            // Right - Resources
            var resourcePanel = CreateResourcePanel();
            resourcePanel.HorizontalAlignment = HorizontalAlignment.Right;
            resourcePanel.VerticalAlignment = VerticalAlignment.Top;
            resourcePanel.Margin = new Thickness(0, 100, 16, 0);
            root.Widgets.Add(resourcePanel);
            
            // Bottom - Hotbar
            hotbar = new HotbarPanel();
            hotbar.HorizontalAlignment = HorizontalAlignment.Center;
            hotbar.VerticalAlignment = VerticalAlignment.Bottom;
            hotbar.Margin = new Thickness(0, 0, 0, 20);
            root.Widgets.Add(hotbar);
            
            // Center - Inventory
            inventory = new InventoryPanel();
            inventory.HorizontalAlignment = HorizontalAlignment.Center;
            inventory.VerticalAlignment = VerticalAlignment.Center;
            inventory.Visible = false;
            root.Widgets.Add(inventory);
            
            gameHUDScreen = root;
        }
        
        private Panel CreateTopBar()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(PanelDark, 200)),
                Padding = new Thickness(20, 12),
                Width = 400
            };
            
            var grid = new Grid { ColumnSpacing = 20 };
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            
            dayLabel = new Label
            {
                Text = "Day 1",
                TextColor = Accent
            };
            dayLabel.Scale = new Vector2(1.3f);
            Grid.SetColumn(dayLabel, 0);
            grid.Widgets.Add(dayLabel);
            
            timeLabel = new Label
            {
                Text = "06:00",
                TextColor = TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            timeLabel.Scale = new Vector2(1.3f);
            Grid.SetColumn(timeLabel, 1);
            grid.Widgets.Add(timeLabel);
            
            panel.Widgets.Add(grid);
            return panel;
        }
        
        private Panel CreateResourcePanel()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(PanelDark, 200)),
                Padding = new Thickness(16),
                Width = 220
            };
            
            var stack = new VerticalStackPanel { Spacing = 12 };
            
            var title = new Label
            {
                Text = "RESOURCES",
                TextColor = TextSecondary
            };
            title.Scale = new Vector2(0.9f);
            stack.Widgets.Add(title);
            
            woodLabel = new Label { Text = "🌲 0", TextColor = TextPrimary };

            woodLabel.Scale = new Vector2(1.1f);
            stack.Widgets.Add(woodLabel);
            
            // stoneLabel = new Label { Text = "🪨 0", TextColor = TextPrimary };
            // stoneLabel.Scale = new Vector2(1.1f);
            // stack.Widgets.Add(stoneLabel);
            
            foodLabel = new Label { Text = "🍖 0", TextColor = TextPrimary };
            foodLabel.Scale = new Vector2(1.1f);
            stack.Widgets.Add(foodLabel);
            
            panel.Widgets.Add(stack);
            return panel;
        }

         public void UpdateResource(PlayerCharacter player)
            {
                if (player == null) return;
                
                woodLabel.Text = $"  🌲: {player.Inventory.GetItemCount("wood")} ";
                foodLabel.Text = $"  🍖 : {player.Inventory.GetItemCount("food")}";
                
            }

        
        // ==================== CUSTOM PROGRESS BAR ====================
        
        private class CustomProgressBar : Panel
        {
            private Panel fillPanel;
            private int barWidth;
            private float maxValue = 100f;
            private float currentValue = 100f;
            private Color fillColor;
            
            public CustomProgressBar(int width, int height, Color color)
            {
                barWidth = width;
                Width = width;
                Height = height;
                fillColor = color;
                
                // Background
                Background = new SolidBrush(Color.Black * 0.5f);
                
                // Fill
                fillPanel = new Panel
                {
                    Width = width,
                    Height = height,
                    Background = new SolidBrush(fillColor)
                };
                
                Widgets.Add(fillPanel);
            }
            
            public void SetValue(float value, float max)
            {
                maxValue = max;
                currentValue = value;
                
                float percent = Math.Max(0f, Math.Min(1f, value / max));
                fillPanel.Width = (int)(barWidth * percent);
                
                // Color feedback
                if (percent > 0.5f)
                    fillPanel.Background = new SolidBrush(fillColor);
                else if (percent > 0.25f)
                    fillPanel.Background = new SolidBrush(Warning);
                else
                    fillPanel.Background = new SolidBrush(Danger);
            }
        }
        
        // ==================== RPG HUD ====================
        
        private class RPGHudPanel : Panel
        {
            private Label healthText;
            private Label staminaText;
            private CustomProgressBar healthBar;
            private CustomProgressBar staminaBar;
            private const int BAR_WIDTH = 250;
            private const int BAR_HEIGHT = 24;
            
            public RPGHudPanel()
            {
                Background = new SolidBrush(new Color(PanelDark, 200));
                Padding = new Thickness(16);
                Width = BAR_WIDTH + 32;
                
                var stack = new VerticalStackPanel { Spacing = 16 };
                
                var title = new Label
                {
                    Text = "PLAYER",
                    TextColor = TextSecondary
                };
                title.Scale = new Vector2(0.9f);
                stack.Widgets.Add(title);
                
                // Health
                var healthContainer = new VerticalStackPanel { Spacing = 4 };
                healthText = new Label
                {
                    Text = "HP: 100 / 100",
                    TextColor = TextPrimary
                };
                healthContainer.Widgets.Add(healthText);
                
                healthBar = new CustomProgressBar(BAR_WIDTH, BAR_HEIGHT, Success);
                healthContainer.Widgets.Add(healthBar);
                stack.Widgets.Add(healthContainer);
                
                // Stamina
                var staminaContainer = new VerticalStackPanel { Spacing = 4 };
                staminaText = new Label
                {
                    Text = "Stamina: 100 / 100",
                    TextColor = TextPrimary
                };
                staminaContainer.Widgets.Add(staminaText);
                
                staminaBar = new CustomProgressBar(BAR_WIDTH, BAR_HEIGHT, Accent);
                staminaContainer.Widgets.Add(staminaBar);
                stack.Widgets.Add(staminaContainer);
                
                Widgets.Add(stack);
            }
            
            public void UpdatePlayerStats(PlayerCharacter player)
            {
                if (player == null) return;
                
                healthText.Text = $"HP: {(int)player.Health} / {(int)player.MaxHealth}";
                staminaText.Text = $"Stamina: {(int)player.Stamina} / {(int)player.MaxStamina}";
                
                healthBar.SetValue(player.Health, player.MaxHealth);
                staminaBar.SetValue(player.Stamina, player.MaxStamina);
            }
        }
        
        // ==================== HOTBAR ====================
        
        private class HotbarPanel : Panel
        {
            private Label[] slotLabels;
            private Panel[] slotPanels;
            private const int SLOT_SIZE = 64;
            private const int SLOT_COUNT = 9;
            
            public HotbarPanel()
            {
                Background = new SolidBrush(new Color(PanelDark, 220));
                Padding = new Thickness(12);
                Width = (SLOT_SIZE + 8) * SLOT_COUNT + 24;
                Height = SLOT_SIZE + 48;
                
                var stack = new VerticalStackPanel { Spacing = 8 };
                
                var title = new Label
                {
                    Text = "HOTBAR",
                    TextColor = TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                title.Scale = new Vector2(0.8f);
                stack.Widgets.Add(title);
                
                var slotsGrid = new HorizontalStackPanel { Spacing = 8 };
                
                slotLabels = new Label[SLOT_COUNT];
                slotPanels = new Panel[SLOT_COUNT];
                
                for (int i = 0; i < SLOT_COUNT; i++)
                {
                    var slotPanel = new Panel
                    {
                        Width = SLOT_SIZE,
                        Height = SLOT_SIZE,
                        Background = new SolidBrush(new Color(PanelLight, 180))
                    };
                    
                    var slotStack = new VerticalStackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    var numberLabel = new Label
                    {
                        Text = (i + 1).ToString(),
                        TextColor = TextSecondary,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    numberLabel.Scale = new Vector2(0.8f);
                    slotStack.Widgets.Add(numberLabel);
                    
                    var itemLabel = new Label
                    {
                        Text = "",
                        TextColor = TextPrimary,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    slotStack.Widgets.Add(itemLabel);
                    
                    slotPanel.Widgets.Add(slotStack);
                    slotsGrid.Widgets.Add(slotPanel);
                    
                    slotLabels[i] = itemLabel;
                    slotPanels[i] = slotPanel;
                }
                
                stack.Widgets.Add(slotsGrid);
                Widgets.Add(stack);
            }
            
            public void UpdateHotbar(PlayerCharacter player)
            {
                if (player?.Equipment == null) return;
                
                var hotbar = player.Equipment.GetHotbar();
                int currentSlot = player.Equipment.CurrentSlot;
                
                for (int i = 0; i < SLOT_COUNT; i++)
                {
                    int slot = i + 1;
                    
                    if (hotbar.ContainsKey(slot))
                    {
                        var tool = hotbar[slot];
                        slotLabels[i].Text = GetToolIcon(tool.Type);
                        slotLabels[i].Scale = new Vector2(1.5f);
                    }
                    else
                    {
                        slotLabels[i].Text = "—";
                        slotLabels[i].Scale = new Vector2(1f);
                    }
                    
                    if (slot == currentSlot)
                    {
                        slotPanels[i].Background = new SolidBrush(Accent * 0.6f);
                    }
                    else
                    {
                        slotPanels[i].Background = new SolidBrush(new Color(PanelLight, 180));
                    }
                }
            }
            
            private string GetToolIcon(ToolType type)
            {
                return type switch
                {
                    ToolType.Axe => "🪓",
                    ToolType.Pickaxe => "⛏️",
                    ToolType.Sword => "⚔️",
                    ToolType.Hoe => "🔨",
                    _ => "✋"
                };
            }
        }
        
        // ==================== INVENTORY ====================
        
        private class InventoryPanel : Panel
        {
            public InventoryPanel()
            {
                Background = new SolidBrush(new Color(0, 0, 0, 200));
                Width = 500;
                Height = 400;
                
                var contentPanel = new Panel
                {
                    Background = new SolidBrush(PanelDark),
                    Padding = new Thickness(20),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var stack = new VerticalStackPanel { Spacing = 16 };
                
                var title = new Label
                {
                    Text = "INVENTORY",
                    TextColor = Accent,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                title.Scale = new Vector2(1.5f);
                stack.Widgets.Add(title);
                
                var hint = new Label
                {
                    Text = "Press TAB to close",
                    TextColor = TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                hint.Scale = new Vector2(0.9f);
                stack.Widgets.Add(hint);
                
                contentPanel.Widgets.Add(stack);
                Widgets.Add(contentPanel);
            }
            
            public void UpdateInventory(PlayerCharacter player)
            {
                // TODO: Display inventory items
            }
        }
        
        // ==================== PAUSE & SETTINGS ====================
        
        private void CreatePauseMenuScreen()
        {
            var overlay = new Panel
            {
                Background = new SolidBrush(new Color(0, 0, 0, 220))
            };
            
            var box = new Panel
            {
                Background = new SolidBrush(PanelDark),
                Padding = new Thickness(32),
                Width = 400,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var stack = new VerticalStackPanel { Spacing = 24 };
            
            var title = new Label
            {
                Text = "PAUSED",
                HorizontalAlignment = HorizontalAlignment.Center,
                TextColor = Accent
            };
            title.Scale = new Vector2(2f);
            stack.Widgets.Add(title);
            
            var divider = new Panel
            {
                Height = 2,
                Background = new SolidBrush(TextSecondary * 0.3f)
            };
            stack.Widgets.Add(divider);
            
            stack.Widgets.Add(CreateModernButton("▶  RESUME", () =>
            {
                ShowGame();
                GameManager.Instance?.ResumeGame();
            }, Success));
            
            stack.Widgets.Add(CreateModernButton("⚙  SETTINGS", ShowSettings, AccentDark));
            stack.Widgets.Add(CreateModernButton("⌂  MAIN MENU", ShowMainMenu, Warning));
            
            box.Widgets.Add(stack);
            overlay.Widgets.Add(box);
            
            pauseMenuScreen = overlay;
        }
        
        private void CreateSettingsScreen()
        {
            var overlay = new Panel
            {
                Background = new SolidBrush(new Color(0, 0, 0, 220))
            };
            
            var box = new Panel
            {
                Background = new SolidBrush(PanelDark),
                Padding = new Thickness(32),
                Width = 500,
                Height = 400,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var stack = new VerticalStackPanel { Spacing = 20 };
            
            var title = new Label
            {
                Text = "SETTINGS",
                HorizontalAlignment = HorizontalAlignment.Center,
                TextColor = Accent
            };
            title.Scale = new Vector2(1.8f);
            stack.Widgets.Add(title);
            
            var grid = new Grid
            {
                RowSpacing = 16,
                ColumnSpacing = 16
            };
            
            for (int i = 0; i < 3; i++)
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            
            // Volume
            var volumeLabel = new Label
            {
                Text = "Master Volume:",
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = TextPrimary
            };
            Grid.SetRow(volumeLabel, 0);
            Grid.SetColumn(volumeLabel, 0);
            grid.Widgets.Add(volumeLabel);
            
            var volumeSlider = new HorizontalSlider
            {
                Width = 200,
                Minimum = 0,
                Maximum = 100,
                Value = 75
            };
            Grid.SetRow(volumeSlider, 0);
            Grid.SetColumn(volumeSlider, 1);
            grid.Widgets.Add(volumeSlider);
            
            // Fullscreen
            var fullscreenLabel = new Label
            {
                Text = "Fullscreen:",
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = TextPrimary
            };
            Grid.SetRow(fullscreenLabel, 1);
            Grid.SetColumn(fullscreenLabel, 0);
            grid.Widgets.Add(fullscreenLabel);
            
            var fullscreenCheck = new CheckBox { IsChecked = false };
            Grid.SetRow(fullscreenCheck, 1);
            Grid.SetColumn(fullscreenCheck, 1);
            grid.Widgets.Add(fullscreenCheck);
            
            // VSync
            var vsyncLabel = new Label
            {
                Text = "VSync:",
                VerticalAlignment = VerticalAlignment.Center,
                TextColor = TextPrimary
            };
            Grid.SetRow(vsyncLabel, 2);
            Grid.SetColumn(vsyncLabel, 0);
            grid.Widgets.Add(vsyncLabel);
            
            var vsyncCheck = new CheckBox { IsChecked = true };
            Grid.SetRow(vsyncCheck, 2);
            Grid.SetColumn(vsyncCheck, 1);
            grid.Widgets.Add(vsyncCheck);
            
            stack.Widgets.Add(grid);
            stack.Widgets.Add(new Panel { Height = 20 });
            stack.Widgets.Add(CreateModernButton("← BACK", ShowMainMenu, AccentDark));
            
            box.Widgets.Add(stack);
            overlay.Widgets.Add(box);
            
            settingsScreen = overlay;
        }
        
        // ==================== HELPERS ====================
        
        private TextButton CreateModernButton(string text, Action onClick, Color color)
        {
            var btn = new TextButton
            {
                Text = text,
                Width = 320,
                Height = 56,
                Background = new SolidBrush(color),
                OverBackground = new SolidBrush(color * 1.2f),
                PressedBackground = new SolidBrush(color * 0.8f),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(16)
            };
            
            btn.Click += (_, __) => onClick();
            return btn;
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
            var keyState = Keyboard.GetState();
            
            // ESC - Pause
            if (keyState.IsKeyDown(Keys.Escape) && !previousKeyState.IsKeyDown(Keys.Escape))
            {
                if (CurrentScreen == UIScreen.InGame)
                {
                    if (showInventory)
                    {
                        showInventory = false;
                        inventory.Visible = false;
                    }
                    else
                    {
                        GameManager.Instance?.PauseGame();
                        ShowPause();
                    }
                }
                else if (CurrentScreen == UIScreen.Paused)
                {
                    GameManager.Instance?.ResumeGame();
                    ShowGame();
                }
            }
            
            // TAB - Inventory
            if (CurrentScreen == UIScreen.InGame)
            {
                if (keyState.IsKeyDown(Keys.Tab) && !previousKeyState.IsKeyDown(Keys.Tab))
                {
                    showInventory = !showInventory;
                    inventory.Visible = showInventory;
                }
                
                UpdateGameHUD();
            }
            
            previousKeyState = keyState;
        }
        
        private void UpdateGameHUD()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            
            dayLabel.Text = $"Day {gm.CurrentDay}";
            timeLabel.Text = gm.GetTimeString();
            
            var phase = gm.DayNightCycle.CurrentPhase;
            timeLabel.TextColor = phase switch
            {
                DayNightCycleManager.TimeOfDayPhase.Night => Danger,
                DayNightCycleManager.TimeOfDayPhase.Evening => Warning,
                _ => TextPrimary
            };
            
            var player = gm.World.GetPlayerCharacter;
            if (player != null)
            {
                rpgHud.UpdatePlayerStats(player);
                hotbar.UpdateHotbar(player);
                UpdateResource(player);
                
                if (showInventory)
                {
                    inventory.UpdateInventory(player);
                }
            }
        }
        
        public void Draw()
        {
            desktop.Render();
        }
    }
}