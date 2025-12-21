using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.NPC;
using TribeBuild.World;

namespace TribeBuild.Player
{
    public class PlayerCharacter : Entity.Entity
    {
        // Stats
        public float MaxHealth { get; private set; }
        public float Health { get; set; }
        public float MaxStamina { get; private set; }
        public float Stamina { get; set; }
        public float MoveSpeed { get; private set; }
        public float SprintSpeed { get; private set; }
        
        // Movement
        public Direction Direction { get; set; }
        private bool isSprinting;
        
        // Equipment
        public EquipmentManager Equipment { get; private set; }
        private float actionCooldown;
        private float actionLockTime;
        private bool isActing;
        
        // Inventory
        public PlayerInventory Inventory { get; private set; }
        
        // Animation
        private TextureAtlas atlas;
        private Vector2 Scale;
        private PlayerState currentState;
        private Direction lastDirection;
        private PlayerState lastState;
        private KeyboardState _prevKeyboard;
        private MouseState _prevMouse;
        
        public enum PlayerState
        {
            Idle,
            Walking,
            Running,
            Working,
            Attacking
        }

        public PlayerCharacter(int id, Vector2 pos, TextureAtlas atlas, Vector2 scale) : base(id, pos)
        {
            // Stats
            MaxHealth = 100f;
            Health = MaxHealth;
            MaxStamina = 100f;
            Stamina = MaxStamina;
            MoveSpeed = 150f;
            SprintSpeed = 250f;
            
            this.atlas = atlas;
            Direction = Direction.Font;
            currentState = PlayerState.Idle;
            
            // Load animation
            if (atlas != null)
            {
                AnimatedSprite = atlas.CreateAnimatedSprite("idle-font");
            }

            Scale = scale != Vector2.Zero ? scale : Vector2.One;
            
            if (AnimatedSprite != null)
            {
                AnimatedSprite._scale = Scale;
                
                var bounds = AnimatedSprite._region.Bound;
                Collider = new Rectangle(
                    bounds.Width / 4,
                    bounds.Height / 2,
                    bounds.Width / 2,
                    bounds.Height / 2
                );
            }
            else
            {
                Collider = new Rectangle(0, 0, 32, 32);
            }
            
            Equipment = new EquipmentManager(this);
            Inventory = new PlayerInventory(20);
            Name = "Player";
            
            Console.WriteLine("[Player] Character created");
        }

        // ✅ FIX: Single Update method
        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            var keyState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            // Regenerate stamina
            if (!isSprinting && Stamina < MaxStamina)
            {
                Stamina = Math.Min(Stamina + 20f * deltaTime, MaxStamina);
            }
            
            if (actionCooldown > 0)
                actionCooldown -= deltaTime;

            if (actionLockTime > 0)
            {
                actionLockTime -= deltaTime;
                isActing = true;
            }
            else
            {
                isActing = false;
            }
            
            // Handle input
            HandleMovement(keyState, deltaTime);
            HandleEquipment(keyState, _prevKeyboard);
            HandleActions(keyState, _prevKeyboard, mouseState);
            
            // Update animation
            UpdateAnimation();
            AnimatedSprite?.Update(gameTime);
            
            // Store previous states
            _prevKeyboard = keyState;
            _prevMouse = mouseState;
        }
        
