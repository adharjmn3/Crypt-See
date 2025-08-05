# Sound System Integration Guide

## Overview
The enhanced sound system provides realistic sound propagation for the ImprovedEnemyAI, enabling enemies to react to player actions like movement, gunshots, and environmental sounds.

## Setup Instructions

### 1. Scene Setup
1. Create an empty GameObject named "SoundManager"
2. Add the `SoundManager` script to it
3. The SoundManager will automatically register itself as a singleton

### 2. Player Movement Integration
- The `Sound.cs` script on your player should automatically work with the new system
- Make sure it references your `PlayerMovement` script correctly
- Configure the walking/running sound arrays in the inspector:
  - **Walking Sounds**: Quiet footstep audio clips
  - **Running Sounds**: Louder footstep audio clips

### 3. Weapon Integration
- Add the `WeaponSounds` script to your weapon GameObjects
- Configure audio clips for each weapon action:
  - **Gunshot Clips**: Various firing sounds
  - **Reload Clips**: Reload/magazine sounds  
  - **Weapon Switch Clips**: Equipment change sounds
  - **Impact Clips**: Bullet hit sounds

### 4. Enemy Setup
- Your enemies with `ImprovedEnemyAI` should automatically work
- Each enemy needs the `EnemyHearing` component (should already be attached)
- Fine-tune hearing sensitivity in the inspector:
  - **Movement Sensitivity**: How well they hear footsteps (0.5-1.0)
  - **Gunshot Sensitivity**: How well they hear weapons (0.8-1.2)
  - **Environment Sensitivity**: How well they hear other sounds (0.3-0.8)

## Sound Types and Usage

### Movement Sounds
```csharp
// Automatically handled by Sound.cs based on player speed
// Walking: speed < walkingThreshold
// Running: speed >= walkingThreshold
```

### Weapon Sounds
```csharp
// In your shooting script:
WeaponSounds weaponSounds = GetComponent<WeaponSounds>();

// When firing
weaponSounds.PlayGunshotSound();

// When reloading
weaponSounds.PlayReloadSound();

// When switching weapons
weaponSounds.PlayWeaponSwitchSound();

// When bullets hit
weaponSounds.PlayImpactSound();
```

### Custom Sounds
```csharp
// For other sound events:
SoundManager.Instance.BroadcastSound(
    position,           // Vector3: Where the sound occurred
    intensity,          // float: How loud (0.0-2.0+)
    SoundType.Environment, // SoundType: Category of sound
    sourceObject        // GameObject: What made the sound (optional)
);
```

## Available Sound Types
- **Movement**: Footsteps, walking, running
- **Gunshot**: Weapon firing
- **Reload**: Weapon reloading
- **WeaponSwitch**: Changing weapons
- **Impact**: Bullets hitting surfaces
- **Environment**: Doors, objects, etc.
- **Conversation**: NPCs talking
- **Explosion**: Grenades, explosives

## Intensity Guidelines
- **0.1-0.3**: Very quiet (breathing, cloth rustling)
- **0.3-0.6**: Quiet (careful footsteps, whispering)
- **0.6-1.0**: Normal (regular footsteps, talking)
- **1.0-1.5**: Loud (running, shouting, gunshots)
- **1.5+**: Very loud (explosions, alarms)

## Distance Falloff
Sound intensity decreases with distance:
- **Full intensity**: 0-2 units
- **Linear falloff**: 2-20 units
- **No effect**: 20+ units

## Hearing Sensitivity
Each enemy can have different sensitivity to sound types:
- **Movement**: 0.5 = half sensitivity to footsteps
- **Gunshot**: 1.2 = 20% more sensitive to weapons
- **Environment**: 0.3 = less sensitive to ambient sounds

## Debugging
Enable debug visualization in SoundManager to see:
- Sound propagation spheres (red = high intensity, blue = low intensity)
- Enemy hearing ranges
- Sound source locations

## Integration with ImprovedEnemyAI
The AI automatically receives sound information and uses it for:
- **Alertness**: Nearby sounds increase awareness
- **Investigation**: AI moves toward interesting sounds
- **Combat**: Gunshots trigger aggressive behavior
- **Learning**: ML-Agents trains on sound-based decisions

## Performance Notes
- Sound events are processed efficiently using cached enemy lists
- Debug visualization can be disabled in builds
- Sound propagation uses simple distance calculations for performance

## Troubleshooting

### Enemies not responding to sounds:
1. Check that SoundManager exists in scene
2. Verify EnemyHearing component is attached
3. Ensure sensitivity values are > 0
4. Check debug visualization to see if sounds are being broadcast

### Sounds too loud/quiet:
1. Adjust intensity values in sound scripts
2. Modify sensitivity values on enemy hearing
3. Check distance falloff calculations

### Performance issues:
1. Disable debug visualization
2. Reduce number of sound events per frame
3. Consider culling distant enemies from sound processing
