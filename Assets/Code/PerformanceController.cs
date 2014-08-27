using UnityEngine;
using System.Collections;

public class PerformanceController : MonoBehaviour
{
  // TODO: Make public (when tweaking is finished)
  private bool shouldAutoAdjust = true;
  private static float initialPerformanceRatio = 0.7f;
  private static float maxPerformanceRatio = 0.7f;
  private static float minPerformanceRatio = 0.2f;
  private static float performanceRatioStep = 0.1f;

  private float bestAllowedFrameRate = 45f;
  private float worstAllowedFrameRate = 15f;

  private float minTimeSinceChangeUp = 1.0f;
  private float minTimeSinceChangeDown = 0.033f;

  private float debugLogTimeSpan = 0.5f;

  public static bool pauseAutoAdjust;

  public float manualPerformanceRatio;

  public static float performanceRatio { get; private set; }
  public static PerformanceValues performanceValues { get; private set; }

  private static float timeAtChange;

  private static float timeAtLastUpdate;
  private static float smootheDeltaTimeAtLastUpdate;

  private static float performanceRatioBeforePause;
  private static float ignoreUntilTime;

  private static float timeAtLastDebugLog;

  public static void PausePerformance()
  {
    Debug.Log("Pause Performance");
    pauseAutoAdjust = true;
    performanceRatioBeforePause = performanceRatio;
    ChangePerformance(0);
  }

  public static void ResetPerformance(float ignoreTimeSpan)
  {
    Debug.Log("Reset Performance");

    ChangePerformance(initialPerformanceRatio);
    pauseAutoAdjust = false;

    ignoreUntilTime = Time.time + ignoreTimeSpan;
  }

  public static void ContinuePerformance()
  {
    Debug.Log("Continue Performance");

    ChangePerformance(performanceRatioBeforePause);
    pauseAutoAdjust = false;
  }

  // Use update instead of fixed update to get the game frame rate
  void Update()
  {
    if (pauseAutoAdjust)
    {
      return;
    }

    timeAtLastUpdate = Time.time;
    smootheDeltaTimeAtLastUpdate = Time.smoothDeltaTime;
  }

  // Fixed update will occur before update when there is a problem
  void FixedUpdate()
  {
    if (!shouldAutoAdjust || pauseAutoAdjust)
    {
      return;
    }

    if (Time.time < ignoreUntilTime)
    {
      return;
    }

    // Make sure it has been initialized
    if (!performanceValues.hasValue)
    {
      ChangePerformance(initialPerformanceRatio);
      return;
    }

    if (manualPerformanceRatio != 0)
    {
      ChangePerformance(manualPerformanceRatio);
      return;
    }

    // Auto adjust performance (compare target physics framerate to engine framerate (which should be much faster))
    var timeSinceUpdate = Time.time - timeAtLastUpdate;

    var deltaTimeSmart = timeSinceUpdate;

    // Take the worst (biggest) delta time
    if (deltaTimeSmart < smootheDeltaTimeAtLastUpdate)
    {
      deltaTimeSmart = smootheDeltaTimeAtLastUpdate;
    }

    var frameTime = deltaTimeSmart;
    var frameRate = 1.0f / frameTime;
    var targetPhysicsTime = performanceValues.timeFixedTimestep;

    var timeSinceChange = Time.time - timeAtChange;

    if (Time.time - timeAtLastDebugLog > debugLogTimeSpan)
    {
      Debug.Log("GameTime=" + Time.time + ", TimeSinceChange=" + timeSinceChange + ", FrameRate=" + frameRate + ", FrameTime=" + frameTime + ", TargetPhysicsTime=" + targetPhysicsTime + ", PerformanceRatio=" + performanceRatio);
      timeAtLastDebugLog = Time.time;
    }

    // Good performance
    if (frameRate > bestAllowedFrameRate)
    {
      if (timeSinceChange > minTimeSinceChangeUp)
      {
        ChangePerformance(performanceRatio + performanceRatioStep);
      }
    }
    // Poor Performance
    else if (frameRate < worstAllowedFrameRate)
    {
      if (timeSinceChange > minTimeSinceChangeDown)
      {
        ChangePerformance(performanceRatio - performanceRatioStep);
      }
    }
  }


