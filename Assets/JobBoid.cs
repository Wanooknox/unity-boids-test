using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;



public class JobBoid : MonoBehaviour {
    private static Bounds bounds = new Bounds(Vector3.zero, new Vector3(18f, 10f));
    
    [SerializeField] private float maxSpeed = 5;
    [SerializeField] private float friendRadius = 2f;
    [SerializeField] private float crowdRadius = 0.5f;
    [SerializeField] private float avoidRadius = 1.5f;
    [SerializeField] private float coheseRadius = 5f;

    // job handles
    private JobHandle centerMassJobHandle;
    private JobHandle avoidanceJobHandle;

    // rule vectors
    private Vector3 centerMass;
    private Vector3 avoidance;
    private NativeArray<Vector3> friendPositions;
    
    private JobBoid[] friendBoids;

    public Avoid[] Avoids;

    public Vector3 Velocity;

    public Vector3 Pos {
        get { return transform.position; }
        set { transform.position = value; }
    }

    private void Start() {
        Velocity = CalcNoiseVector();
//        UpdateFriends();
//        InvokeRepeating("UpdateFriends", Random.Range(0f, 5f), 2f);
    }

    private void Update() {
        UpdateFriends();
        ScheduleRuleJobs();
    }

    private void LateUpdate() {
        centerMassJobHandle.Complete();
        avoidanceJobHandle.Complete();
        
        CalcPositionAndRotation();
        WrapAround();
            
        friendPositions.Dispose();
    }

    private void ScheduleRuleJobs() {
        friendPositions = new NativeArray<Vector3>(friendBoids.Select(x => x.Pos).ToArray(), Allocator.TempJob);

        var centerMassJob = new CenterMassJob() {
            friendPositions = friendPositions,
            currPos = this.Pos,
            centerMass = centerMass
        };

        var avoidanceJob = new AvoidanceJob() {
            friendPositions = friendPositions,
            currPos = this.Pos,
            crowdRadius = crowdRadius,
            avoidance = avoidance
        };

        centerMassJobHandle = centerMassJob.Schedule();
        avoidanceJobHandle = avoidanceJob.Schedule();
    }

    private void CalcPositionAndRotation() {
        
//        Vector3 centerMass = CalcCenterMassVector();
//        Vector3 avoidance = CalcAvoidanceVector();
        Vector3 matchSpeed = CalcMatchSpeedVector();
        Vector3 noise = CalcNoiseVector();
        Vector3 avoidsVec = CalcAvoidanceAvoidsVector();

        UpdateVelocity(centerMass, avoidance, matchSpeed, noise, avoidsVec);

        var currBoid = this;
        currBoid.Pos += Velocity * Time.deltaTime;
        // rotate to direction of velocity
        currBoid.transform.right = Velocity.normalized;
    }

    private void UpdateVelocity(Vector3 centerMass, Vector3 avoidance, Vector3 matchSpeed, Vector3 noise,
        Vector3 avoidsVec) {
        Velocity += centerMass * 30f;
        Velocity += avoidance;
        Velocity += matchSpeed;
        Velocity += noise * 0.1f;
        Velocity += avoidsVec;
        
        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);
    }

    private static Vector3 CalcNoiseVector() {
        return new Vector3(Random.Range(-1f, 0f), Random.Range(-1f, 1f));
    }

    private void UpdateFriends() {
        JobBoid[] allBoids = FindObjectsOfType<JobBoid>();
        List<JobBoid> nearby = new List<JobBoid>();

        for (int i = 0; i < allBoids.Length; i++) {
            if (allBoids[i] != this) {
                if (Vector3.Distance(allBoids[i].Pos, this.Pos) < friendRadius) {
                    nearby.Add(allBoids[i]);
                }
            }
        }

        friendBoids = nearby.ToArray();
    }

    private Vector3 CalcCenterMassVector() {
        Vector3 center = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            if (friendBoids[i] != this) {
                center += friendBoids[i].Pos;
            }
        }

        center = (friendBoids.Length > 1) ? center / (friendBoids.Length - 1) : center;

        return (center - this.Pos) / 100f;
    }

    struct CenterMassJob : IJob {
        [ReadOnly] public NativeArray<Vector3> friendPositions;
        [ReadOnly] public Vector3 currPos;

        public Vector3 centerMass;

        public void Execute() {
            Vector3 center = Vector3.zero;

            for (int i = 0; i < friendPositions.Length; i++) {
                center += friendPositions[i];
            }

            center = (friendPositions.Length > 1) ? center / (friendPositions.Length - 1) : center;

            centerMass = (center - currPos) / 100f;
        }
    }

    private Vector3 CalcAvoidanceVector() {
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            float dist = Vector3.Distance(Pos, friendBoids[i].Pos);

            if (dist > 0 && dist < crowdRadius) {
                Vector3 diff = Vector3.Normalize(Pos - friendBoids[i].Pos);
                diff = diff / dist;
                avoidance += diff;
            }
        }

        return avoidance;
    }
    
    struct AvoidanceJob : IJob {
        [ReadOnly] public NativeArray<Vector3> friendPositions;
        [ReadOnly] public Vector3 currPos;
        [ReadOnly] public float crowdRadius;

        public Vector3 avoidance;

        public void Execute() {
            Vector3 avoidanceVec = Vector3.zero;

            for (int i = 0; i < friendPositions.Length; i++) {
                float dist = Vector3.Distance(currPos, friendPositions[i]);

                if (dist > 0 && dist < crowdRadius) {
                    Vector3 diff = Vector3.Normalize(currPos - friendPositions[i]);
                    diff = diff / dist;
                    avoidanceVec += diff;
                }
            }

            avoidance = avoidanceVec;
        }
    }

    private Vector3 CalcAvoidanceAvoidsVector() {
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < Avoids.Length; i++) {
            float dist = Vector3.Distance(Pos, Avoids[i].Pos);

            if (dist > 0 && dist < avoidRadius) {
                Vector3 diff = Vector3.Normalize(Pos - Avoids[i].Pos);
                diff = diff / dist;
                avoidance += diff;
            }
        }

        return avoidance;
    }

    private Vector3 CalcMatchSpeedVector() {
        Vector3 perceivedVelocity = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            if (Vector3.Distance(Pos, friendBoids[i].Pos) < coheseRadius) {
                perceivedVelocity += friendBoids[i].Velocity;
            }
        }

        perceivedVelocity = (friendBoids.Length > 1) ? perceivedVelocity / (friendBoids.Length - 1) : perceivedVelocity;

        return (perceivedVelocity - Velocity) / 8f;
    }

    private void WrapAround() {
        var dummy = Pos;
        dummy.x = (Pos.x + bounds.size.x) % bounds.size.x;
        dummy.y = (Pos.y + bounds.size.y) % bounds.size.y;
        Pos = dummy;
    }
}