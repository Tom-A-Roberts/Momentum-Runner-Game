using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCreator))]
public class PlayerSimulator : MonoBehaviour
{
    [Header("Running Settings")]

    [Tooltip("How much force the playersim puts into a jump")]
    public float JumpForce = 8f;
    [Tooltip("How quick the playersim runs and wallruns")]
    public float RunSpeed = 1f;
    [Tooltip("How much acceleration to use when accelerating to runspeed")]
    public float RunAcceleration = 1f;
    [Tooltip("How much drag the playersim experiences when running or wallrunning")]
    public float Drag = 0.5f;


    [Header("Falling Settings")]
    [Tooltip("How fast (on top of gravity) should the playersim fall. This should be the same as the player")]
    public float characterFallingRate = 20f;
    [Tooltip("If false, then the falling action may switch to another at any point during the fall, even if playersim is travelling upwards")]
    public bool changeOnlyWhenDownfalling = true;

    [Header("Wallrunning Settings")]
    [Tooltip("0= weightless, 1= as weighty as normal")]
    [Range(0f, 1f)]
    public float effectOfGravityDuringWallrun = 0.3f;
    [Tooltip("Amount of friction added to slow you moving down a wall during wallrunning")]
    public float verticalUpFrictionalCoefficient = 1;
    [Tooltip("Amount of friction added to slow you moving up a wall during wallrunning")]
    public float verticalDownFrictionalCoefficient = 1;

    [Header("Grappling Settings")]
    [Tooltip("Max distance the grapple rope can reach")]
    public float maxGrappleDistance = 25f;
    [Tooltip("The grapple rope will never choose to be shorter than this length")]
    public float minGrappleDistance = 5f;
    [Tooltip("playersim's velocity is increased by this force while grappling")]
    public float addForwardGrappleForce = 0.1f;
    [Tooltip("How late in the swing the grapple may release. (0 = up, 90 = horizontal)")]
    public float latestReleaseAngle = 0;
    [Tooltip("How early in the swing the grapple may release. (0 = up, 90 = horizontal)")]
    public float earliestReleaseAngle = 90;

    [Header("Other")]
    [Tooltip("Which direction (generally) should the playersim travel in")]
    public Vector3 forwardsDirection = new Vector3(0, 0, 1);
    [Tooltip("Difficulty adjusts things like fall time and grapple speed in order to give the playersim more momentum. 0=easy, 1=hardest")]
    [Range(0f, 1f)]
    public float difficulty = 0;
    [Tooltip("Should debugging info be logged")]
    public bool debugLogging = true;
    [Tooltip("How high fidelity should the sim be. High values (larger than 0.08) can cause odd behaviour. This should be equal to Time.FixedDeltaTimestep (0.02) if you want this sim to run in FixedUpdate() in realtime.")]
    public float simulationTimestepSize = 0.02f;
    [Tooltip("Every simulation frame the chances of each object type being placed is modified by this variable randomly.")]
    public float biomeChangeSpeed = 0.03f;

    // Static variable to track the other debugLogging variable, do not touch
    public static bool DebugLogging;

    // ### TRACKERS
    /// <summary>
    /// The current action the playersim is undergoing
    /// </summary>
    public Action currentAction;
    /// <summary>
    /// The current velocity of the playersim.
    /// </summary>
    public Vector3 velocity = Vector3.zero;

    //public float currentGrappleAngle = 0;

    /// <summary>
    /// All available actions in an array for reference
    /// </summary>
    private Action[] actions;

    /// <summary>
    /// for each available action, the chances of that action can be modified over time to make certain objects more or less frequent. The indices for this chance 
    /// array are the same as Action[] actions;
    /// </summary>
    private float[] actionChanceModifiers;

    /// <summary>
    /// Modifiable in realtime.
    /// Changes how long each action lasts on average. If this is 1, then actions last twice as long, -1 then actions never end.
    /// </summary>
    private float actionSpeedModifier = 0;

    /// <summary>
    /// Script that helps the creation of map elements such as walls and floors
    /// </summary>
    [System.NonSerialized]
    public MeshCreator meshCreator;

    [System.NonSerialized]
    public Vector3 sidewaysDirection;// = Vector3.Cross(playerSimulator.forwardsDirection, Vector3.up);


    /// <summary>
    /// An abstract class that describes an activity that a player may be doing, such as runnning or falling.
    /// </summary>
    public abstract class Action
    {
        /// <summary>
        /// Name of the action for debug purposes
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Where the action lies in the action array
        /// </summary>
        public readonly int ID;
        /// <summary>
        /// Average time that this action takes (in seconds)
        /// </summary>
        public readonly float AverageDuration;
        /// <summary>
        /// Allows actions to modify how likely they are to swap to another action. Setting this to 0 means it will never swap
        /// </summary>
        public float swapProbabilityModifier = 1;
        /// <summary>
        /// Knowledge of the parent that created the action, so position, velocity, and settings can be accessed
        /// </summary>
        public readonly PlayerSimulator playerSimulator;

