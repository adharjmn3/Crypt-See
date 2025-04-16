using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();

        // Get the Inventory script
        Inventory inventory = (Inventory)target;

        // Display the current weapon stats
        if (inventory.CurrentWeapon != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Weapon Stats", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", inventory.CurrentWeapon.weaponName);
            EditorGUILayout.LabelField("Ammo Type", inventory.CurrentWeapon.ammoType.ToString());
            EditorGUILayout.LabelField("Magazine Size", inventory.CurrentWeapon.magazineSize.ToString());
            EditorGUILayout.LabelField("Bullets in Magazine", inventory.CurrentWeapon.bulletsInMagazine.ToString());
            EditorGUILayout.LabelField("Total Ammo", inventory.CurrentWeapon.totalAmmo.ToString());
            EditorGUILayout.LabelField("Damage", inventory.CurrentWeapon.damage.ToString());
            EditorGUILayout.LabelField("Fire Rate", inventory.CurrentWeapon.fireRate.ToString());
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Weapon Stats", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("No weapon equipped.");
        }
    }
}