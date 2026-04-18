#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Discord Server Setup Script for Director Simulator: Scene Builder
Automatically creates channels, roles, and configures the server.
"""

import requests
import json
import time
import sys
import io

# Fix encoding for Windows
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Configuration
# NOTE: Set these environment variables before running:
# export DISCORD_BOT_TOKEN="your-bot-token-here"
# export DISCORD_SERVER_ID="your-server-id-here"
import os
BOT_TOKEN = os.getenv("DISCORD_BOT_TOKEN", "")
SERVER_ID = os.getenv("DISCORD_SERVER_ID", "")

if not BOT_TOKEN or not SERVER_ID:
    print("ERROR: DISCORD_BOT_TOKEN and DISCORD_SERVER_ID environment variables must be set!")
    sys.exit(1)
BASE_URL = "https://discord.com/api/v10"
HEADERS = {
    "Authorization": f"Bot {BOT_TOKEN}",
    "Content-Type": "application/json"
}

# Color codes
COLORS = {
    "director": 0xE74C3C,
    "contributor": 0x3498DB,
    "filmmaker": 0x2ECC71,
    "new_member": 0x95A5A6
}

# Channel structure
CHANNELS = {
    "WELCOME & INFO": [
        {"name": "welcome", "type": 0, "topic": "Chao mung den voi Director Simulator! Gioi thieu ban than tai day"},
        {"name": "rules", "type": 0, "topic": "Code of Conduct va quy tac server"},
        {"name": "announcements", "type": 5, "topic": "Updates, releases, va events chinh thuc"},
        {"name": "roadmap", "type": 0, "topic": "Public roadmap va voting features"}
    ],
    "COMMUNITY": [
        {"name": "general", "type": 0, "topic": "Casual chat ve filmmaking, Unity, va cuoc song"},
        {"name": "showcase", "type": 0, "topic": "Share your scenes, screenshots, va videos"},
        {"name": "feedback", "type": 0, "topic": "Get constructive feedback on your work"},
        {"name": "scene-challenges", "type": 15, "topic": "Monthly themed challenges"}
    ],
    "DEVELOPMENT": [
        {"name": "devlog", "type": 0, "topic": "Development updates va behind-the-scenes"},
        {"name": "bug-reports", "type": 15, "topic": "Report bugs here"},
        {"name": "feature-requests", "type": 15, "topic": "Suggest new features va improvements"},
        {"name": "contributions", "type": 0, "topic": "Discuss PRs, good-first-issues"}
    ],
    "HELP & LEARNING": [
        {"name": "help", "type": 15, "topic": "Ask questions, troubleshooting, va support"},
        {"name": "tutorials", "type": 0, "topic": "Share tutorials, tips & tricks"},
        {"name": "resources", "type": 0, "topic": "Useful links, assets, tools"}
    ],
    "VOICE": [
        {"name": "General Voice", "type": 2},
        {"name": "Co-Direct Session", "type": 2}
    ]
}

def create_role(name, color, permissions=0):
    """Create a role"""
    url = f"{BASE_URL}/guilds/{SERVER_ID}/roles"
    payload = {
        "name": name,
        "color": color,
        "permissions": str(permissions),
        "hoist": True,
        "mentionable": True
    }
    response = requests.post(url, headers=HEADERS, json=payload)
    if response.status_code == 201:
        print(f"[OK] Created role: {name}")
        return response.json()
    else:
        print(f"[ERROR] Failed to create role {name}: {response.text}")
        return None

def create_category(name):
    """Create a category"""
    url = f"{BASE_URL}/guilds/{SERVER_ID}/channels"
    payload = {
        "name": name,
        "type": 4
    }
    response = requests.post(url, headers=HEADERS, json=payload)
    if response.status_code == 201:
        print(f"[OK] Created category: {name}")
        return response.json()["id"]
    else:
        print(f"[ERROR] Failed to create category {name}: {response.text}")
        return None

def create_channel(name, channel_type, parent_id, topic=""):
    """Create a channel"""
    url = f"{BASE_URL}/guilds/{SERVER_ID}/channels"
    payload = {
        "name": name,
        "type": channel_type,
        "parent_id": parent_id,
        "topic": topic
    }
    response = requests.post(url, headers=HEADERS, json=payload)
    if response.status_code == 201:
        print(f"  [OK] Created channel: #{name}")
        return response.json()["id"]
    else:
        print(f"  [ERROR] Failed to create channel {name}: {response.text}")
        return None

def send_message(channel_id, content):
    """Send a message to a channel"""
    url = f"{BASE_URL}/channels/{channel_id}/messages"
    payload = {"content": content}
    response = requests.post(url, headers=HEADERS, json=payload)
    if response.status_code == 200:
        return response.json()["id"]
    else:
        print(f"[ERROR] Failed to send message: {response.text}")
        return None

def pin_message(channel_id, message_id):
    """Pin a message"""
    url = f"{BASE_URL}/channels/{channel_id}/pins/{message_id}"
    response = requests.put(url, headers=HEADERS)
    if response.status_code == 204:
        print(f"  [OK] Pinned message")
    else:
        print(f"  [ERROR] Failed to pin message: {response.text}")

def main():
    print("Setting up Director Simulator Discord Server...\n")
    
    # Step 1: Create Roles
    print("Creating roles...")
    roles = {}
    roles["director"] = create_role("@Director", COLORS["director"], 8)
    roles["contributor"] = create_role("@Contributor", COLORS["contributor"], 0)
    roles["filmmaker"] = create_role("@Filmmaker", COLORS["filmmaker"], 0)
    roles["new_member"] = create_role("@New Member", COLORS["new_member"], 0)
    print()
    
    # Step 2: Create Categories and Channels
    print("Creating categories and channels...")
    channel_ids = {}
    for category_name, channels in CHANNELS.items():
        category_id = create_category(category_name)
        if category_id:
            for channel in channels:
                channel_id = create_channel(
                    channel["name"],
                    channel["type"],
                    category_id,
                    channel.get("topic", "")
                )
                if channel_id:
                    channel_ids[channel["name"]] = channel_id
            time.sleep(1)
    print()
    
    # Step 3: Send Welcome Messages
    print("Sending welcome messages...")
    
    if "welcome" in channel_ids:
        welcome_msg = """Welcome to Director Simulator: Scene Builder!

