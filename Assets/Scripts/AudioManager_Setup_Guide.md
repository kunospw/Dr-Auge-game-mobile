# AudioManager Setup Guide

## Steps to Set Up AudioManager in Unity:

### 1. Create AudioManager GameObject
1. In your Unity scene, create a new empty GameObject
2. Rename it to "AudioManager"
3. Add the `AudioManager.cs` script to this GameObject

**⚠️ IMPORTANT:** 
- **DO NOT** manually add AudioSource components to the AudioManager GameObject
- The AudioManager script will **automatically create** AudioSource components for each sound when the game starts
- You only need the empty GameObject with the AudioManager script attached

### 2. Configure Audio Clips
In the AudioManager component inspector, you'll see a "Sounds" array. Set it up with these entries:

**Array Size: 7**

**Element 0 - Main Menu Music:**
- Name: "Main Menu"
- Clip: Drag your "Main Menu" audio clip here
- Volume: 0.7
- Pitch: 1
- Loop: ✓ (checked)

**Element 1 - Gameplay Music:**
- Name: "Gameplay" 
- Clip: Drag your "Gameplay" audio clip here
- Volume: 0.5
- Pitch: 1
- Loop: ✓ (checked)

**Element 2 - Damage Sound:**
- Name: "Damage"
- Clip: Drag your "Damage" audio clip here
- Volume: 0.8
- Pitch: 1
- Loop: ✗ (unchecked)

**Element 3 - Game Complete:**
- Name: "Game Complete"
- Clip: Drag your "Game Complete" audio clip here
- Volume: 0.9
- Pitch: 1
- Loop: ✗ (unchecked)

**Element 4 - Game Over:**
- Name: "Game Over"
- Clip: Drag your "Game Over" audio clip here
- Volume: 0.8
- Pitch: 1
- Loop: ✗ (unchecked)

**Element 5 - Jump Sound:**
- Name: "Jump"
- Clip: Drag your "Jump" audio clip here
- Volume: 0.6
- Pitch: 1
- Loop: ✗ (unchecked)

**Element 6 - Multiply Door:**
- Name: "Multiply Door"
- Clip: Drag your "Multiply Door" audio clip here
- Volume: 0.7
- Pitch: 1
- Loop: ✗ (unchecked)

### 3. Configure Volume Settings
In the AudioManager inspector:
- **Master Volume:** 1.0
- **SFX Volume:** 1.0  
- **Music Volume:** 0.8

### 4. Configure Music Settings
- **Play Music On Start:** ✓ (checked)
- **Background Music Name:** "Main Menu"

### 5. Important Notes
- Make sure the AudioManager GameObject is in your scene at startup
- The AudioManager will persist across scene loads (DontDestroyOnLoad)
- Audio will automatically transition between Main Menu and Gameplay music
- All sound effects are integrated with game events (damage, jumping, gates, etc.)
- **AudioSource components are created automatically** - you don't add them manually
- Each sound in the array gets its own AudioSource component when the game starts

### 6. Testing
- Play the scene
- You should hear Main Menu music on startup
- When you start the game, it should transition to Gameplay music
- Test jumping, hitting gates, taking damage, game over, and winning to hear sound effects

### 7. Optional: Volume Controls
If you want to add volume sliders in your UI, you can call these methods:
- `AudioManager.Instance.SetMasterVolume(float value)`
- `AudioManager.Instance.SetSFXVolume(float value)`
- `AudioManager.Instance.SetMusicVolume(float value)`

## Troubleshooting
- If sounds don't play, check that the audio clips are assigned correctly
- Make sure the sound names in the script match exactly (case-sensitive)
- Check that the AudioManager GameObject exists in the scene
- Verify that audio clips are imported correctly (not compressed if quality is important)