  private static void ChangePerformance(float targetPerformanceRatio)
  {
    targetPerformanceRatio = Mathf.Clamp(targetPerformanceRatio, 0.0f, 1.0f);
    targetPerformanceRatio = Mathf.Clamp(targetPerformanceRatio, minPerformanceRatio, maxPerformanceRatio);

    if (performanceRatio == targetPerformanceRatio)
    {
      return;
    }

    performanceRatio = targetPerformanceRatio;

    var values = PerformanceValues.GetValuesAtRatio(performanceRatio);
    performanceValues = values;

    Physics.solverIterationCount = values.physicsSolverIterationCount;
    Physics.sleepVelocity = values.physicsSleepVelocity;
    Physics.sleepAngularVelocity = values.physicsSleepAngularVelocity;
    Physics.minPenetrationForPenalty = values.physicsMinPenetrationForPenalty;
    Time.fixedDeltaTime = values.timeFixedTimestep;
    Time.maximumDeltaTime = values.timeMaximumeAllowedTimestep;
    MirrorObject.isEnabledGlobal = values.isMirroringEnabled;
    SimpleJoint.sleepDistance = values.simpleJointSleepDistance;

    timeAtChange = Time.time;

    Debug.Log(
      "Performance Changed: PerformanceRatio=" + performanceRatio + ", " +
      "physicsSolverIterationCount=" + values.physicsSolverIterationCount + ", " +
      "physicsSleepVelocity=" + values.physicsSleepVelocity + ", " +
      "physicsSleepAngularVelocity=" + values.physicsSleepAngularVelocity + ", " +
      "physicsMinPenetrationForPenalty=" + values.physicsMinPenetrationForPenalty + ", " +
      "timeFixedTimestep=" + values.timeFixedTimestep + ", " +
      "timeMaximumeAllowedTimestep=" + values.timeMaximumeAllowedTimestep + ", " +
      "isMirroringEnabled=" + values.isMirroringEnabled + ", " +
      "simpleJointSleepDistance=" + values.simpleJointSleepDistance
    );
  }

}

public struct PerformanceValues
{
  public bool hasValue;
  public int physicsSolverIterationCount;
  public float physicsSleepVelocity;
  public float physicsSleepAngularVelocity;
  public float physicsMinPenetrationForPenalty;
  public float timeFixedTimestep;
  public float timeMaximumeAllowedTimestep;
  public float simpleJointSleepDistance;

  public bool isMirroringEnabled;

  public static PerformanceValues Worst { get; private set; }
  public static PerformanceValues Best { get; private set; }
  public static PerformanceValues Range { get; private set; }

  private static float isMirroringEnabledMinRatio = 0.1f;

  static PerformanceValues()
  {
    Worst = new PerformanceValues()
    {
      physicsSolverIterationCount = 2,
      physicsSleepVelocity = 0.5f,//1f,
      physicsSleepAngularVelocity = 0.5f,//1f,
      physicsMinPenetrationForPenalty = 0.05f,//1f,
      timeFixedTimestep = 0.1f,//0.2f,
      timeMaximumeAllowedTimestep = 0.2f,
      simpleJointSleepDistance = 0.2f
    };

    Best = new PerformanceValues()
    {
      physicsSolverIterationCount = 7,
      physicsSleepVelocity = 0.3f,// 0.15f,
      physicsSleepAngularVelocity = 0.3f,//0.14f,
      physicsMinPenetrationForPenalty = 0.01f,//1f,
      timeFixedTimestep = 0.015f,
      timeMaximumeAllowedTimestep = 0.33f,
      simpleJointSleepDistance = 0.02f
    };

    Range = new PerformanceValues()
    {
      physicsSolverIterationCount = Best.physicsSolverIterationCount - Worst.physicsSolverIterationCount,
      physicsSleepVelocity = Best.physicsSleepVelocity - Worst.physicsSleepVelocity,
      physicsSleepAngularVelocity = Best.physicsSleepAngularVelocity - Worst.physicsSleepAngularVelocity,
      physicsMinPenetrationForPenalty = Best.physicsMinPenetrationForPenalty - Worst.physicsMinPenetrationForPenalty,
      timeFixedTimestep = Best.timeFixedTimestep - Worst.timeFixedTimestep,
      timeMaximumeAllowedTimestep = Best.timeMaximumeAllowedTimestep - Worst.timeMaximumeAllowedTimestep,
      simpleJointSleepDistance = Best.simpleJointSleepDistance - Worst.simpleJointSleepDistance
    };
  }

  public static PerformanceValues GetValuesAtRatio(float performanceRatio)
  {
    performanceRatio = Mathf.Clamp(performanceRatio, 0.0f, 1.0f);

    var values = new PerformanceValues()
    {
      hasValue = true,
      physicsSolverIterationCount = (int)(Worst.physicsSolverIterationCount + Range.physicsSolverIterationCount * performanceRatio),
      physicsSleepVelocity = Worst.physicsSleepVelocity + Range.physicsSleepVelocity * performanceRatio,
      physicsSleepAngularVelocity = Worst.physicsSleepAngularVelocity + Range.physicsSleepAngularVelocity * performanceRatio,
      physicsMinPenetrationForPenalty = Worst.physicsMinPenetrationForPenalty + Range.physicsMinPenetrationForPenalty * performanceRatio,
      timeFixedTimestep = Worst.timeFixedTimestep + Range.timeFixedTimestep * performanceRatio,
      timeMaximumeAllowedTimestep = Worst.timeMaximumeAllowedTimestep + Range.timeMaximumeAllowedTimestep * performanceRatio,
      simpleJointSleepDistance = Worst.simpleJointSleepDistance + Range.simpleJointSleepDistance * performanceRatio,

      isMirroringEnabled = performanceRatio < isMirroringEnabledMinRatio ? false : true
    };

    return values;
  }
}
