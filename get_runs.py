import urllib.request
import json
import sys

url = "https://api.github.com/repos/siduko/unity-sandbox-director/actions/runs/22668714434/jobs"
req = urllib.request.Request(url, headers={'Accept': 'application/vnd.github.v3+json'})
try:
    with urllib.request.urlopen(req) as response:
        data = json.loads(response.read().decode())
        for job in data.get('jobs', []):
            print(f"Job: {job['name']}, Status: {job['status']}, Conclusion: {job['conclusion']}")
except Exception as e:
    print(f"Error: {e}")