        private void HandleMovement(KeyboardState keyState, float deltaTime)
        {
            Vector2 moveDir = Vector2.Zero;
            
            if (keyState.IsKeyDown(Keys.W)) moveDir.Y -= 1;
            if (keyState.IsKeyDown(Keys.S)) moveDir.Y += 1;
            if (keyState.IsKeyDown(Keys.A)) moveDir.X -= 1;
            if (keyState.IsKeyDown(Keys.D)) moveDir.X += 1;
            
            isSprinting = keyState.IsKeyDown(Keys.LeftShift) && Stamina > 0;

            if (isActing)
            {
                Velocity = Vector2.Zero;
                return;
            }
            
            if (moveDir.LengthSquared() > 0)
            {
                moveDir.Normalize();
                
                float speed = isSprinting ? SprintSpeed : MoveSpeed;
                Velocity = moveDir * speed;
                
                // ✅ FIXED COLLISION: Check each axis separately
                Vector2 originalPos = Position;
                Vector2 newPosition = Position;
                
                // Try X axis movement
                Vector2 testPosX = originalPos + new Vector2(Velocity.X * deltaTime, 0);
                if (CanMoveTo(testPosX))
                {
                    newPosition.X = testPosX.X;
                }
                
                // Try Y axis movement
                Vector2 testPosY = originalPos + new Vector2(0, Velocity.Y * deltaTime);
                if (CanMoveTo(testPosY))
                {
                    newPosition.Y = testPosY.Y;
                }
                
                Position = newPosition;
                
                if (isSprinting)
                {
                    Stamina = Math.Max(0, Stamina - 30f * deltaTime);
                }
                
                UpdateDirection(moveDir);
                currentState = isSprinting ? PlayerState.Running : PlayerState.Walking;
            }
            else if (!isActing)
            {
                Velocity = Vector2.Zero;
                currentState = PlayerState.Idle;
            }
        }

