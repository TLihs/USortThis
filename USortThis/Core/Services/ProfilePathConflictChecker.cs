using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    public enum ProfileConflictKind
    {
        None,
        WaitForCopyBeforeMove,
        Conflict
    }

    public sealed record ProfilePathConflict(ProfileConflictKind Kind, string Message);

    public static class ProfilePathConflictChecker
    {
        private sealed record Root(string Path, bool IsSource, bool MovesSource);

        public static ProfilePathConflict Check(ProfileItemModel candidate, ProfileItemModel running)
        {
            var candidateRoots = GetRoots(candidate);
            var runningRoots = GetRoots(running);
            var sharedSourceMoveConflict = false;

            foreach (var candidateRoot in candidateRoots)
            foreach (var runningRoot in runningRoots)
            {
                if (!PathsOverlap(candidateRoot.Path, runningRoot.Path))
                    continue;

                if (candidateRoot.IsSource && runningRoot.IsSource &&
                    PathsOverlap(candidateRoot.Path, runningRoot.Path))
                {
                    if (candidateRoot.MovesSource && !runningRoot.MovesSource)
                    {
                        sharedSourceMoveConflict = true;
                        continue;
                    }

                    if (!candidateRoot.MovesSource && runningRoot.MovesSource &&
                        running.Status == ProfileItemModel.ProfileStatus.WaitingForProfile)
                        continue;

                    if (!candidateRoot.MovesSource && !runningRoot.MovesSource)
                        continue;
                }

                return new ProfilePathConflict(
                    ProfileConflictKind.Conflict,
                    $"'{candidate.Name}' overlaps source or destination paths used by running profile '{running.Name}'. Change the paths or stop the other profile.");
            }

            return sharedSourceMoveConflict
                ? new ProfilePathConflict(ProfileConflictKind.WaitForCopyBeforeMove,
                    $"'{candidate.Name}' will wait for '{running.Name}' to stop before moving files from the shared source.")
                : new ProfilePathConflict(ProfileConflictKind.None, string.Empty);
        }

        public static bool PathsOverlap(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return false;

            try
            {
                var normalizedFirst = Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedSecond = Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase) ||
                    normalizedFirst.StartsWith(normalizedSecond + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    normalizedSecond.StartsWith(normalizedFirst + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static List<Root> GetRoots(ProfileItemModel profile)
        {
            return profile.GetFolderBridges()
                .SelectMany(bridge =>
                {
                    var roots = new List<Root>();
                    var source = bridge.SourceFolder?.FolderPath;
                    if (!string.IsNullOrWhiteSpace(source))
                        roots.Add(new Root(source, true, bridge.MoveSourceAfterCopy));
                    roots.AddRange(bridge.Routes
                        .Where(route => !string.IsNullOrWhiteSpace(route.DestinationPath))
                        .Select(route => new Root(route.DestinationPath, false, false)));
                    return roots;
                })
                .ToList();
        }
    }
}
