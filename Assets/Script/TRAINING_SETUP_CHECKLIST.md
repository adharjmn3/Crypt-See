# ML-Agents Training Setup Checklist

## 1. Scene Configuration

### Training Scene Requirements:
- [ ] Create a dedicated training scene (duplicate your main scene)
- [ ] Add multiple spawn points for agents (5-10 agents train faster)
- [ ] Add player spawn points for variety
- [ ] Ensure NavMesh covers the entire training area
- [ ] Add boundaries to prevent agents from falling off

### Agent Setup:
- [ ] Set Behavior Name in ImprovedEnemyAI: "EnemyAI" (must match config)
- [ ] Set Max Step to reasonable value (5000-10000 steps)
- [ ] Add Decision Requester component (Decision Period: 5-10)
- [ ] Ensure all required components are attached

### Player Setup:
- [ ] Player GameObject with "Player" tag
- [ ] PlayerController for movement (can be simple)
- [ ] Optional: Multiple player behavior scripts for variety

## 2. Training Environment Manager

### EnemyAITrainingManager Setup:
- [ ] Attach to empty GameObject in scene
- [ ] Configure curriculum settings
- [ ] Set up scenario randomization
- [ ] Add episode reset logic

## 3. Configuration Files

### trainer_config.yaml:
- [ ] Correct behavior name matches Unity
- [ ] Appropriate hyperparameters
- [ ] Curriculum learning enabled
- [ ] Checkpoint saving enabled

## 4. Training Monitoring

### During Training:
- [ ] TensorBoard for monitoring progress
- [ ] Episode length tracking
- [ ] Reward progression monitoring
- [ ] Model checkpoints saving

## 5. Testing Setup

### Before Full Training:
- [ ] Test Heuristic mode works
- [ ] Verify all observations are valid
- [ ] Check action space responds correctly
- [ ] Ensure episodes reset properly

## 6. Hardware Considerations

### Training Requirements:
- [ ] Good GPU for faster training (recommended)
- [ ] Sufficient RAM (8GB+ recommended)
- [ ] SSD for faster I/O
- [ ] Stable power supply for long training

## 7. Training Process

### Command to Start:
```bash
mlagents-learn Assets/Script/trainer_config.yaml --run-id=EnemyAI_v1 --force
```

### Monitoring Commands:
```bash
tensorboard --logdir results
```

### Expected Training Time:
- Initial learning: 30 minutes - 2 hours
- Good performance: 4-8 hours
- Professional level: 12-24 hours

## 8. Common Issues to Watch For

### Training Problems:
- [ ] Reward not changing (check reward function)
- [ ] Episodes too short/long (adjust max steps)
- [ ] Agents getting stuck (check NavMesh)
- [ ] NaN values (check observations)
- [ ] No learning progress (adjust hyperparameters)

### Performance Issues:
- [ ] Training too slow (reduce agents or complexity)
- [ ] Memory usage too high (reduce batch size)
- [ ] Crashes during training (check stability)
