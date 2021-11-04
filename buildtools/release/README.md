# Toolkit release manifests

This folder contains content relevant to:
* preparing a commit for release
* publishing the Toolkit into the Marketplace
* producing a GitHub tagged release

Marketplace manifests are specific to each Toolkit extension. These reside in folders named by their associated Visual Studio version, and the folders are referenced by the release pipeline.

* `v15`: Toolkit for VS 2017 & 2019
* `v17`: Toolkit for VS 2022

### Setup
- Install Python 3.9+ (32-bit)
  - Script was tested with 3.9.6
  - The 64-bit version has known issues on Windows with some libraries (e.g PyNaCl)
- Check that Python is correctly in your PATH by running `python` in a new shell

### Usage
Run the following with Developer Command Prompt or PowerShell for VS 2019 in the root of the repository:
```
msbuild buildtools\build.proj /t:queueRelease
```

### Troubleshooting
- Script fails to start with "'foo' is not a valid Win32 application in Python"
  - You may be using 64-bit Python. Try 32-bit instead
- Push to GitHub fails
  - Check if you have 2FA enabled for GitHub. If so, you must use a PAT to login instead of username/password
  - See [here](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token) for help with creating a token. The token needs 'repo' permissions.
- Script fails before showing commits
  - Check the error message. It's likely that the release branch is out of sync (squash merge, botched release, etc.) and will need to be cleaned up.
- Creating PR fails
  - Credentials may be incorrectly configured. Reaching this stage means the branch has already been pushed to the remote. You can create the PR manually from a `release/candidate/$VERSION` branch instead.

### Advanced
- The script does _not_ need to be ran from `msbuild`. You can run it from the repository root in a developer shell. Keep in mind that you'll need to pass in parameters that the build task would normally do.
  - `release.ps1` contains the additional parameters being passed in
- You can easily use older versions of the script by simply checking out a commit containing the older version. The script does not care about from which commit it is ran from since it always fetches from remote. 
  - This means you can also edit the script on-the-fly if things breaks. Checkout a branch and make changes (be sure to commit them!), then run as normal.
  - Keep in mind that while the Python script is independent of the commit, downstream scripts are not. For example, the script will _always_ execute the `createRelease` task from the latest commit.

### Testing
Testing this script must currently be done manually. The easiest way is to create a fork off the main repo, then run the script there. Be sure to test the script all the way to the end! You should end up with a PR against the release branch with the `config.json` file correctly updated and release notes adjusted.


