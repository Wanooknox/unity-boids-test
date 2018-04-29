using UnityEngine;

public class BoidManager : MonoBehaviour {
    [SerializeField]
    private Boid boidPrefab;

    [SerializeField]
    private int boidCount = 25;

    private Boid[] boids;

    private Avoid[] avoids;

    // Use this for initialization
    void Start() {
        avoids = FindObjectsOfType<Avoid>();


        SpawnSomeBoids(boidCount);
    }

    private void SpawnSomeBoids(int count) {
        boids = new Boid[boidCount];

        for (int i = 0; i < count; i++) {
            var pos = new Vector3() {
                x = Random.Range(-8f, 8f),
                y = Random.Range(-5f, 5f)
            };

            var boid = Instantiate(boidPrefab, pos, Quaternion.identity);
            boid.Avoids = avoids;
            boids[i] = boid;
        }
    }
}