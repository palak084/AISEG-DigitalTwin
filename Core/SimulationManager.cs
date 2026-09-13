using System;
using System.Collections.Generic;
using AISEG.DigitalTwin.Environment;
using AISEG.DigitalTwin.Conveyor;

namespace AISEG.DigitalTwin.Core;

public sealed class SimulationManager
{
    private readonly UConveyorPath _path;
    private readonly ConveyorParameters _parameters;
    
    public ConveyorParameters Parameters => _parameters;
    
    private readonly List<WasteObject> _activeWaste = new();
    private readonly RoboticArm[] _arms;
    
    private float _timeSinceLastSpawn = 0.0f;
    private readonly Random _rng = new Random();
    
    public IReadOnlyList<WasteObject> ActiveWaste => _activeWaste;
    
    // Global UI State inputs
    public bool SensorDepth { get; set; } = true;
    public bool SensorNir { get; set; } = true;
    public bool SensorLoadCell { get; set; } = true;
    public bool SensorInductive { get; set; } = true;
    public bool SensorCapacitive { get; set; } = true;

    public float AIConfidence 
    {
        get 
        {
            float conf = 99.9f;
            if (!SensorLoadCell) conf -= 35.7f;
            if (!SensorNir) conf -= 15.0f;
            if (!SensorDepth) conf -= 10.0f;
            if (!SensorInductive) conf -= 5.0f;
            if (!SensorCapacitive) conf -= 5.0f;
            return MathF.Max(0.0f, conf);
        }
    }
    
    public int ItemsProcessed { get; private set; } = 0;
    public int HazardsMissed { get; private set; } = 0;
    public int HazardsDetected { get; private set; } = 0;
    public float SpawnInterval { get; set; } = 2.5f; // metres of belt travel between spawns
    public float ElapsedTime { get; private set; } = 0f;
    
    public SimulationManager(UConveyorPath path, ConveyorParameters parameters, RoboticArm[] arms)
    {
        _path = path;
        _parameters = parameters;
        _arms = arms;
    }
    
    public void Update(double deltaTime)
    {
        if (!_parameters.IsRunning)
            return;
            
        float dt = (float)deltaTime;
        
        // 1. Spawning Logic
        _timeSinceLastSpawn += dt;
        
        ElapsedTime += dt;
        
        // Spawn a new waste object based on configurable spawn interval
        float spawnInterval = SpawnInterval / MathF.Max(0.1f, MathF.Abs(_parameters.Velocity));
        
        if (_timeSinceLastSpawn > spawnInterval)
        {
            SpawnWaste();
            _timeSinceLastSpawn = 0.0f;
        }
        
        // 2. Update existing waste objects
        for (int i = _activeWaste.Count - 1; i >= 0; i--)
        {
            var waste = _activeWaste[i];
            
            // Update the object's transform and distance
            waste.Update(dt, _parameters.Velocity);
            
            // Evaluate if it passes 8.5m (after all sensors)
            if (waste.EvalState == WasteObject.EvaluationState.Pending && waste.Distance > 8.5f)
            {
                // Simple logic: Remove Plastic, Keep Cardboard/Paper/Metal (as an example)
                if (waste.Type == WasteType.PlasticBottle || waste.Type == WasteType.PlasticContainer)
                {
                    if (waste.HasHiddenHazard)
                    {
                        // HIDDEN HAZARD LOGIC (Stone in bag)
                        if (SensorLoadCell)
                        {
                            waste.EvalState = WasteObject.EvaluationState.Remove;
                            HazardsDetected++;
                        }
                        else
                        {
                            waste.EvalState = WasteObject.EvaluationState.Keep;
                            HazardsMissed++;
                        }
                    }
                    else
                    {
                        waste.EvalState = WasteObject.EvaluationState.Remove;
                    }
                }
                else
                {
                    waste.EvalState = WasteObject.EvaluationState.Keep;
                }
            }
            
            // If it reaches the end, it's in the pickup zone
            if (waste.Distance >= _path.TotalLength - 1.0f)
            {
                // Only pick it up if it is evaluated to be Removed
                if (waste.EvalState == WasteObject.EvaluationState.Remove)
                {
                    bool picked = false;
                    foreach (var arm in _arms)
                    {
                        if (!arm.IsPicking)
                        {
                            arm.StartPicking(waste.Position);
                            picked = true;
                            break;
                        }
                    }
                }
                
                // Regardless of whether an arm picked it, we remove it from the belt 
                // to simulate it being processed or falling off the end.
                _activeWaste.RemoveAt(i);
                ItemsProcessed++;
            }
        }
    }
    
    private void SpawnWaste()
    {
        // Random waste type
        var types = Enum.GetValues<WasteType>();
        WasteType randomType = types[_rng.Next(types.Length)];
        
        // Force plastic sometimes to show the hazard logic
        bool isHiddenHazard = false;
        if (_rng.NextDouble() < 0.2) // 20% chance of hidden hazard
        {
            randomType = WasteType.PlasticBottle; // Or PlasticContainer
            isHiddenHazard = true;
        }

        // Random slight lateral offset (-0.3 to 0.3)
        float lateralOffset = (float)(_rng.NextDouble() * 0.6 - 0.3);
        
        // Random rotation
        float baseYaw = (float)(_rng.NextDouble() * 360.0);
        
        // The waste object creates its own internal primitive parts upon initialization
        var waste = new WasteObject(
            _path,
            randomType,
            0.0f, // Start at distance 0
            lateralOffset,
            1.0f, // Scale
            baseYaw,
            isHiddenHazard
        );
        
        _activeWaste.Add(waste);
    }
    
    public void Reset()
    {
        _activeWaste.Clear();
        _timeSinceLastSpawn = 0f;
        ItemsProcessed = 0;
        HazardsMissed = 0;
        HazardsDetected = 0;
        ElapsedTime = 0f;
    }
    
    public void Draw(Shader shader)
    {
        foreach (var waste in _activeWaste)
        {
            waste.Draw(shader);
        }
    }
}
