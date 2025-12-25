using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace TribeBuild.UI
{
    /// <summary>
    /// ✅ Comprehensive settings system with save/load functionality
    /// </summary>
    public class SettingsManager
    {
        private static SettingsManager instance;
        public static SettingsManager Instance => instance ??= new SettingsManager();

        private const string SETTINGS_FILE = "settings.json";

        // Graphics Settings
        public int ScreenWidth { get; set; } = 1920;
        public int ScreenHeight { get; set; } = 1080;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = true;
        public bool AntiAliasing { get; set; } = false;
        
        // Audio Settings
        public float MasterVolume { get; set; } = 0.75f;
        public float MusicVolume { get; set; } = 0.5f;
        public float SFXVolume { get; set; } = 0.7f;
        public bool MuteWhenInactive { get; set; } = true;
        
        // Gameplay Settings
        public float GameSpeed { get; set; } = 1.0f;
        public bool AutoPause { get; set; } = true;
        public bool ShowTutorial { get; set; } = true;
        public bool AutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 5; // minutes
        
        // UI Settings
        public bool ShowFPS { get; set; } = true;
        public bool ShowMinimap { get; set; } = true;
        public bool ShowResourceBar { get; set; } = true;
        public float UIScale { get; set; } = 1.0f;
        
        // Debug Settings
        public bool EnableProfiling { get; set; } = false;
        public bool ShowDebugInfo { get; set; } = false;
        public bool LogToFile { get; set; } = false;
        
        // Input Settings
        public float MouseSensitivity { get; set; } = 1.0f;
        public float CameraScrollSpeed { get; set; } = 1.0f;
        public bool InvertCameraY { get; set; } = false;

        // Events
        public event Action OnSettingsChanged;

        private SettingsManager()
        {
            Load();
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SETTINGS_FILE, json);

                Console.WriteLine($"[SettingsManager] ✅ Settings saved to {SETTINGS_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsManager] ❌ ERROR saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    string json = File.ReadAllText(SETTINGS_FILE);
                    var loaded = JsonSerializer.Deserialize<SettingsManager>(json);

                    if (loaded != null)
                    {
                        CopyFrom(loaded);
                        Console.WriteLine($"[SettingsManager] ✅ Settings loaded from {SETTINGS_FILE}");
                    }
                }
                else
                {
                    Console.WriteLine($"[SettingsManager] No settings file found, using defaults");
                    Save(); // Save defaults
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsManager] ⚠️ ERROR loading settings: {ex.Message}");
                Console.WriteLine("[SettingsManager] Using default settings");
            }
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            ScreenWidth = 1920;
            ScreenHeight = 1080;
            Fullscreen = false;
            VSync = true;
            AntiAliasing = false;

            MasterVolume = 0.75f;
            MusicVolume = 0.5f;
            SFXVolume = 0.7f;
            MuteWhenInactive = true;

            GameSpeed = 1.0f;
            AutoPause = true;
            ShowTutorial = true;
            AutoSave = true;
            AutoSaveInterval = 5;

            ShowFPS = true;
            ShowMinimap = true;
            ShowResourceBar = true;
            UIScale = 1.0f;

            EnableProfiling = false;
            ShowDebugInfo = false;
            LogToFile = false;

            MouseSensitivity = 1.0f;
            CameraScrollSpeed = 1.0f;
            InvertCameraY = false;

            OnSettingsChanged?.Invoke();
            Console.WriteLine("[SettingsManager] ✅ Reset to defaults");
        }

        /// <summary>
        /// Apply settings changes
        /// </summary>
        public void Apply()
        {
            OnSettingsChanged?.Invoke();
            Save();
            Console.WriteLine("[SettingsManager] ✅ Settings applied");
        }

        private void CopyFrom(SettingsManager other)
        {
            ScreenWidth = other.ScreenWidth;
            ScreenHeight = other.ScreenHeight;
            Fullscreen = other.Fullscreen;
            VSync = other.VSync;
            AntiAliasing = other.AntiAliasing;

            MasterVolume = other.MasterVolume;
            MusicVolume = other.MusicVolume;
            SFXVolume = other.SFXVolume;
            MuteWhenInactive = other.MuteWhenInactive;

            GameSpeed = other.GameSpeed;
            AutoPause = other.AutoPause;
            ShowTutorial = other.ShowTutorial;
            AutoSave = other.AutoSave;
            AutoSaveInterval = other.AutoSaveInterval;

            ShowFPS = other.ShowFPS;
            ShowMinimap = other.ShowMinimap;
            ShowResourceBar = other.ShowResourceBar;
            UIScale = other.UIScale;

            EnableProfiling = other.EnableProfiling;
            ShowDebugInfo = other.ShowDebugInfo;
            LogToFile = other.LogToFile;

            MouseSensitivity = other.MouseSensitivity;
            CameraScrollSpeed = other.CameraScrollSpeed;
            InvertCameraY = other.InvertCameraY;
        }

        /// <summary>
        /// Get resolution presets
        /// </summary>
        public static (int width, int height)[] GetResolutionPresets()
        {
            return new[]
            {
                (1280, 720),   // HD
                (1600, 900),   // HD+
                (1920, 1080),  // Full HD
                (2560, 1440),  // QHD
                (3840, 2160)   // 4K
            };
        }

        /// <summary>
        /// Validate settings
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            // Clamp volumes
            MasterVolume = MathHelper.Clamp(MasterVolume, 0f, 1f);
            MusicVolume = MathHelper.Clamp(MusicVolume, 0f, 1f);
            SFXVolume = MathHelper.Clamp(SFXVolume, 0f, 1f);

            // Validate resolution
            if (ScreenWidth < 800 || ScreenHeight < 600)
            {
                Console.WriteLine("[SettingsManager] ⚠️ Invalid resolution, resetting to 1920x1080");
                ScreenWidth = 1920;
                ScreenHeight = 1080;
                valid = false;
            }

            // Clamp game speed
            GameSpeed = MathHelper.Clamp(GameSpeed, 0.25f, 4.0f);

            // Clamp UI scale
            UIScale = MathHelper.Clamp(UIScale, 0.5f, 2.0f);

            // Clamp sensitivities
            MouseSensitivity = MathHelper.Clamp(MouseSensitivity, 0.1f, 5.0f);
            CameraScrollSpeed = MathHelper.Clamp(CameraScrollSpeed, 0.1f, 5.0f);

            return valid;
        }

        /// <summary>
        /// Export settings to string
        /// </summary>
        public string ExportToString()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                return JsonSerializer.Serialize(this, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsManager] ❌ ERROR exporting settings: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Import settings from string
        /// </summary>
        public bool ImportFromString(string json)
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<SettingsManager>(json);
                if (loaded != null)
                {
                    CopyFrom(loaded);
                    Validate();
                    Apply();
                    Console.WriteLine("[SettingsManager] ✅ Settings imported successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsManager] ❌ ERROR importing settings: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Get settings summary
        /// </summary>
        public string GetSummary()
        {
            return $@"
=== SETTINGS SUMMARY ===
Graphics:
  Resolution: {ScreenWidth}x{ScreenHeight} {(Fullscreen ? "(Fullscreen)" : "(Windowed)")}
  VSync: {(VSync ? "ON" : "OFF")}
  Anti-Aliasing: {(AntiAliasing ? "ON" : "OFF")}

Audio:
  Master: {MasterVolume * 100:F0}%
  Music: {MusicVolume * 100:F0}%
  SFX: {SFXVolume * 100:F0}%

Gameplay:
  Game Speed: {GameSpeed:F2}x
  Auto-Pause: {(AutoPause ? "ON" : "OFF")}
  Auto-Save: {(AutoSave ? $"Every {AutoSaveInterval}min" : "OFF")}

UI:
  Show FPS: {(ShowFPS ? "ON" : "OFF")}
  Show Minimap: {(ShowMinimap ? "ON" : "OFF")}
  UI Scale: {UIScale:F2}x

Debug:
  Profiling: {(EnableProfiling ? "ON" : "OFF")}
  Debug Info: {(ShowDebugInfo ? "ON" : "OFF")}
========================
";
        }
    }
}