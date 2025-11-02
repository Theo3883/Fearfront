using UnityEngine;
using TMPro;

public class ScoreZone : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI pointsText;

    public GameObject explosion1Prefab;
    public GameObject explosion2Prefab;
    public GameObject explosion3Prefab;

    private int score = 0;
    private int totalPoints = 0;

    public float minDistanceFor2Points = 2.0f;
    public float minDistanceFor3Points = 4.0f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("BBasketball"))
            return;

        BBasketball parentBall = other.GetComponentInParent<BBasketball>();
        if (parentBall == null)
        {
            Debug.LogWarning("[ScoreZone] BBasketball parent not found.");
            return;
        }

        Vector3 releasePosition = parentBall.ReleasePosition;
        float distance = Vector3.Distance(releasePosition, transform.position);

        int points = 1;
        if (distance >= minDistanceFor3Points)
            points = 3;
        else if (distance >= minDistanceFor2Points)
            points = 2;

        score++;
        totalPoints += points;

        Debug.Log($"[ScoreZone] Release: {releasePosition}, Target: {transform.position}, Distance: {distance:F3}, Points earned: {points}, Hoops: {score}, Total Points: {totalPoints}");

        if (scoreText != null)
            scoreText.text = "Hoops: " + score;
        if (pointsText != null)
            pointsText.text = "Points: " + totalPoints;

        switch (points)
        {
            case 1: PlayExplosion(explosion1Prefab); break;
            case 2: PlayExplosion(explosion2Prefab); break;
            case 3: PlayExplosion(explosion3Prefab); break;
        }

        parentBall.ResetPosition();
    }

    private void PlayExplosion(GameObject prefab)
    {
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity);
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();

        Destroy(instance, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
