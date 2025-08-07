#!/usr/bin/env python3

# By Southbridge, feel free to reuse this in your own branch if you want. It's my finest shitcode.

from os import path
import re
import json
import yaml
try:
    from yaml import CLoader as Loader, CDumper as Dumper
except ImportError:
    from yaml import Loader, Dumper
import traceback
import subprocess # For gh usage, needs to be installed.
from dateutil.parser import isoparse # requires dateutil

repo_url = "https://github.com/moff-station/moff-station-14.git"
changelog_filepath = "Resources/Changelog/Mofflog.yml"
time_format = f'%Y-%m-%dT%H:%M:%S.000'
changelog_entries_to_pull = 500

changelog_section_regex = re.compile(r"(?::cl:|🆑)\s*(?P<author>[\w\s,]*?)?\s*$\s*(?P<changelogs>.*)\s*", re.DOTALL | re.MULTILINE)
changelog_entry_regex = re.compile(r"\s*-\s*(?P<type>add|remove|tweak|fix)[:]\s*(?P<message>.*?)\s*$", re.IGNORECASE | re.MULTILINE)

class ChangelogEntry(dict):
    def __hash__(self):
        return self["id"]

def get_prs():
    proc = subprocess.run(
        [
            "gh",
            "pr",
            "list",
            "--state", "merged",
            "--limit", str(changelog_entries_to_pull),
            "--json", "author,title,url,mergedAt,body,number",
            "--repo", str(repo_url)
        ],
        capture_output = True
    )
    return json.loads(proc.stdout)


def parse_changelog(pr_dict):
    # Get body and remove comments
    body = re.sub(re.compile("<!--.*?-->", re.DOTALL | re.MULTILINE), "", pr_dict["body"])
    # parse for CL header, then grab everything after it
    changelog_slugs = re.search(changelog_section_regex, body)
    if changelog_slugs == None:
        print("Couldn't find changelog entires for #{0[number]}:{0[title]}".format(pr_dict))
        return None

    changelog_entry_dict = {
        "author" : changelog_slugs["author"],
        "changes" : []
    }

    # parse out each individual changelog messages
    for line in re.finditer(changelog_entry_regex, changelog_slugs["changelogs"]):
        if line is None or line["type"] == "" or line["message"] == "":
            print("Could not parse changelog entry: {0}".format(line))
            continue

        changelog_entry_dict["changes"].append(line.groupdict())

    return changelog_entry_dict


def get_all_changelogs(recent_prs):
    changelog_entries = []
    print("Extracting pr data...")
    for pr in recent_prs:
        changelogs = {
            "id" : pr["number"],
            "time" : isoparse(pr["mergedAt"]).strftime(time_format),
            "url": pr["url"]
        }
        try:
            changelog_entry = parse_changelog(pr)
            if changelog_entry is None:
                continue
        except Exception as e:
            print("Error:", traceback.format_exc())
            continue
        changelogs.update(changelog_entry)
        if changelogs["author"] == "":
            changelogs["author"] = pr["author"]["login"] if pr["author"]["name"] == "" else pr["author"]["name"]
        changelog_entries.append(changelogs)
    return changelog_entries


def update_changelog_file(changelog_entries):
    # load previous changelog if it exists
    changelog_file = {}

    try:
        with open(changelog_filepath, "r") as f:
            changelog_file = yaml.load(f, Loader=Loader)
    except Exception as e:
        print("Error:", traceback.format_exc())

    if changelog_file is None or changelog_file == {}:
        changelog_file = {
            "Name" : "Mofflog",
            "Order" : -2,
            "Entries" : []
        }

    # add current changelog entries
    prev_entries = changelog_file["Entries"]
    prev_len = len(prev_entries)
    prev_entries = changelog_entries + prev_entries

    # get unique ones
    prev_entries = [ ChangelogEntry(i) for i in prev_entries ] # make them hashable
    prev_entries = sorted(list(set(prev_entries)), key=lambda x: x["id"])
    prev_entries = [ dict(i) for i in prev_entries ]

    # write them
    changelog_file["Entries"] = prev_entries
    yaml_dump = yaml.dump(changelog_file, Dumper=Dumper)

    if yaml_dump != "":
        with open(changelog_filepath, "w") as f:
            f.write(yaml_dump)
    return len(prev_entries) - prev_len


def main():
    print("Starting Moffstation changelog process...")
    recent_prs = get_prs()
    print("Got {0} recent prs, from #{1[number]}:{1[title]} to #{2[number]}:{2[title]}.".format(len(recent_prs), recent_prs[-1], recent_prs[0]))
    changelog_entries = get_all_changelogs(recent_prs)
    print("Parsed changelog data, loaded {0} entries.".format(len(changelog_entries)))
    entry_count = update_changelog_file(changelog_entries)
    print("Wrote {0} new entries to {1}.".format(entry_count, changelog_filepath))

if __name__ == "__main__":
    main()
