using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Myra;
using TribeBuild.Scenes;
using FontStashSharp;


namespace TribeBuild;

public class Game1 : Core
{

    public SpriteFont debugFont {get; private set;}
    public SpriteFontBase font {get; private set;}
    
    public Game1() : base("TribeBuild", 1920, 1080, true)
    {
        
        
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        base.Initialize();
        MyraEnvironment.Game = this;
        // debugFont = Content.Load<SpriteFont>("Font/Baskic8"); 
        // font = Content.Load<SpriteFontBase>("Font/Baskic8");
        
        
       ChangeScene(new GameplayScene());
    
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }


}
