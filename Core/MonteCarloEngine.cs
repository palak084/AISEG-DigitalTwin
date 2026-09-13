using System;
using System.Collections.Generic;

namespace AISEG.DigitalTwin.Core;

/// <summary>
/// Monte Carlo simulation engine for the SMART-SEG digital twin.
/// Runs N independent trials with randomized waste streams and sensor configurations
/// to predict throughput, hazard detection rates, and system reliability.
/// </summary>
public sealed class MonteCarloEngine
{
    public sealed class TrialResult
    {
        public int ItemsProcessed { get; set; }
        public int HazardsDetected { get; set; }
        public int HazardsMissed { get; set; }
        public float DetectionRate => (HazardsDetected + HazardsMissed) > 0
            ? 100f * HazardsDetected / (HazardsDetected + HazardsMissed)
            : 100f;
        public float EffectiveThroughput { get; set; }
    }

    public sealed class SimulationResult
    {
        public int TrialCount { get; set; }
        public float MeanThroughput { get; set; }
        public float StdDevThroughput { get; set; }
        public float MeanDetectionRate { get; set; }
        public float StdDevDetectionRate { get; set; }
        public float MinDetectionRate { get; set; }
        public float MaxDetectionRate { get; set; }
        public float MeanHazardsMissed { get; set; }
        public float P95Throughput { get; set; }     // 5th percentile (worst case)
        public float P95DetectionRate { get; set; }  // 5th percentile
        public bool IsComplete { get; set; }
        public List<TrialResult> Trials { get; set; } = new();
    }

    private readonly Random _rng = new();
    
    /// <summary>
    /// Run a Monte Carlo simulation with the given parameters.
    /// Each trial simulates a fixed time window of waste processing.
    /// </summary>
    public SimulationResult Run(
        int trialCount,
        float beltSpeed,
        float spawnInterval,
        bool sensorDepth,
        bool sensorNir,
        bool sensorLoadCell,
        bool sensorInductive,
        bool sensorCapacitive,
        float simulatedSeconds = 60f)
    {
        var result = new SimulationResult { TrialCount = trialCount };
        
        float aiConfidence = ComputeConfidence(sensorDepth, sensorNir, sensorLoadCell, sensorInductive, sensorCapacitive);
        
        for (int trial = 0; trial < trialCount; trial++)
        {
            var trialResult = RunSingleTrial(
                beltSpeed, spawnInterval, sensorLoadCell,
                aiConfidence, simulatedSeconds);
            result.Trials.Add(trialResult);
        }
        
        // Compute statistics
        var throughputs = new List<float>();
        var detectionRates = new List<float>();
        float totalMissed = 0;
        
        foreach (var t in result.Trials)
        {
            throughputs.Add(t.EffectiveThroughput);
            detectionRates.Add(t.DetectionRate);
            totalMissed += t.HazardsMissed;
        }
        
        throughputs.Sort();
        detectionRates.Sort();
        
        result.MeanThroughput = Mean(throughputs);
        result.StdDevThroughput = StdDev(throughputs, result.MeanThroughput);
        result.MeanDetectionRate = Mean(detectionRates);
        result.StdDevDetectionRate = StdDev(detectionRates, result.MeanDetectionRate);
        result.MinDetectionRate = detectionRates.Count > 0 ? detectionRates[0] : 0;
        result.MaxDetectionRate = detectionRates.Count > 0 ? detectionRates[^1] : 0;
        result.MeanHazardsMissed = totalMissed / trialCount;
        
        // 5th percentile (worst-case)
        int p5Index = Math.Max(0, (int)(0.05f * throughputs.Count) - 1);
        result.P95Throughput = throughputs.Count > 0 ? throughputs[p5Index] : 0;
        result.P95DetectionRate = detectionRates.Count > 0 ? detectionRates[p5Index] : 0;
        
        result.IsComplete = true;
        return result;
    }
    
    private TrialResult RunSingleTrial(
        float beltSpeed, float spawnInterval,
        bool hasLoadCell, float aiConfidence,
        float simulatedSeconds)
    {
        var result = new TrialResult();
        
        float velocity = beltSpeed;
        float timePerSpawn = spawnInterval / MathF.Max(0.1f, velocity);
        int totalItems = (int)(simulatedSeconds / timePerSpawn);
        
        // Add noise to the item count (±15%)
        float noise = 1.0f + ((float)_rng.NextDouble() * 0.3f - 0.15f);
        totalItems = (int)(totalItems * noise);
        totalItems = Math.Max(1, totalItems);
        
        int hazardCount = 0;
        
        for (int i = 0; i < totalItems; i++)
        {
            bool isHazard = _rng.NextDouble() < 0.2; // 20% hidden hazard rate
            
            if (isHazard)
            {
                hazardCount++;
                if (hasLoadCell)
                {
                    // Detection probability based on AI confidence + noise
                    float detProb = (aiConfidence / 100f) * (0.9f + (float)_rng.NextDouble() * 0.1f);
                    if (_rng.NextDouble() < detProb)
                        result.HazardsDetected++;
                    else
                        result.HazardsMissed++;
                }
                else
                {
                    // Without load cell: only 30% chance of catching it via other sensors
                    if (_rng.NextDouble() < 0.30)
                        result.HazardsDetected++;
                    else
                        result.HazardsMissed++;
                }
            }
        }
        
        result.ItemsProcessed = totalItems;
        
        // Effective throughput: items/min adjusted by confidence
        float baseTPM = (totalItems / simulatedSeconds) * 60f;
        result.EffectiveThroughput = baseTPM * (aiConfidence / 100f) * noise;
        
        return result;
    }
    
    private static float ComputeConfidence(bool depth, bool nir, bool loadCell, bool inductive, bool capacitive)
    {
        float conf = 99.9f;
        if (!loadCell)   conf -= 35.7f;
        if (!nir)        conf -= 15.0f;
        if (!depth)      conf -= 10.0f;
        if (!inductive)  conf -= 5.0f;
        if (!capacitive) conf -= 5.0f;
        return MathF.Max(0f, conf);
    }
    
    private static float Mean(List<float> values)
    {
        if (values.Count == 0) return 0;
        float sum = 0;
        foreach (var v in values) sum += v;
        return sum / values.Count;
    }
    
    private static float StdDev(List<float> values, float mean)
    {
        if (values.Count < 2) return 0;
        float sumSq = 0;
        foreach (var v in values) sumSq += (v - mean) * (v - mean);
        return MathF.Sqrt(sumSq / (values.Count - 1));
    }
}
