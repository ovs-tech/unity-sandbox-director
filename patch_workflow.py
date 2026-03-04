import re

with open('.github/workflows/unity-builder.yml', 'r') as f:
    content = f.read()

fix_step = """
      - name: Fix package resolution errors
        run: |
          rm -f Packages/packages-lock.json
          jq 'del(.dependencies["com.unity.ai.toolkit"], .dependencies["com.unity.ai.assistant"], .dependencies["com.unity.ai.generators"], .dependencies["com.unity.ai.inference"], .dependencies["com.unity.modules.adaptiveperformance"])' Packages/manifest.json > temp.json && mv temp.json Packages/manifest.json
"""

# We want to insert this right after checkout in both test and build jobs.
# Look for:
#       - name: Checkout repository
#         uses: actions/checkout@v4
#         with:
#           lfs: true
#

pattern = r"(      - name: Checkout repository\n        uses: actions/checkout@v4\n        with:\n          lfs: true\n)"

new_content = re.sub(pattern, r"\1" + fix_step, content)

with open('.github/workflows/unity-builder.yml', 'w') as f:
    f.write(new_content)

print("Patched.")
