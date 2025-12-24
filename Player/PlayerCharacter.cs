using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using TribeBuild.Entity.Resource;
using TribeBuild.Entity.NPC.Animals;
using TribeBuild.Entity.NPC;
using TribeBuild.World;
using TribeBuild.Entity;
using TribeBuild.Entity.Enemies;

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
        private float stuckCheckTimer = 0f;
        private Vector2 lastCheckedPosition;
        private const float STUCK_CHECK_INTERVAL = 3f;
        private const float STUCK_THRESHOLD = 10f;
        private bool lockSprint;
        
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
            KnockBack = 20;
            
            this.atlas = atlas;
            Direction = Direction.Font;
            currentState = PlayerState.Idle;
            
            // Collision setup
            BlocksMovement = true;
            IsPushable = false;
            Layer = CollisionLayer.Player;
            
            // Load animation
            if (atlas != null)
            {
                AnimatedSprite = atlas.CreateAnimatedSprite("idle-font");
            }
            Scale = scale;
            var bounds = AnimatedSprite._region.Bound;

            AnimatedSprite._origin = new Vector2(0, 0);
            AnimatedSprite._scale = Scale;

            Collider = new Rectangle(
                (int)(bounds.Width * Scale.X * 0.25f),
                (int)(bounds.Height * Scale.X * 0.5f),
                (int)(bounds.Width * Scale.X * 0.5f),
                (int)(bounds.Height * Scale.X * 0.5f)
            );
            
            Equipment = new EquipmentManager(this);
            Inventory = new PlayerInventory(20);
            Name = "Player";
            lastCheckedPosition = pos;
            stuckCheckTimer = 0f;
            
            Console.WriteLine("[Player] Character created");
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            var keyState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            if(Stamina <= 0)
            {
                isSprinting = false;
                lockSprint = true;
            }
            
            // Regenerate stamina
            if (!isSprinting && Stamina < MaxStamina)
            {
                Stamina = Math.Min(Stamina + 10f * deltaTime, MaxStamina);
                if(Stamina >= 30)
                    lockSprint = false;
            }
            
            // Update cooldowns
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
            
            // Update knockback
            UpdateKnockback(deltaTime);
            
            // Handle input (only if no knockback)
            if (KnockbackVelocity.LengthSquared() < 0.1f)
            {
                HandleMovement(keyState, deltaTime);
                HandleEquipment(keyState, _prevKeyboard);
                HandleActions(keyState, _prevKeyboard, mouseState);
            }
            
            // Resolve overlaps
            var world = GameManager.Instance?.World;
            if (world != null)
            {
                ResolveOverlaps(world, pushStrength: 1.2f);
            }

            #if DEBUG
            stuckCheckTimer += deltaTime;
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                float distanceMoved = Vector2.Distance(Position, lastCheckedPosition);
                
                if (distanceMoved < STUCK_THRESHOLD && Velocity.LengthSquared() > 0)
                {
                    Console.WriteLine($"[Player] ⚠️ WARNING: Possible stuck! Moved only {distanceMoved:F1} pixels");
                    ValidatePosition();
                }
                
                lastCheckedPosition = Position;
                stuckCheckTimer = 0f;
            }
            #endif
            
            // Update animation
            UpdateAnimation();
            AnimatedSprite?.Update(gameTime);
            
            // Store previous states
            _prevKeyboard = keyState;
            _prevMouse = mouseState;
        }

        /// <summary>
        /// ✅ Movement with collision
        /// </summary>
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
                
                Vector2 desiredPosition = Position + Velocity * deltaTime;
                var world = GameManager.Instance?.World;
                
                if (world != null)
                {
                    Position = MoveWithCollision(desiredPosition, deltaTime, world);
                }
                else
                {
                    Position = desiredPosition;
                }
                
                // Update stamina
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
        /// ✅ Handle equipment switching
        /// </summary>
        private void HandleEquipment(KeyboardState keyState, KeyboardState prevKeyState)
        {
            Equipment.HandleInput(keyState, prevKeyState);
        }
        
        /// <summary>
        /// ✅ IMPROVED: Handle actions with better tool logic
        /// </summary>
        private void HandleActions(KeyboardState keyState, KeyboardState prevKeyState, MouseState mouseState)
        {
            // Attack/Use tool (SPACE or LEFT CLICK)
            if ((keyState.IsKeyDown(Keys.Space) && !prevKeyState.IsKeyDown(Keys.Space)) || 
                (mouseState.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton == ButtonState.Released))
            {
                UseTool();
            }
            
            // Interact (E key) - Quick pickup/talk
            if (keyState.IsKeyDown(Keys.E) && !prevKeyState.IsKeyDown(Keys.E))
            {
                TryInteract();
            }
        }
        
        /// <summary>
        /// ✅ IMPROVED: Use current tool with effectiveness system
        /// </summary>
        private void UseTool()
        {
            if (actionCooldown > 0) return;
            
            Tool currentTool = Equipment.CurrentTool;
            
            // Set cooldown and lock
            actionCooldown = currentTool.Cooldown;
            actionLockTime = 0.25f;
            isActing = true;
            
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            Vector2 targetPos = Position + GetDirectionVector() * currentTool.Range;
            var entities = world.GetEntitiesInRadius(targetPos, currentTool.Range);
            
            foreach (var entity in entities)
            {
                if (entity == this) continue;
                
                bool actionPerformed = false;
                float effectiveness = Equipment.GetEffectiveness(currentTool.Type, entity);
                float finalDamage = currentTool.Damage * effectiveness;
                
                switch (currentTool.Type)
                {
                    case ToolType.Axe:
                        if (entity is Tree tree && tree.IsActive)
                        {
                            currentState = PlayerState.Working;
                            tree.TakeDamage(finalDamage, this);
                            actionPerformed = true;
                            
                            if (effectiveness > 1f)
                                Console.WriteLine($"[Player] Axe effectiveness: {effectiveness:F1}x!");
                        }
                        break;
                        
                    case ToolType.Pickaxe:
                        // TODO: Add stone/rock gathering
                        break;
                        
                    case ToolType.Sword:
                        if (entity is AnimalEntity animal && animal.IsActive)
                        {
                            currentState = PlayerState.Attacking;
                            animal.TakeDamage(finalDamage, this);
                            actionPerformed = true;
                            if(animal.Health <= 0f)
                            {
                                Random random = new Random();
                                int foodCount = random.Next(1,3);
                                Inventory.AddItem("food", foodCount);
                            }
                                
                        }
                        else if (entity is NightEnemyEntity enemy && enemy.IsActive)
                        {
                            currentState = PlayerState.Attacking;
                            enemy.TakeDamage(finalDamage, this);
                            actionPerformed = true;
                            
                            if (effectiveness > 1f)
                                Console.WriteLine($"[Player] Sword effectiveness: {effectiveness:F1}x!");
                        }
                        
                        break;
                        
                    case ToolType.Hoe:
                        // TODO: Add farming
                        break;
                        
                    case ToolType.None: // Bare hands
                        if (entity is Tree handTree && handTree.IsActive)
                        {
                            currentState = PlayerState.Working;
                            handTree.TakeDamage(finalDamage, this);
                            actionPerformed = true;
                        }
                        else if (entity is AnimalEntity handAnimal && handAnimal.IsActive)
                        {
                            currentState = PlayerState.Attacking;
                            handAnimal.TakeDamage(finalDamage, this);
                            actionPerformed = true;
                        }
                        break;
                }
                
                if (actionPerformed)
                {
                    // Play sound effect here
                    break; // Only hit one entity
                }
            }
        }
        
        /// <summary>
        /// ✅ IMPROVED: Quick interact (pickup, talk, etc.)
        /// </summary>
        private void TryInteract()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            const float INTERACT_RANGE = 64f;
            var entities = world.GetEntitiesInRadius(Position, INTERACT_RANGE);
            
            foreach (var entity in entities)
            {
                if (entity == this) continue;
                
                // Pick berries from bushes
                if (entity is Bush bush && bush.IsActive)
                {
                    int berries = 3;
                    if (Inventory.AddItem("berry", berries))
                    {
                        bush.IsActive = false;
                        GameManager.Instance?.OnResourceCollected("berry", berries);
                        Console.WriteLine($"[Player] Picked {berries} berries");
                    }
                    return;
                }
                
                // Quick pickup fallen items (if implemented)
                // TODO: Add item pickup system
                
                // Talk to NPCs
                if (entity is NPCBody npc && npc.IsActive)
                {
                    Console.WriteLine($"[Player] Talking to NPC #{npc.ID}");
                    // TODO: Open dialogue system
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

        public void ValidatePosition()
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            var tilemap = world.Tilemap;
            if (tilemap == null) return;
            
            Point currentTile = tilemap.WorldToTile(Position);
            
            if (!tilemap.IsTileWalkable(currentTile.X, currentTile.Y))
            {
                Console.WriteLine($"[Player] ⚠️ INVALID POSITION");
                
                Point? nearestWalkable = FindNearestWalkableTile(tilemap, currentTile, 10);
                
                if (nearestWalkable.HasValue)
                {
                    Position = tilemap.TileToWorld(nearestWalkable.Value.X, nearestWalkable.Value.Y);
                    Console.WriteLine($"[Player] ✅ Corrected position");
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

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 200f)
        {
            Health -= damage;
            Console.WriteLine($"[Player] Took {damage} damage! HP: {Health:F0}/{MaxHealth:F0}");
            
            ApplyKnockback(knockbackDir, knockbackForce);
            
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