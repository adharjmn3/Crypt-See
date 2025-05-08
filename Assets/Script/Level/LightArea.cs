using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // For Light2D

[RequireComponent(typeof(Light2D))]
[RequireComponent(typeof(PolygonCollider2D))]
public class LightArea : MonoBehaviour
{
    private Light2D light2D;
    private PolygonCollider2D polygonCollider;

    [SerializeField, Range(3, 100)] private int circleSegments = 36; // Number of segments for the circle

    void Awake()
    {
        // Get the Light2D and PolygonCollider2D components
        light2D = GetComponent<Light2D>();
        polygonCollider = GetComponent<PolygonCollider2D>();

        // Ensure the collider is set as a trigger
        polygonCollider.isTrigger = true;

        // Generate the polygon collider based on the light's properties
        GenerateHalfCircularCollider();
    }

    void GenerateHalfCircularCollider()
    {
        // Clear existing points
        polygonCollider.pathCount = 1;

        // Generate points for the half-circle
        List<Vector2> halfCirclePoints = GenerateHalfCirclePoints(light2D.pointLightOuterRadius);

        // Set the points to the collider
        polygonCollider.SetPath(0, halfCirclePoints.ToArray());
    }

    List<Vector2> GenerateHalfCirclePoints(float radius)
    {
        List<Vector2> points = new List<Vector2>();

        // If the radius is zero, return an empty list
        if (radius <= 0)
        {
            return points;
        }

        // Calculate the angle step based on the number of segments
        float angleStep = 180f / (circleSegments - 1); // Half-circle spans 180 degrees

        // Generate points along the half-circle
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep); // Convert angle to radians

            Vector2 point = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            points.Add(point);
        }

        // Add the center point to close the half-circle
        points.Add(Vector2.zero);

        return points;
    }

    void OnValidate()
    {
        // Regenerate the light area when properties are changed in the editor
        if (light2D != null && polygonCollider != null)
        {
            GenerateHalfCircularCollider();
        }
    }
}
