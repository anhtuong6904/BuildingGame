// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Runtime.CompilerServices;
// using System.Text;
// using Microsoft.Xna.Framework;
// using MonoGameLibrary;

// namespace TribeBuild.Logging
// {

//     public enum LogLevel
//     {
//         Debug,
//         Info,
//         Warning,
//         Error,
//         GameEvent
//     }
//     public class GameLogger
//     {
//         //singleton pattern (design pattern dung de dam bao chi duoc khoi tao duy nhat 1 lan)
//         public static GameLogger Instance {get; private set;}

//         //setting
//         public bool EnableConsoleOutput{get; set;}
//         public bool EnableFileOutput{get; set;}
//         public bool EnableInGameLog{get; set;}
//         public LogLevel MinimumLogLevel {get; set;}

//         //file logging
//         private string logFilePath;
//         private StreamWriter logFileWriter;

//         private Queue<LogEntry> inGameLog;
//         private int maxInGameLogSize = 50;

//         private DateTime sessionStartTime;
//         private int frameCount;

//         public GameLogger()
//         {
//             Instance = this;

//             EnableConsoleOutput = true;
//             EnableFileOutput = true;
//             EnableInGameLog = true;
//             MinimumLogLevel = LogLevel.Debug;

//             inGameLog = new Queue<LogEntry>();
//             sessionStartTime = DateTime.Now;
//         }

//         private void InitializeFileLogging()
//         {
//             try
//             {
//                 string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"logs");
//                 Directory.CreateDirectory(logDir);
//                 string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
//                 logFilePath = Path.Combine(logDir, $"game_log_{timestamp}.txt");
//                 logFileWriter = new StreamWriter(logFilePath, false, Encoding.UTF8);
//                 logFileWriter.AutoFlush = true;

//                 WriteHeader();
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Failure to initialize file logging: {ex.Message}");
//                 EnableFileOutput = false;
//             }

//         }
//         private void WriteHeader()
//         {
//             logFileWriter?.WriteLine("================================================================================");
//             logFileWriter?.WriteLine($"TRIBE BUILD - GAME LOG");
//             logFileWriter?.WriteLine($"Session Start: {sessionStartTime}");
//             logFileWriter?.WriteLine($"Version: 1.0.0 (Demo)");
//             logFileWriter?.WriteLine("================================================================================");
//             logFileWriter?.WriteLine();
//         }

//         public void Log(LogLevel level, string category, string message)
//         {
//             if(level < MinimumLogLevel)
//             {
//                 return;
//             }

//             var entry = new LogEntry
//             {
//                 Level = level,
//                 Category = category,
//                 Message = message,
//                 Timestamp = DateTime.Now,
//                 GameTime = GetGameTimeString();
//             }
//         }

//         private string GetGameTimeString()
//         {
//             var manager = Core.Instance;
//             if(manager != null)
//             {
//                 return $"Day {manager.CurrentDay} {manager.GetTimeString()}";
//             }
//             return " ";
//         }



//     }

    
//     public class LogEntry
//     {
//         // noi dung cua log bao gom cac thong tin 
//         public LogLevel Level { get; set; }
//         public string Category { get; set; }
//         public string Message { get; set; }
//         public DateTime Timestamp { get; set; }
//         public string GameTime { get; set; }
//         public int Frame { get; set; }
//     }
// }