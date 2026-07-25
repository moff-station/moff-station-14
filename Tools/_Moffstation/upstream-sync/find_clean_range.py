#!/usr/bin/env python3
"""Find the last upstream commit that still merges cleanly into a base ref.

Binary-searches the range of not-yet-merged upstream commits using
`git merge-tree` trial merges (these only touch git's object database,
never the working tree or index, so they're safe to run repeatedly).

Assumes conflicts are monotonic along the upstream commit sequence: once a
commit in the range would conflict, later commits are treated as
conflicting too, and the binary search finds the boundary between the two.
This can occasionally be wrong (a later commit could coincidentally fix the
conflicting hunk) but is a reasonable trade-off against testing every
commit individually.

Prints two `key=value` lines in $GITHUB_OUTPUT format:
  clean_sha=<sha of the last upstream commit that merges cleanly, or empty>
  conflict_sha=<sha of the first commit that doesn't merge cleanly, or empty>
"""
import subprocess
import sys


def run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(args, capture_output=True, text=True)


def commits_between(base_ref: str, upstream_ref: str) -> list[str]:
    result = run("git", "rev-list", "--reverse", f"{base_ref}..{upstream_ref}")
    result.check_returncode()
    return [line for line in result.stdout.splitlines() if line]


def merges_cleanly(base_ref: str, sha: str) -> bool:
    return run("git", "merge-tree", "--write-tree", base_ref, sha).returncode == 0


def find_last_clean_index(base_ref: str, commits: list[str]) -> int:
    """Returns the index of the last clean commit, or -1 if none merge cleanly."""
    if merges_cleanly(base_ref, commits[-1]):
        return len(commits) - 1

    lo, hi, last_clean = 0, len(commits) - 1, -1
    while lo <= hi:
        mid = (lo + hi) // 2
        if merges_cleanly(base_ref, commits[mid]):
            last_clean = mid
            lo = mid + 1
        else:
            hi = mid - 1
    return last_clean


def main() -> None:
    if len(sys.argv) != 3:
        sys.exit("usage: find_clean_range.py <base-ref> <upstream-ref>")
    base_ref, upstream_ref = sys.argv[1], sys.argv[2]

    commits = commits_between(base_ref, upstream_ref)
    if not commits:
        print("clean_sha=")
        print("conflict_sha=")
        return

    last_clean = find_last_clean_index(base_ref, commits)

    print(f"clean_sha={commits[last_clean] if last_clean >= 0 else ''}")
    print(f"conflict_sha={commits[last_clean + 1] if last_clean < len(commits) - 1 else ''}")


if __name__ == "__main__":
    main()
