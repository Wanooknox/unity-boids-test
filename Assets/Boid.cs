using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour {
    private static float maxSpeed = 2f;
    private static float friendRadius = 5f;
    private static float coheseRadius = 2f;
    private static Bounds bounds = new Bounds(Vector3.zero, new Vector3(16, 10));

    private Boid[] friendBoids;

    public Vector3 Velocity;

    public Vector3 Pos {
        get { return transform.position; }
        set {
            if (float.IsNaN(value.x) || float.IsNaN(value.y)) {
                print(value);
            } else {
                transform.position = value;
            }
        }
    }

    private void Start() {
        Velocity = Vector3.zero;
    }

    private void Update() {
        UpdateFriends();

        DoUpdatePositionAndJunk();

        WrapAround();
    }

    private void DoUpdatePositionAndJunk() {
        var currBoid = this;

        Vector3 centerMass = CalcCenterMassVector();
        Vector3 avoidance = CalcAvoidanceVector();
        Vector3 matchSpeed = CalcMatchSpeedVector();
        Vector3 noise = new Vector3(1f, 1f) * Random.Range(-1f, 1f);

        Velocity += centerMass * 1f;
        Velocity += avoidance;
        Velocity += matchSpeed;
        Velocity += noise * 0.1f;

        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

        currBoid.Pos += Velocity * Time.deltaTime;

        // rotate to direction of velocity
        currBoid.transform.right = Velocity.normalized;
    }

    private void UpdateFriends() {
        Boid[] allBoids = FindObjectsOfType<Boid>();
        List<Boid> nearby = new List<Boid>();

        for (int i = 0; i < allBoids.Length; i++) {
            if (allBoids[i] != this) {
                if (Vector3.Distance(allBoids[i].Pos, this.Pos) < friendRadius) {
                    nearby.Add(allBoids[i]);
                }
            } else {
                Debug.Log("skipping self");
            }
        }

        friendBoids = nearby.ToArray();
    }


    private Vector3 CalcCenterMassVector() {
        Vector3 center = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            if (friendBoids[i] != this) {
                center += friendBoids[i].Pos;
            } else {
                Debug.Log("skipping self");
            }
        }

        center = center / (friendBoids.Length - 1);

        return (center - this.Pos) / 100f;
    }


    private Vector3 CalcAvoidanceVector() {
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < friendBoids.Length; i++) {
            if (friendBoids[i] != this) {
                var bPos = friendBoids[i].Pos;
                Vector3 diff = new Vector3() {
                    x = Mathf.Abs(bPos.x - this.Pos.x),
                    y = Mathf.Abs(bPos.y - this.Pos.y)
                };

                if (diff.magnitude < 0.5f) {
                    avoidance -= (bPos - this.Pos);
                }
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

        perceivedVelocity = perceivedVelocity / (friendBoids.Length - 1);

        return (perceivedVelocity - Velocity) / 8f;
    }

    private void WrapAround() {
        var vector3 = Pos;

        if (Pos.x >= bounds.max.x) {
            vector3.x = bounds.min.x;
        } else if (Pos.x <= bounds.min.x) {
            vector3.x = bounds.min.x;
        }

        if (Pos.y >= bounds.max.y) {
            vector3.y = bounds.min.y;
        } else if (Pos.y <= bounds.min.y) {
            vector3.y = bounds.min.y;
        }

        this.transform.position = vector3;
    }
}