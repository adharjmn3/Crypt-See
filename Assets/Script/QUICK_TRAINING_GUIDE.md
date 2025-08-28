# Quick Training Setup Guide

## Step-by-Step Unity Setup (5 minutes)

### 1. Prepare Your ImprovedEnemyAI GameObject
```
In Unity Inspector:
├── ImprovedEnemyAI (Script)
│   ├── Behavior Name: "EnemyAI"
│   ├── Max Step: 5000
│   └── All component references assigned
├── Decision Requester (Add Component)
│   └── Decision Period: 5
├── NavMeshAgent (already required)
├── EnemyStats, EnemyShoot, EnemyVision, EnemyHearing (already required)
└── Awareness (optional but recommended)
```

### 2. Create Training Scene
1. **Duplicate your current scene** → Save as "EnemyAI_Training"
2. **Add Training Manager**:
   - Create empty GameObject → "TrainingManager"
   - Add EnemyAITrainingManager script
3. **Setup Multiple Agents** (for faster training):
   - Duplicate your enemy 3-5 times
   - Spread them around the level
   - Each needs all the same components

### 3. Verify Scene Setup
- [ ] Player GameObject exists with "Player" tag
- [ ] NavMesh baked and covers training area
- [ ] All agents have "EnemyAI" behavior name
- [ ] TrainingManager configured

### 4. Test Before Training
1. **Play Scene** → Check console for errors
2. **Enable Heuristic** → Test with WASD + Space
3. **Verify Agents Reset** → They should respawn/reset properly

## Quick Training Commands

### Start Training (from Unity project root):
```bash
# First time training
mlagents-learn Assets/Script/trainer_config.yaml --run-id=EnemyAI_v1

# Continue previous training
mlagents-learn Assets/Script/trainer_config.yaml --run-id=EnemyAI_v1 --resume

# Force overwrite previous
mlagents-learn Assets/Script/trainer_config.yaml --run-id=EnemyAI_v1 --force
```

### Monitor Training:
```bash
# In separate terminal
tensorboard --logdir results
# Then open: http://localhost:6006
```

## What to Expect

### Training Phases (based on curriculum):
1. **Easy Phase** (0-500k steps): Learning basic movement and detection
2. **Medium Phase** (500k-1.5M steps): Learning combat positioning  
3. **Hard Phase** (1.5M-2M steps): Learning advanced tactics

### Progress Indicators:
- **Episode Length**: Should increase over time (agents survive longer)
- **Cumulative Reward**: Should trend upward
- **Mean Reward**: Should stabilize and improve
- **Episodes**: Should complete successfully

### Training Time Estimates:
- **Basic competence**: 1-2 hours
- **Good performance**: 4-6 hours  
- **Professional level**: 8-12 hours

## Troubleshooting During Training

### If agents aren't learning:
1. Check TensorBoard - is reward changing?
2. Verify observations aren't NaN
3. Ensure episodes reset properly
4. Check if agents are getting stuck

### If training is too slow:
1. Reduce number of agents
2. Increase Decision Period to 10
3. Reduce Max Steps to 3000
4. Use faster hardware

### If Unity crashes:
1. Reduce batch size in config
2. Close other applications
3. Save scene before training
4. Monitor memory usage

## Ready to Train?

Once you've set up the training scene with the checklist above, you can start training and mostly let it run automatically. The ML-Agents system will handle the learning process, and you can monitor progress through TensorBoard.

The trained model will be saved in the `results/EnemyAI_v1/` folder as `.onnx` files that you can then use in your game!