        /// <summary>
        /// ✅ FIXED: Check ALL tiles covered by player collider
        /// </summary>
        private bool CanMoveTo(Vector2 newPosition)
        {
            var world = GameManager.Instance?.World;
            if (world == null) return true;
            
            var tilemap = world.Tilemap;
            if (tilemap == null) return true;
            
            // ✅ Bounds check
            if (newPosition.X < 0 || newPosition.Y < 0 || 
                newPosition.X >= tilemap.ScaleMapWidth || 
                newPosition.Y >= tilemap.ScaleMapHeight)
            {
                Console.WriteLine($"[Player] Out of bounds: ({newPosition.X:F0}, {newPosition.Y:F0})");
                return false;
            }
            
            // ✅ Create world-space collider at new position
            Rectangle newCollider = new Rectangle(
                (int)newPosition.X + Collider.X,
                (int)newPosition.Y + Collider.Y,
                Collider.Width,
                Collider.Height
            );
            
            // ✅ CRITICAL FIX: Check ALL 4 corners of collider
            Vector2[] cornerOffsets = new Vector2[]
            {
                new Vector2(newCollider.Left, newCollider.Top),      // Top-left
                new Vector2(newCollider.Right, newCollider.Top),     // Top-right
                new Vector2(newCollider.Left, newCollider.Bottom),   // Bottom-left
                new Vector2(newCollider.Right, newCollider.Bottom)   // Bottom-right
            };
            
            foreach (var corner in cornerOffsets)
            {
                Point tilePos = tilemap.WorldToTile(corner);
                
                // ✅ Check if tile exists
                if (tilePos.X < 0 || tilePos.X >= tilemap.Width ||
                    tilePos.Y < 0 || tilePos.Y >= tilemap.Height)
                {
                    return false;
                }
                
                // ✅ Check if tile is walkable
                if (!tilemap.IsTileWalkable(tilePos.X, tilePos.Y))
                {
                    // ✅ Debug log for water tiles
                    if (tilemap.IsWaterTile(tilePos.X, tilePos.Y))
                    {
                        Console.WriteLine($"[Player] ⛔ Cannot walk on WATER at tile ({tilePos.X}, {tilePos.Y})");
                    }
                    return false;
                }
            }
            
            // ✅ Check collision with static entities
            var nearbyEntities = world.KDTree.FindInRadius(newPosition, 64f);
            
            foreach (var entityResult in nearbyEntities)
            {
                var entity = entityResult.Item;
                if (entity == this || !entity.IsActive) continue;
                
                if (EntityBlocksMovement(entity))
                {
                    Rectangle entityCollider = GetEntityWorldCollider(entity);
                    if (newCollider.Intersects(entityCollider))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }


        private bool EntityBlocksMovement(Entity.Entity entity)
        {
            return entity switch
            {
                Tree tree => tree.IsActive,
                Bush bush => bush.IsActive,
                Mine mine => mine.IsActive,
                AnimalEntity => false,
                NPCBody => false,
                PlayerCharacter => false,
                _ => false
            };
        }

        private Rectangle GetEntityWorldCollider(Entity.Entity entity)
        {
            return new Rectangle(
                (int)entity.Position.X + entity.Collider.X,
                (int)entity.Position.Y + entity.Collider.Y,
                entity.Collider.Width,
                entity.Collider.Height
            );
        }
        
        private void HandleEquipment(KeyboardState keyState, KeyboardState prevKeyState)
        {
            Equipment.HandleInput(keyState, prevKeyState);
        }
        
        private void HandleActions(KeyboardState keyState, KeyboardState prevKeyState, MouseState mouseState)
        {
            if ((keyState.IsKeyDown(Keys.Space) && !prevKeyState.IsKeyDown(Keys.Space)) || 
                (mouseState.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton == ButtonState.Released &&
                actionCooldown <= 0))
            {
                UseTool();
            }
            
            if (keyState.IsKeyDown(Keys.E) && !prevKeyState.IsKeyDown(Keys.E))
            {
                TryInteract();
            }
        }
        
        private void UseTool()
        {
            if (actionCooldown > 0) return;
            
            Tool currentTool = Equipment.CurrentTool;
            isActing = true;
            actionCooldown = currentTool.Cooldown;
            actionLockTime = 0.25f;
            
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            Vector2 targetPos = Position + GetDirectionVector() * currentTool.Range;
            var entities = world.GetEntitiesInRadius(targetPos, currentTool.Range);
            
            foreach (var entity in entities)
            {
                if (entity == this) continue;
                
                bool actionPerformed = false;
                
                switch (currentTool.Type)
                {
                    case ToolType.Axe:
                        if (entity is Tree tree && tree.IsActive)
                        {
                            currentState = PlayerState.Working;
                            tree.TakeDamage(currentTool.Damage, this);
                            actionPerformed = true;
                        }
                        break;
                        
                    case ToolType.Sword:
                        if (entity is AnimalEntity animal && animal.IsActive)
                        {
                            currentState = PlayerState.Attacking;
                            animal.TakeDamage(currentTool.Damage, this);
                            actionPerformed = true;
                        }
                        break;
                        
                    case ToolType.None:
                        if (entity is Tree handTree && handTree.IsActive)
                        {
                            currentState = PlayerState.Working;
                            handTree.TakeDamage(currentTool.Damage, this);
                            actionPerformed = true;
                        }
                        else if (entity is AnimalEntity handAnimal && handAnimal.IsActive)
                        {
                            currentState = PlayerState.Attacking;
                            handAnimal.TakeDamage(currentTool.Damage, this);
                            actionPerformed = true;
                        }
                        break;
                }
                
                if (actionPerformed) break;
            }
        }
        
        private void TryInteract()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            var entities = world.GetEntitiesInRadius(Position, 64f);
            
            foreach (var entity in entities)
            {
                if (entity == this) continue;
                
                if (entity is Bush bush && bush.IsActive)
                {
                    Inventory.AddItem("berry", 3);
                    bush.IsActive = false;
                    Console.WriteLine($"[Player] Picked berries");
                    return;
                }
                
                if (entity is NPCBody npc && npc.IsActive)
                {
                    Console.WriteLine($"[Player] Talking to NPC #{npc.ID}");
                    return;
                }
            }
        }
        
        private void UpdateDirection(Vector2 moveDir)
        {
            if (Math.Abs(moveDir.Y) > Math.Abs(moveDir.X))
            {
                Direction = moveDir.Y > 0 ? Direction.Font : Direction.Back;
            }
            else
            {
                Direction = moveDir.X > 0 ? Direction.Right : Direction.Left;
            }
        }
        
        private Vector2 GetDirectionVector()
        {
            return Direction switch
            {
                Direction.Back => new Vector2(0, -1),
                Direction.Font => new Vector2(0, 1),
                Direction.Left => new Vector2(-1, 0),
                Direction.Right => new Vector2(1, 0),
                _ => new Vector2(0, 1)
            };
        }
        
        private void UpdateAnimation()
        {
            if (atlas == null) return;
            if (Direction == lastDirection && currentState == lastState) return;
            
            string animName = GetAnimationName();
            AnimatedSprite = atlas.CreateAnimatedSprite(animName);
            AnimatedSprite._scale = Scale;
            
            lastDirection = Direction;
            lastState = currentState;
        }

        /// <summary>
        /// ✅ Validate and correct player position if stuck on invalid tile
        /// </summary>
        public void ValidatePosition()
        {
            var tilemap = GameManager.Instance?.World?.Tilemap;
            if (tilemap == null) return;
            
            Point currentTile = tilemap.WorldToTile(Position);
            
            if (!tilemap.IsTileWalkable(currentTile.X, currentTile.Y))
            {
                Console.WriteLine($"[Player] ⚠️ INVALID POSITION: ({Position.X:F0}, {Position.Y:F0}) on tile ({currentTile.X}, {currentTile.Y})");
                
                if (tilemap.IsWaterTile(currentTile.X, currentTile.Y))
                {
                    Console.WriteLine($"[Player] Standing on WATER!");
                }
                
                Point? nearestWalkable = FindNearestWalkableTile(tilemap, currentTile, 10);
                
                if (nearestWalkable.HasValue)
                {
                    Position = tilemap.TileToWorld(nearestWalkable.Value.X, nearestWalkable.Value.Y);
                    Console.WriteLine($"[Player] ✅ Corrected to ({Position.X:F0}, {Position.Y:F0})");
                }
                else
                {
                    Console.WriteLine($"[Player] ❌ Could not find walkable tile nearby!");
                }
            }
        }

        private Point? FindNearestWalkableTile(Tilemap tilemap, Point center, int radius)
        {
            for (int r = 1; r <= radius; r++)
            {
                for (int angle = 0; angle < 360; angle += 45)
                {
                    float rad = MathHelper.ToRadians(angle);
                    int x = center.X + (int)(Math.Cos(rad) * r);
                    int y = center.Y + (int)(Math.Sin(rad) * r);
                    
                    if (x >= 0 && x < tilemap.Width &&
                        y >= 0 && y < tilemap.Height &&
                        tilemap.IsTileWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }
            
            return null;
        }
        
        private string GetAnimationName()
        {
            string state = currentState switch
            {
                PlayerState.Walking => "walk",
                PlayerState.Running => "walk",
                PlayerState.Working => "work",
                PlayerState.Attacking => "work",
                _ => "idle"
            };
            
            string dir = Direction.ToString().ToLower();
            return $"{state}-{dir}";
        }
        
        public void TakeDamage(float damage)
        {
            Health -= damage;
            Console.WriteLine($"[Player] Took {damage} damage! HP: {Health:F0}/{MaxHealth:F0}");
            
            if (Health <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            IsActive = false;
            Console.WriteLine("[Player] Player died!");
        }
        
        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;
            AnimatedSprite?.Draw(spriteBatch, Position);
        }
    }

    public class RPGCamera
    {
        private Camera2D camera;
        private PlayerCharacter player;
        private float smoothSpeed = 8f;
        
        public RPGCamera(Camera2D camera, PlayerCharacter player)
        {
            this.camera = camera;
            this.player = player;
        }
        
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector2 targetPos = player.Position;
            
            camera.Position = Vector2.Lerp(
                camera.Position,
                targetPos,
                smoothSpeed * deltaTime
            );
        }
        
        public void HandleZoom(MouseState mouseState, int previousScroll)
        {
            int scrollDelta = mouseState.ScrollWheelValue;
            if (scrollDelta != previousScroll)
            {
                float zoom = scrollDelta > previousScroll ? 0.1f : -0.1f;
                camera.Zoom = MathHelper.Clamp(camera.Zoom + zoom, 0.5f, 2f);
            }
        }
    }
}