        /// <summary>
        /// A dictionary of each action the playersim may switch to while undergoing this current action. The actions are stored with their probabilities
        /// which do not need to add to 1.
        /// </summary>
        public Dictionary<Action, float> transitionChances;
        
        /// <summary>
        /// A  list of all the positions that the playersim was in throughout the last action. This is reset every time the action is started.
        /// Useful for creating objects that span the positions.
        /// </summary>
        public List<Vector3> latestPositions;
        public Action(string name, PlayerSimulator playerSimulatorInstance, int actionID, float averageDuration)
        {
            Name = name;
            transitionChances = new Dictionary<Action, float>();
            playerSimulator = playerSimulatorInstance;
            playerSimulator.actions[actionID] = this;
            ID = actionID;
            AverageDuration = averageDuration;
        }
        /// <summary>
        /// Upon starting the action, this is called
        /// </summary>
        public virtual void Start()
        {
            if (DebugLogging)
                Debug.Log("Switching to " + Name + ".");
            playerSimulator.currentAction = this;
            latestPositions = new List<Vector3>();
        }
        /// <summary>
        /// Upon ending the action, this is called
        /// </summary>
        /// <param name="newAction">The action that the script will be switching to next</param>
        public virtual void End(Action newAction) { }
        /// <summary>
        /// Upon every sim update, this is called when this action is currently being undergone
        /// </summary>
        public virtual void Update() {
            RecordPosition();
        }
        /// <summary>
        /// Log the current position of the playersim, useful for later creating objects of the correct size.
        /// </summary>
        public void RecordPosition()
        {
            latestPositions.Add(playerSimulator.gameObject.transform.position);
        }
        /// <summary>
        /// Every frame, there is a chance to switch to a different action.
        /// This function rolls that dice
        /// </summary>
        public bool ActionEndsThisTurn()
        {
            float swapProbability = playerSimulator.simulationTimestepSize / AverageDuration;
            swapProbability *= swapProbabilityModifier;
            float rand = Random.value;
            if(rand < swapProbability)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    /// <summary>
    /// The playersim runs in a straight line, y velocity is set to 0
    /// </summary>
    public class Running : Action
    {
        public Running(string name, PlayerSimulator playerSimulatorInstance, int actionID, float averageDuration) : base(name, playerSimulatorInstance, actionID, averageDuration)
        {}
        public override void Update()
        {
            RecordPosition();
            playerSimulator.velocity.y = 0;
            playerSimulator.velocity *= (1 - playerSimulator.Drag)* playerSimulator.simulationTimestepSize;
            if(Vector3.Dot(playerSimulator.velocity, playerSimulator.forwardsDirection) < playerSimulator.RunSpeed)
                playerSimulator.velocity += playerSimulator.forwardsDirection * playerSimulator.RunAcceleration;
        }
        /// <summary>
        /// Jump when the player is no longer running (reaches the end of the platform)
        /// </summary>
        public override void End(Action newAction)
        {
            base.End(newAction);
            playerSimulator.velocity.y += playerSimulator.JumpForce;

            playerSimulator.meshCreator.CreateRunningCube(latestPositions, playerSimulator.difficulty);
        }
    }

    /// <summary>
    /// The playersim simply falls while waiting to switch to a new action. This is a good in-between for actions
    /// </summary>
    public class Falling : Action
    {
        public Falling(string name, PlayerSimulator playerSimulatorInstance, int actionID, float averageDuration) : base(name, playerSimulatorInstance, actionID, averageDuration)
        {
        }
        public override void Update()
        {
            RecordPosition();
            playerSimulator.velocity += Vector3.down * (Physics.gravity.magnitude + playerSimulator.characterFallingRate) * playerSimulator.simulationTimestepSize;
            
            // Only swap to another action when falling downwards
            if(playerSimulator.velocity.y > 0 && playerSimulator.changeOnlyWhenDownfalling)
            {
                swapProbabilityModifier = 0;
            }
            else
            {
                swapProbabilityModifier = 1;
            }
        
        }
    }

    /// <summary>
    /// The playersim wallruns with up and down friction, accelerating to runspeed
    /// </summary>
    public class Wallrunning : Action
    {
        public Wallrunning(string name, PlayerSimulator playerSimulatorInstance, int actionID, float averageDuration) : base(name, playerSimulatorInstance, actionID, averageDuration)
        {
        }
        /// <summary>
        /// The playersim wallruns with up and down friction, accelerating to runspeed
        /// </summary>
        public override void Update()
        {
            RecordPosition();
            // Add a little gravity to the wallrun
            playerSimulator.velocity += Vector3.down * (Physics.gravity.magnitude + playerSimulator.characterFallingRate) * playerSimulator.simulationTimestepSize * (1 - playerSimulator.effectOfGravityDuringWallrun);

            // Add vertical wall friction
            if (playerSimulator.velocity.y < 0)
            {
                playerSimulator.velocity += Vector3.up * -playerSimulator.velocity.y * playerSimulator.verticalUpFrictionalCoefficient;
            }
            else
            {
                playerSimulator.velocity += Vector3.up * -playerSimulator.velocity.y * playerSimulator.verticalDownFrictionalCoefficient;
            }
            // Incorporate drag
            playerSimulator.velocity *= (1 - playerSimulator.Drag) * playerSimulator.simulationTimestepSize;

            // Accelerate up to run speed
            if (Vector3.Dot(playerSimulator.velocity, playerSimulator.forwardsDirection) < playerSimulator.RunSpeed)
                playerSimulator.velocity += playerSimulator.forwardsDirection * playerSimulator.RunAcceleration;
        }
        public override void End(Action newAction)
        {
            base.End(newAction);

            // Add wallkick
            playerSimulator.velocity.y += playerSimulator.JumpForce;

            bool leftSide = false;
            if (Vector3.Dot(playerSimulator.sidewaysDirection, playerSimulator.velocity) > 0)
            {
                leftSide = true;
            }
            playerSimulator.meshCreator.CreateWallrunningWall(latestPositions, leftSide, playerSimulator.difficulty);
        }
    }

    /// <summary>
    /// The playersim chooses a random grapple point in front of it, swings then releases when it reaches the desired random angle
    /// </summary>
    public class Grappling : Action
    {
        /// <summary>
        /// The radius of the swing
        /// </summary>
        private float chosenRadius;
        /// <summary>
        /// Randomly, when should the playersim release the grapple (0 = up, 90 = horizontal).
        /// </summary>
        private float chosenReleaseAngle;
        /// <summary>
        /// Where the grapple point gets placed in worldspace
        /// </summary>
        public Vector3 grapplePoint;
        /// <summary>
        /// This gets set to true when the playersim is currently at the end of the rope (the rope is fully taut)
        /// </summary>
        private bool ropeIsFullyExtended = false;
        public Grappling(string name, PlayerSimulator playerSimulatorInstance, int actionID, float averageDuration) : base(name, playerSimulatorInstance, actionID, averageDuration)
        {
        }
        /// <summary>
        /// Choose a grapple point and release angle
        /// </summary>
        public override void Start()
        {
            base.Start();

            grapplePoint = playerSimulator.transform.position + (Vector3.up + Vector3.forward * Random.value).normalized * (playerSimulator.minGrappleDistance + Random.value * (playerSimulator.maxGrappleDistance - playerSimulator.minGrappleDistance));

            chosenRadius = Vector3.Distance(playerSimulator.transform.position, grapplePoint);

            chosenReleaseAngle = Random.value * (playerSimulator.latestReleaseAngle - playerSimulator.earliestReleaseAngle) + playerSimulator.earliestReleaseAngle;
        }

        /// <summary>
        /// Swing on the grapple point and check if releasing is required
        /// </summary>
        public override void Update()
        {
            RecordPosition();

            playerSimulator.velocity += Vector3.down * (Physics.gravity.magnitude + playerSimulator.characterFallingRate) * playerSimulator.simulationTimestepSize;

            if (Vector3.Distance(playerSimulator.transform.position, grapplePoint) > chosenRadius)
            {
                Vector3 directionToGrapple = (grapplePoint - playerSimulator.transform.position).normalized;
                float forceAwayFromGrapple = Vector3.Dot(playerSimulator.velocity, directionToGrapple);
                if(forceAwayFromGrapple < 0)
                {
                    // Rope force
                    playerSimulator.velocity += -forceAwayFromGrapple * directionToGrapple;

                    // Add more force forwards:
                    playerSimulator.velocity += playerSimulator.velocity.normalized * playerSimulator.addForwardGrappleForce;
                    ropeIsFullyExtended = true;
                }
            }
            else
            {
                // Don't swap action if not currently at end of rope
                ropeIsFullyExtended = false;
            }

            if (ropeIsFullyExtended)
            {

                if(playerSimulator.velocity.y > 0 && playerSimulator.transform.position.y > grapplePoint.y)
                {
                    // SET the probability to velocityProbabilityTracker.
                    // This requires us to cancel out the previous value that the probability calculation uses.
                    swapProbabilityModifier = AverageDuration / playerSimulator.simulationTimestepSize;
                }
                else
                {
                    float angleToGrapple = Mathf.Acos(Vector3.Dot(playerSimulator.velocity.normalized, Vector3.up)) * Mathf.Rad2Deg;
                    //playerSimulator.currentGrappleAngle = angleToGrapple;
                    if (angleToGrapple < chosenReleaseAngle)
                    {
                        // SET the probability to velocityProbabilityTracker.
                        // This requires us to cancel out the previous value that the probability calculation uses.
                        swapProbabilityModifier = AverageDuration / playerSimulator.simulationTimestepSize;
                    }
                    else
                    {
                        swapProbabilityModifier = 0;
                    }
                }
            }
            else
            {
                swapProbabilityModifier = 0;
            }
        }
    }

    /// <summary>
    /// Setup actions and their transition chances
    /// </summary>
    void Start()
    {
        meshCreator = GetComponent<MeshCreator>();
        DebugLogging = debugLogging;
        sidewaysDirection = Vector3.Cross(forwardsDirection, Vector3.up);

        actions = new Action[4];
        actionChanceModifiers = new float[4];

        Falling falling = new("falling", this, actionID: 0, averageDuration: 0.4f);
        Running running = new("running", this, actionID: 1, averageDuration: 1f);
        Wallrunning wallrunning = new("wallrunning", this, actionID: 2, averageDuration: 1f);
        Grappling grappling = new("grappling", this, actionID: 3, averageDuration: 2f);

        running.Start();

        // Chances to swap to each other action when in each action.
        //falling.transitionChances[running] = 0.25f;
        falling.transitionChances[wallrunning] = 0.25f;
        //falling.transitionChances[grappling] = 0.25f;

        //running.transitionChances[running] = 0.2f;
        running.transitionChances[falling] = 0.6f;
        //running.transitionChances[wallrunning] = 0.6f;
        //running.transitionChances[grappling] = 0.2f;
        //grappling.transitionChances[falling] = 0.8f;
        //grappling.transitionChances[grappling] = 0.2f;

        //wallrunning.transitionChances[falling] = 0.8f;
        //wallrunning.transitionChances[grappling] = 0.2f;
        wallrunning.transitionChances[falling] = 0.8f;

    }

    void FixedUpdate()
    {
        SimUpdate();
    }

    /// <summary>
    /// Update the position according to velocity, check if we should swap action to another, update the biome modifiers, then call update() on the current action
    /// </summary>
    void SimUpdate()
    {
        transform.position += velocity * simulationTimestepSize;

        if (currentAction.ActionEndsThisTurn())
        {
            ChooseNewAction();
        }
        ChangeBiomeModifiers();

        currentAction.Update();
    }

    /// <summary>
    /// Change the chances of each action being called, over time. This is to introduce some large overall level-scale variation
    /// </summary>
    void ChangeBiomeModifiers()
    {
        for (int actionID = 0; actionID < actionChanceModifiers.Length; actionID++)
        {
            actionChanceModifiers[actionID] += (Random.value - 0.5f) * biomeChangeSpeed;
            actionSpeedModifier = Mathf.Clamp(actionSpeedModifier, -0.8f, 0.8f);
        }
        actionSpeedModifier += (Random.value - 0.5f) * biomeChangeSpeed;
        actionSpeedModifier = Mathf.Clamp(actionSpeedModifier, -0.8f, 0.8f);
    }

    /// <summary>
    /// Assess which actions we can switch to from the current one and choose a new action according to their weights
    /// </summary>
    void ChooseNewAction()
    {
        float[] actionChances = new float[actionChanceModifiers.Length];
        float chanceTotal = 0;
        for (int actionID = 0; actionID < actionChanceModifiers.Length; actionID++)
        {
            if (currentAction.transitionChances.ContainsKey(actions[actionID]))
            {
                // Prevent the chance ever hitting 0
                if (actionChanceModifiers[actionID] <= -0.99f)
                    actionChanceModifiers[actionID] = -0.99f;

                // Chance of swapping to this action is the transition chance + ambient modifier
                actionChances[actionID] = currentAction.transitionChances[actions[actionID]] * (actionChanceModifiers[actionID] + 1);
                chanceTotal += actionChances[actionID];
            }
            else
            {
                actionChances[actionID] = 0;
            }
        }
        float randomFloat = Random.value;
        float accumulatedWeightings = 0;
        string debugOutput = "Chances per choice: ";
        for (int actionID = 0; actionID < actionChanceModifiers.Length; actionID++)
        {
            debugOutput += actions[actionID].Name + ": " + actionChances[actionID].ToString() + ", ";
            actionChances[actionID] /= chanceTotal;
            accumulatedWeightings += actionChances[actionID];
            
            if (randomFloat <= accumulatedWeightings)
            {
                Action newAction = actions[actionID];
                currentAction.End(newAction);

                newAction.Start();
                if (DebugLogging) Debug.Log(debugOutput);
                break;
            }
        }
    }

}
