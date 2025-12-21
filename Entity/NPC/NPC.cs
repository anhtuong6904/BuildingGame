using Microsoft.Xna.Framework;
using MonoGameLibrary.Behavior;

namespace TribeBuild.Entity.NPC
{
    /// <summary>
    /// Base class for all NPC AI implementations
    /// </summary>
    public abstract class NPC 
    {
        // Needs
        public float Energy { get; set; }
        public float MaxEnergy { get; set; }
        public float Hunger { get; set; }
        public float MaxHunger { get; set; }
        
        // Movement
        public float Speed { get; set; }
        
        // State
        public NPCState CurrentState { get; protected set; }

        // Behavior Tree
        protected BehaviorTree behaviorTree;
        protected BehaviorContext context;

        protected NPC()
        {
            Energy = 100f;
            MaxEnergy = 100f;
            Hunger = 0f;
            MaxHunger = 100f;
            Speed = 80f;
            CurrentState = NPCState.Idle;
        }

        /// <summary>
        /// Initialize the behavior tree - must be implemented by subclasses
        /// </summary>
        protected abstract void InitializeBehaviorTree();

        /// <summary>
        /// Main update loop for NPC AI
        /// </summary>
        public virtual void Update(GameTime gameTime, NPCBody body)
        {
            // Update needs (energy, hunger)
            UpdateNeeds(gameTime);

            // Execute behavior tree
            if (behaviorTree != null)
            {
                context = new BehaviorContext(body, gameTime);
                behaviorTree.Tick(context);
            }

            // Update animation based on current state
            UpdateAnimation(gameTime, body);

            // Clamp values
            Energy = MathHelper.Clamp(Energy, 0f, MaxEnergy);
            Hunger = MathHelper.Clamp(Hunger, 0f, MaxHunger);
        }

        /// <summary>
        /// Update energy and hunger over time
        /// </summary>
        protected virtual void UpdateNeeds(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Energy decreases over time
            Energy -= 1f * deltaTime;
            
            // Hunger increases over time
            Hunger += 2f * deltaTime;
            
            // Extra energy drain based on state
            switch (CurrentState)
            {
                case NPCState.Working:
                    Energy -= 2f * deltaTime;
                    break;
                case NPCState.Fighting:
                    Energy -= 3f * deltaTime;
                    break;
                case NPCState.Fleeing:
                    Energy -= 4f * deltaTime;
                    break;
            }
        }

        /// <summary>
        /// Update animation based on current state and direction
        /// </summary>
        protected virtual void UpdateAnimation(GameTime gameTime, NPCBody body)
        {
            if (body.AnimatedSprite == null) 
                return;

            body.AnimatedSprite.Update(gameTime);
            
        }

        /// <summary>
        /// Get animation name based on state and direction
        /// </summary>
        protected virtual string GetAnimationName(NPCBody body)
        {
            string state;
            
            switch (CurrentState)
            {
                case NPCState.Moving:
                case NPCState.Fleeing:
                    state = "walk";
                    break;
                case NPCState.Working:
                    state = "work";
                    break;
                case NPCState.Fighting:
                    state = "attack";
                    break;
                case NPCState.Sleeping:
                    state = "sleep";
                    break;
                case NPCState.Eating:
                    state = "eat";
                    break;
                default:
                    state = "idle";
                    break;
            }
            
            string direction = body.Direction.ToString().ToLower();
            return $"{state}-{direction}";
        }

        /// <summary>
        /// Change NPC state
        /// </summary>
        public void ChangeState(NPCState newState)
        {
            if (CurrentState != newState)
            {
                //GameLogger.Instance?.Debug("NPC", $"State changed: {CurrentState} -> {newState}");
                CurrentState = newState;
            }
        }

        /// <summary>
        /// Check if NPC has critical needs
        /// </summary>
        public bool HasCriticalNeeds()
        {
            return Energy < 15f || Hunger > 80f;
        }

        /// <summary>
        /// Check if NPC has low energy
        /// </summary>
        public bool HasLowEnergy()
        {
            return Energy < 40f;
        }

        /// <summary>
        /// Get energy percentage (0-1)
        /// </summary>
        public float GetEnergyPercent()
        {
            return Energy / MaxEnergy;
        }

        /// <summary>
        /// Get hunger percentage (0-1)
        /// </summary>
        public float GetHungerPercent()
        {
            return Hunger / MaxHunger;
        }
    }

    /// <summary>
    /// NPC states for behavior and animation
    /// </summary>

    /// <summary>
    /// Direction the NPC is facing
    /// </summary>
   
}