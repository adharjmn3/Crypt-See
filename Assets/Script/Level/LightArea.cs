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

    [SerializeField] private Transform directionTransform; // Transform to define the light's direction

    void Awake()
    {
        // Get the Light2D and PolygonCollider2D components
        light2D = GetComponent<Light2D>();
        polygonCollider = GetComponent<PolygonCollider2D>();

        // Ensure the collider is set as a trigger
        polygonCollider.isTrigger = true;

        // Generate the polygon collider based on the light's properties
        GenerateLightArea();
    }

    void GenerateLightArea()
    {
        // Clear existing points
        polygonCollider.pathCount = 0;

        // Generate points for the outer radius
        List<Vector2> outerPoints = GenerateArcPoints(light2D.pointLightOuterRadius, light2D.pointLightOuterAngle);

        // Generate points for the inner radius (if applicable)
        List<Vector2> innerPoints = GenerateArcPoints(light2D.pointLightInnerRadius, light2D.pointLightInnerAngle, true);

        // Combine the outer and inner points to form the polygon
        List<Vector2> combinedPoints = new List<Vector2>(outerPoints);

        // Close the polygon by adding the reversed inner points
        if (innerPoints.Count > 0)
        {
            combinedPoints.AddRange(innerPoints);
            combinedPoints.Add(outerPoints[0]); // Close the loop
        }

        // Set the points to the collider
        polygonCollider.pathCount = 1;
        polygonCollider.SetPath(0, combinedPoints.ToArray());
    }

    List<Vector2> GenerateArcPoints(float radius, float angle, bool reverse = false)
    {
        List<Vector2> points = new List<Vector2>();

        // If the radius is zero, return an empty list
        if (radius <= 0)
        {
            return points;
        }

        // Determine the rotation based on the directionTransform or fallback to the object's rotation
        float lightRotation = directionTransform != null
            ? directionTransform.eulerAngles.z // Use directionTransform's Z rotation
            : transform.eulerAngles.z;         // Fallback to the object's Z rotation

        // Convert the rotation to radians
        float lightRotationRadians = Mathf.Deg2Rad * lightRotation;

        // Calculate the number of segments based on the angle
        int segments = Mathf.CeilToInt(angle / 5.0f); // Adjust segment size as needed
        float angleStep = angle / segments;

        // Generate points along the arc
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle / 2 + angleStep * i; // Angle relative to the light
            float radian = Mathf.Deg2Rad * currentAngle + lightRotationRadians; // Add light rotation

            Vector2 point = new Vector2(
                Mathf.Cos(radian) * radius,
                Mathf.Sin(radian) * radius
            );

            points.Add(point);
        }

        // Reverse the points if needed (for inner radius)
        if (reverse)
        {
            points.Reverse();
        }

        return points;
    }

    void OnValidate()
    {
        // Regenerate the light area when properties are changed in the editor
        if (light2D != null && polygonCollider != null)
        {
            GenerateLightArea();
        }
    }
}