You've just joined a community of filmmakers, game-makers, and creators building cinematic scenes in Unity.

What is Director Simulator?
A sandbox tool in Unity where you can:
- Build environments with modular props
- Direct actors and animations
- Control cameras and cinematic effects
- Record and export your scenes
- Share with the community

Getting Started (5 minutes):
1. Read the rules in #rules
2. Introduce yourself in #welcome
3. Check tutorials in #tutorials
4. Share your first scene in #showcase

Helpful Channels:
Community: #general, #showcase, #feedback, #scene-challenges
Development: #devlog, #bug-reports, #feature-requests, #contributions
Learning: #help, #tutorials, #resources

Want to Contribute?
We're looking for artists, programmers, writers, and content creators!
Check #contributions for good-first-issues.

Quick Links:
GitHub: https://github.com/siduko/unity-sandbox-director
Docs: https://github.com/siduko/unity-sandbox-director/tree/main/docs

Questions? Ask in #help - we're here to help!

Let's create something amazing together!"""
        msg_id = send_message(channel_ids["welcome"], welcome_msg)
        if msg_id:
            pin_message(channel_ids["welcome"], msg_id)
    
    if "announcements" in channel_ids:
        announce_msg = "Server is live! Welcome to Director Simulator: Scene Builder community!\n\nJoin #showcase to share your first scene and check #devlog for updates."
        send_message(channel_ids["announcements"], announce_msg)
    
    print()
    print("Server setup complete!")
    print(f"\nSummary:")
    print(f"  Roles created: {len(roles)}")
    print(f"  Channels created: {len(channel_ids)}")
    print(f"  Messages sent: 2")
    print(f"\nYour Discord server is ready!")

if __name__ == "__main__":
    main()
