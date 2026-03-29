# Git Branch Strategy - Jcamp FxScalper Project

## Current Branches

### `master`
- **Purpose**: Main/stable codebase
- **Contains**: Original JCAMP_FxScalper.cs (MT5/cTrader EA)
- **Status**: Stable, production-ready code

### `strategy/jcamp-1m-3rr-scalping` ⭐ (ACTIVE)
- **Purpose**: New scalping strategy targeting 3:1+ risk-reward
- **Contains**:
  - TrendModeRectangleIndicator.cs (M15 swing visualization)
  - Jcamp_1M_scalping.cs (Trading bot)
  - Documentation and guides
- **Target**: Build profitable 3:1 RR scalping system
- **Status**: Foundation complete, ready for incremental development

## Branch Workflow

### Current Branch
```bash
git branch
# * strategy/jcamp-1m-3rr-scalping  ← You are here
#   master
```

### Switching Branches

**Go back to master:**
```bash
git checkout master
```

**Return to scalping strategy:**
```bash
git checkout strategy/jcamp-1m-3rr-scalping
```

### Committing Changes (In This Branch)

As you develop features:
```bash
# 1. Check what changed
git status

# 2. Add changed files
git add Jcamp_1M_scalping.cs

# 3. Commit with descriptive message
git commit -m "Add trailing stop logic to cBot"

# 4. Push to remote (when ready)
git push -u origin strategy/jcamp-1m-3rr-scalping
```

## Development Phases (This Branch)

### ✅ Phase 1: Foundation (COMPLETE)
- [x] Create TrendModeRectangleIndicator
- [x] Create basic Jcamp_1M_scalping cBot
- [x] Verify swing detection works
- [x] Test indicator on M1 and M15 charts

### 🔄 Phase 2: Optimize for 3:1 RR (NEXT)
- [ ] Adjust SL/TP calculations for 3:1 ratio
- [ ] Add ATR-based dynamic SL/TP
- [ ] Optimize entry timing (early/late in zone)
- [ ] Backtest and measure actual RR achieved

### 📋 Phase 3: Risk Management
- [ ] Add break-even logic
- [ ] Add trailing stop
- [ ] Add partial profit taking
- [ ] Add max daily loss/profit limits

### 🎯 Phase 4: Advanced Filters
- [ ] Time of day filter
- [ ] Spread filter
- [ ] Volatility filter (ATR-based)
- [ ] Multi-swing tracking

### 🚀 Phase 5: Optimization & Live
- [ ] Parameter optimization via backtesting
- [ ] Walk-forward analysis
- [ ] Forward testing on demo
- [ ] Live deployment

## Why Separate Branch?

### Benefits:
1. **Independent Development**: Build new strategy without affecting master
2. **Easy Testing**: Compare performance against original EA
3. **Clean History**: Track all changes specific to this strategy
4. **Safe Experimentation**: Try new ideas without risk to main code
5. **Merge Later**: If successful, can merge features back to master

### Master Branch Stays Clean:
- Original JCAMP_FxScalper.cs unchanged
- Can still fix bugs or update original EA
- Can create other strategy branches later

## Files in This Branch

### Strategy Files (New)
```
TrendModeRectangleIndicator.cs    - Visual swing rectangle indicator
Jcamp_1M_scalping.cs              - Trading bot (basic version)
JCAMP_1M_SCALPING_README.md       - Strategy documentation
BRANCH_STRATEGY.md                - This file (branch guide)
```

### Shared/Original Files (From Master)
```
JCAMP_FxScalper.cs                - Original EA (reference only)
debug/                            - Debug files and screenshots
```

## Next Steps

1. **Test the basic cBot** in cTrader backtest
2. **Verify** swing detection matches indicator
3. **Measure** current win rate and RR
4. **Optimize** SL/TP for 3:1 RR target
5. **Commit** each improvement to this branch

## Merging Strategy (Future)

### When Strategy is Profitable:
```bash
# 1. Switch to master
git checkout master

# 2. Merge strategy branch
git merge strategy/jcamp-1m-3rr-scalping

# 3. Resolve conflicts if any
# 4. Push to remote
git push origin master
```

### Or Keep Separate:
- Master = Original EA
- This Branch = New scalping strategy
- Both maintained independently

## Commands Cheat Sheet

```bash
# Check current branch
git branch

# Switch to scalping strategy
git checkout strategy/jcamp-1m-3rr-scalping

# Switch to master
git checkout master

# See changes
git status

# Commit changes
git add <file>
git commit -m "Description"

# View commit history
git log --oneline

# Push branch to remote
git push -u origin strategy/jcamp-1m-3rr-scalping
```

## Notes

- This branch targets **3:1 risk-reward ratio**
- Start with foundation, build incrementally
- Each phase should be tested and verified
- Commit frequently with clear messages
- Keep master branch stable
