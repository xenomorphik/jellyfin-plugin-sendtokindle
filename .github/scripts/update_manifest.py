import json
import sys
import datetime
import os

manifest_path = sys.argv[1]
version_tag = sys.argv[2]  # e.g., v1.0.5
checksum = sys.argv[3]

# Strip 'v' from version and ensure it has 4 parts (e.g., 1.0.5 -> 1.0.5.0)
version_parts = version_tag.lstrip('v').split('.')
while len(version_parts) < 4:
    version_parts.append('0')
version = '.'.join(version_parts)

url = f"https://github.com/{os.environ['GITHUB_REPOSITORY']}/releases/download/{version_tag}/SendToKindle.zip"

with open(manifest_path, 'r') as f:
    data = json.load(f)

# Find the target abi from the previous version, fallback to a default
target_abi = "10.9.11.0"
if data and "versions" in data[0] and len(data[0]["versions"]) > 0:
    target_abi = data[0]["versions"][0].get("targetAbi", target_abi)

new_version = {
    "version": version,
    "changelog": f"Automated release for {version_tag}",
    "targetAbi": target_abi,
    "sourceUrl": url,
    "checksum": checksum,
    "timestamp": datetime.datetime.utcnow().isoformat().split('.')[0] + "Z"
}

# Prepend the new version
data[0].setdefault("versions", []).insert(0, new_version)

with open(manifest_path, 'w') as f:
    json.dump(data, f, indent=4)
    f.write('\n')

print(f"Manifest updated with version {version} and checksum {checksum}")
