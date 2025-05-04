import os
import subprocess
import sys

def run_command(command):
    result = subprocess.run(command, shell=True)
    if result.returncode != 0:
        print(f"⚠️ Command failed: {command}")
        sys.exit(1)

# Ask for commit message
commit_message = input("📝 Enter commit message: ")

# Run Git commands
run_command("cd Documents/Unity_Proeject/Pj-1")
print("📂 Staging changes...")
run_command("git add .")

print("🧾 Committing changes...")
run_command(f'git commit -m "{commit_message}"')

print("🚀 Pushing to GitHub...")
run_command("git push")

print("✅ Done! Everything’s pushed.")
