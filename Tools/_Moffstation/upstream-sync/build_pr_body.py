#!/usr/bin/env python3
"""Write the body for the upstream-sync PR.

Must be run with HEAD checked out on the sync branch, after the clean range
has already been merged in (so `HEAD` is the real merge commit, and a
merge-tree trial against it reflects what a contributor would actually hit
next).

All commit/PR references link back to the upstream repo (not this one),
since that's the one guaranteed to have every referenced SHA and PR --
including the conflicting commit, which was fetched but never pushed here.
"""
import re
import subprocess
import sys

PR_REF_RE = re.compile(r"\(#(\d+)\)")


def run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(args, capture_output=True, text=True)


def commit_url(upstream_repo: str, sha: str) -> str:
    return f"https://redirect.github.com/{upstream_repo}/commit/{sha}"


def compare_url(upstream_repo: str, base_sha: str, head_sha: str) -> str:
    return f"https://redirect.github.com/{upstream_repo}/compare/{base_sha}...{head_sha}"


def linkify_pr_refs(subject: str, upstream_repo: str) -> str:
    """Turns a squash-merge subject's trailing "(#1234)" into a PR link."""
    return PR_REF_RE.sub(
        lambda m: f"([#{m.group(1)}](https://redirect.github.com/{upstream_repo}/pull/{m.group(1)}))",
        subject,
    )


def conflicting_files(conflict_sha: str) -> list[str]:
    """Names of the files that conflict between HEAD and conflict_sha.

    `git merge-tree --write-tree --name-only` prints the resulting tree's
    OID on the first line, then (on conflict) the conflicted paths, then a
    blank line, then human-readable messages.
    """
    result = run("git", "merge-tree", "--write-tree", "--name-only", "HEAD", conflict_sha)
    lines = result.stdout.splitlines()[1:]
    files = []
    for line in lines:
        if not line:
            break
        files.append(line)
    return files


MAX_LISTED_COMMITS = 250


def merged_commit_lines(base_ref: str, clean_sha: str, upstream_repo: str) -> list[str]:
    """Bullet list of merged commits, capped so a large first-ever sync (or a
    long backlog) can't blow past GitHub's PR body size limit."""
    result = run("git", "log", "--reverse", "--format=%H\t%s", f"{base_ref}..{clean_sha}")
    entries = [line for line in result.stdout.splitlines() if line]

    lines = []
    for entry in entries[:MAX_LISTED_COMMITS]:
        sha, subject = entry.split("\t", 1)
        short = f"[`{sha[:7]}`]({commit_url(upstream_repo, sha)})"
        lines.append(f"- {short} {linkify_pr_refs(subject, upstream_repo)}")

    remaining = len(entries) - MAX_LISTED_COMMITS
    if remaining > 0:
        lines.append(f"- ... and {remaining} more (see the compare link above)")
    return lines


def main() -> None:
    if len(sys.argv) != 6:
        sys.exit(
            "usage: build_pr_body.py <base-ref> <clean-sha> <conflict-sha-or-empty> "
            "<upstream-repo> <out-file>"
        )
    base_ref, clean_sha, conflict_sha, upstream_repo, out_file = sys.argv[1:6]

    commit_count = run("git", "rev-list", "--count", f"{base_ref}..{clean_sha}").stdout.strip()
    merge_base = run("git", "merge-base", base_ref, clean_sha).stdout.strip()

    lines = [
        "# This is an automated PR!"
        "",
        f"- Upstream range merged: [`{merge_base[:7]}...{clean_sha[:7]}`]"
        f"({compare_url(upstream_repo, merge_base, clean_sha)})",
        f"- Commits: {commit_count}",
        "",
        "<details>",
        "<summary>Merged commits</summary>",
        "",
    ]
    lines += merged_commit_lines(base_ref, clean_sha, upstream_repo)
    lines += [
        "",
        "</details>",
        ""
    ]

    if conflict_sha:
        subject = linkify_pr_refs(run("git", "log", "-1", "--format=%s", conflict_sha).stdout.strip(), upstream_repo)
        author = run("git", "log", "-1", "--format=%an", conflict_sha).stdout.strip()
        lines += [
            "### Stopped before a commit with conflicts",
            "",
            "The next upstream commit does not merge cleanly:",
            "",
            f"- [`{conflict_sha[:7]}`]({commit_url(upstream_repo, conflict_sha)}) by {author}: {subject}",
            "",
            "Conflicting files:",
            "",
        ]
        lines += [f"- `{path}`" for path in conflicting_files(conflict_sha)]
    else:
        lines.append(f"This PR is fully caught up with upstream as of [`{clean_sha[:7]}`]({commit_url(upstream_repo, clean_sha)}).")

    lines += [
        "<hr/>"
        ""
        "This is an automated merge of upstream commits that apply cleanly. "
        "This PR will be updated by subsequent runs of the `Upstream Sync` workflow."
    ]

    with open(out_file, "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
