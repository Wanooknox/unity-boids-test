using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour {
    [SerializeField]
    private float maxSpeed = 5;

    [SerializeField]
    private float friendRadius = 2f;

    [SerializeField]
    private float crowdRadius = 0.5f;

    [SerializeField]
    private float coheseRadius = 5f;

    private static Bounds bounds = new Bounds(Vector3.zero, new Vector3(18f, 10f));

    private Boid[] friendBoids;

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
        CalcPositionAndRotation();
        WrapAround();
    }

    private void CalcPositionAndRotation() {
        var currBoid = this;

        Vector3 centerMass = CalcCenterMassVector();
        Vector3 avoidance = CalcAvoidanceVector();
        Vector3 matchSpeed = CalcMatchSpeedVector();
        Vector3 noise = CalcNoiseVector();

        Velocity += centerMass * 30f;
        Velocity += avoidance;
        Velocity += matchSpeed;
        Velocity += noise * 0.1f;

        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

        currBoid.Pos += Velocity * Time.deltaTime;

        // rotate to direction of velocity
        currBoid.transform.right = Velocity.normalized;
    }

    private static Vector3 CalcNoiseVector() {
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    }

    private void UpdateFriends() {
        Boid[] allBoids = FindObjectsOfType<Boid>();
        List<Boid> nearby = new List<Boid>();

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

        center = (friendBoids.Length > 1) ? center / (friendBoids.Length - 1) : Vector3.zero;

        return (center - this.Pos) / 100f;
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

    private Vector3 CalcMatchSpeedVector() {
        Vector3 perceivedVelocity = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            if (Vector3.Distance(Pos, friendBoids[i].Pos) < coheseRadius) {
                perceivedVelocity += friendBoids[i].Velocity;
            }
        }

        perceivedVelocity = (friendBoids.Length > 1) ? perceivedVelocity / (friendBoids.Length - 1) : Vector3.zero;

        return (perceivedVelocity - Velocity) / 8f;
    }

    private void WrapAround() {
        var dummy = Pos;
        dummy.x = (Pos.x + bounds.size.x) % bounds.size.x;
        dummy.y = (Pos.y + bounds.size.y) % bounds.size.y;
        Pos = dummy;
    }
}