# Cleanup Steps for Odin Serialization Refactor

## Files to Delete

The following files need to be manually deleted from the project:

### 1. Legacy DTO File
- **File**: `Assets/Scripts/MiniTimeline/Core/LegacyDtos.cs`
- **Meta File**: `Assets/Scripts/MiniTimeline/Core/LegacyDtos.cs.meta`
- **Reason**: Contains `TrackData` and `ClipData` classes that are no longer used after Odin serialization refactor

### 2. Legacy Factory File  
- **File**: `Assets/Scripts/MiniTimeline/Serialization/TrackFactory.cs`
- **Meta File**: `Assets/Scripts/MiniTimeline/Serialization/TrackFactory.cs.meta`
- **Reason**: Contains ~1200 lines of conversion logic between runtime objects and DTOs, no longer needed with direct Odin serialization

## Deletion Method

Use **Git** to delete these files to ensure Unity .meta files are properly removed:

```bash
cd /Users/er-lap-mpm4-023/Documents/Projects/externals/unity/unity-sandbox-director

# Delete the files using git
git rm Assets/Scripts/MiniTimeline/Core/LegacyDtos.cs
git rm Assets/Scripts/MiniTimeline/Serialization/TrackFactory.cs

# Commit the changes
git add -A
git commit -m "Remove legacy serialization files (TrackFactory and LegacyDtos)"
```

## Verification

After deletion:
1. Open Unity Editor
2. Wait for recompilation
3. Check for any compilation errors
4. Verify that no references to `TrackData` or `ClipData` exist (already confirmed - only comments remain)
5. Test project save/load functionality

## Status

- [x] Code refactor completed (all tracks/clips use Odin serialization)
- [x] Comment references updated in MiniTimelineProject.cs
- [ ] Files physically deleted (requires manual git operation)
- [ ] Unity Editor verification (requires Unity testing)
