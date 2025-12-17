using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using MonoGameLibrary.Scenes;
using TribeBuild.Scenes;


namespace TribeBuild;

public class Game1 : Core
{


    
    public Game1() : base("TribeBuild", 1920, 1080, true)
    {
        
        
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        base.Initialize();
        
       ChangeScene(new GameplayScene());
    
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }


}
