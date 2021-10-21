# Python 3.6 or higher 
import os
import time
import subprocess
import sys
import signal
import json
import argparse
import colorama
import git

from typing import Callable, Optional
from threading import Thread, Lock
from github.Repository import Repository
from termcolor import colored
from git.diff import Diff
from git.objects import Commit
from github import Github 

from prompt_toolkit import Application, prompt
from prompt_toolkit.key_binding.key_bindings import KeyBindings
from prompt_toolkit.layout.containers import Window
from prompt_toolkit.layout.controls import FormattedTextControl
from prompt_toolkit.formatted_text import to_formatted_text
from prompt_toolkit.layout import Layout
from prompt_toolkit.application.current import get_app
from prompt_toolkit.styles import Style
from prompt_toolkit.keys import Keys
from prompt_toolkit.formatted_text.utils import fragment_list_to_text


original_branch = None

def dim_text(text: str, color: str = "white") -> str:
    return f'{colorama.Style.DIM}{colored(text, color)}{colorama.Style.NORMAL}'

def bright_text(text: str, color: str = "white") -> str:
    return f'{colorama.Style.BRIGHT}{colored(text, color)}{colorama.Style.NORMAL}'

IS_WINDOWS = sys.platform.startswith('win')

if IS_WINDOWS:
    from ctypes import windll
    
    STD_OUTPUT_HANDLE = -11
    ENABLE_PROCESSED_OUTPUT = 1
    ENABLE_WRAP_AT_EOL_OUTPUT = 2
    ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4
    SETTINGS = ENABLE_PROCESSED_OUTPUT | ENABLE_WRAP_AT_EOL_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING

    kernel32 = windll.kernel32
    kernel32.SetConsoleMode(kernel32.GetStdHandle(STD_OUTPUT_HANDLE), SETTINGS)

# hmmmm
BLUE = 'cyan' if IS_WINDOWS else 'blue'
EOF_COMMAND = 'Ctrl-C' if IS_WINDOWS else 'Ctrl-D'

PR_BODY = '### This PR must be merged by merge commit'
CONFIG_NAME = 'config.json'
    
class Version:
    slots: int

    def __init__(self, version: str):
        is_number: Callable[[str], bool] = lambda part: part.isnumeric()
        conv_number: Callable[[str], int] = lambda s: int(s)

        self._version = list(map(conv_number, filter(is_number, version.split('.'))))

        if (len(self._version) != len(version.split('.'))):
            raise Exception('Version can only contain numbers')

        if (len(self._version) == 0):
            raise Exception('Empty version')

        self.slots = len(self._version)

    def __len__(self) -> int:
        return len(self._version)

    def __eq__ (self, other: 'Version') -> bool:
        padded_left = self._version + [0] * (len(other) - len(self))
        padded_right = other._version + [0] * (len(self) - len(other))
        return not other is None and padded_left == padded_right

    def __gt__(self, other: 'Version') -> bool:
        if other is None:
            return False

        return other < self and not other == self

    def __lt__(self, other: 'Version') -> bool:
        if other is None:
            return True

        for i in range(max(len(self._version), len(other._version))):
            v1 = self._version[i] if i < len(self._version) else 0
            v2 = other._version[i] if i < len(other._version) else 0

            if (v1 > v2):
                return False
            if (v1 < v2):
                return True

        return False

    def __le__(self, other: 'Version') -> bool:
        return self < other or self == other

    def __ge__(self, other: 'Version') -> bool:
        return not (self < other)

    def __str__(self) -> str:
        return '.'.join(map(str, self._version))

    def _reduce(self, pos: int):
        for i in range(pos, len(self._version)):
            self._version[i] = 0
    
    def bump(self, pos: int) -> 'Version':
        copy = self.copy()
        copy._version[pos] += 1
        copy._reduce(pos + 1)

        return copy

    def copy(self) -> 'Version':
        return Version(str(self))

    def bump_highlight(self, target: int) -> str:
        copy = self.copy().bump(target)

        def color(text: str, pos: int) -> str:
            if pos < target:
                return text
            elif pos > target:
                return colorama.Fore.RED + text
            else:
                return colorama.Fore.GREEN + text

        return '.'.join([f'{color(str(v), i)}' for i, v in enumerate(copy._version)]) + colorama.Style.RESET_ALL

class ReleaseConfig:
    version: Version
    commit: Commit
    notes: str
    path: str

    def __init__(self, path: str, repo: git.Repo):
        with open(path) as file:
            config = json.load(file)

        self._repo = repo
        self.path = path
        self.version = Version(config['version'])
        self.notes = config['notes']

        try:
            self.commit = repo.rev_parse(config['commit'])

            if self.commit.type != 'commit':
                raise Exception('not a commit')
        except:
            exit(reason='Bad commit in config. Does it exist on the development branch?')

    def __str__(self) -> str:
        out = ''
        out += f'{dim_text("Version:")} {bright_text(str(self.version), BLUE)}\n'
        out += f'{dim_text("Commit:")} {bright_text(self.commit.hexsha, BLUE)}\n'
        out += f'{dim_text("Notes:")}\n{bright_text(self.notes, BLUE)}\n' if self.notes != '' else ''

        return out

    def _jsonify(self) -> dict:
        return {
            'version': str(self.version),
            'commit': str(self.commit),
            'notes': self.notes,
        }

    def write(self, path: Optional[str] = None) -> str:
        path = self.path if path is None else path

        with open(path, 'w+') as file:
            json.dump(self._jsonify(), file, indent=4)

        return path

    def pre_validate(self, releaseBranch: Commit) -> str or None:
        if not self._repo.head.is_valid():
            return 'Repository head is not valid.'

        head: Commit = self._repo.head.commit

        if not is_reachable(releaseBranch, head):
            return 'Release branch is not reachable from development.'

        if not is_reachable(self.commit, releaseBranch):
            return 'Last release commit is not reachable from the release branch.'

        return None

class SelectionDialog():
    container_style: str = ""
    default_style: str = ""
    selected_style: str = "fg:ansibrightred"
    page_size = 10

    def __init__(self, values) -> None:
        self.values = values
        self.current_value = values[0][0]
        self._selected_index = 0
        self._search_str = ''

        kb = KeyBindings()

        @kb.add('up')
        def _up(event) -> None:
            self._selected_index = max(0, self._selected_index - 1)

        @kb.add('down')
        def _down(event) -> None:
            self._selected_index = min(len(self.values) - 1, self._selected_index + 1)

        @kb.add('enter')
        def _click(event) -> None:
            self.current_value = self.values[self._selected_index]
            get_app().exit(result=self.current_value[0])

        @kb.add('c-c')
        def _exit(event) -> None:
            get_app().exit(exception=KeyboardInterrupt())

        def search(target: str) -> bool:
            for value in values[self._selected_index + 1 :] + values:
                text = fragment_list_to_text(to_formatted_text(value[1])).lower()

                if text.startswith(target):
                    self._selected_index = self.values.index(value)
                    return True
            
            return False

        # keep building up keypresses -> if no match then dump buffer
        @kb.add(Keys.Any)
        def _find(event) -> None:
            self._search_str += event.data.lower()

            if not search(self._search_str):
                self._search_str = event.data.lower()
                search(self._search_str)

        @kb.add('c-v')
        def _paste(event) -> None:
            search(event.app.clipboard.get_data().text)

        self.control = FormattedTextControl(
            self._get_text_fragments, key_bindings=kb, focusable=True, show_cursor=False
        )

        self.window = Window(
            content=self.control,
            style=self.container_style,
            dont_extend_height=True,
            height=self.page_size,
        )

    def _get_text_fragments(self):
        result = []
        for i, value in enumerate(self.values):
            selected = i == self._selected_index

            result.append((self.default_style, ' '))

            if selected:
                result.append(('[SetCursorPosition]', ''))
                result.extend(to_formatted_text(value[1], style=self.selected_style))
            else:
                result.extend(to_formatted_text(value[1], style=self.default_style))

            result.append(('', '\n'))

        result.pop()  # Remove last newline.
        return result

    def __pt_container__(self):
        return self.window
        

class Animation:
    def __init__(self, frames: list[str]):
        self._last = 0
        self._frames = frames
        self._current = 0
        self.frameCount = len(frames)

    def next(self) -> str:
        frame = f'{self._frames[self._current].ljust(self._last)}\u001b[{max(len(self._frames[self._current]), self._last)}D'
        self._last = len(self._frames[self._current])
        self._current = (self._current + 1) % len(self._frames)

        return frame

class ProgressReporter(Thread):
    active_threads = {}
    cleanupLock: Lock = Lock()

    def __init__(self, freq: float = 1.0):
        super().__init__()
        self._lastUpdate = time.time()
        self._freq = freq
        self._animation = Animation(['――', '\\', '|', '/'])
        self._done = False
        print('\u001b[?25l', flush=True, end='')

    def update(self):
        delta = time.time() - self._lastUpdate

        if delta > (1 / self._freq / self._animation.frameCount):
            self._lastUpdate = time.time()
            print(self._animation.next(), flush=True, end='')

    def __terminate(self, message: str): 
        if self.native_id in self.active_threads:
            self.cleanupLock.acquire()

            self._done = True
            self.join()
            print(f'\u001b[?25h{message}', flush=True)
            del self.active_threads[self.native_id]
            set_echo(enabled=True)

            self.cleanupLock.release()

    def cancel(self):
        self.__terminate(dim_text('CANCELLED', 'yellow'))

    def complete(self, success: bool = True):
        message = dim_text('DONE', 'green') if success else dim_text('FAILED', 'red')
        self.__terminate(message)

    def run(self):
        ProgressReporter.active_threads[self.native_id] = self
        set_echo(enabled=False)

        while not self._done:
            self.update()
            time.sleep(0.025)

def set_echo(enabled: bool):
    if IS_WINDOWS or not sys.stdin.isatty():
        return
    
    if enabled:
        os.system('stty echo')
    else:
        os.system('stty -echo')


# Is target reachable from source (is target a parent of source?)
# Cache is constructed from parent chains
cache = {}
def is_reachable(target: Commit, source: Commit) -> bool:   
    if target.hexsha == source.hexsha:
        return True

    if not cache.get(source.hexsha, {}).get(target.hexsha, None) is None:
        return cache.get(source.hexsha, {}).get(target.hexsha)

    queue: list[list[Commit]] = list([[p] for p in source.parents])
    visited = set()

    while len(queue) > 0:
        current: list[Commit] = queue.pop(0)
        # small optimization: no need to store whole commit in the chain, only need hash
        if current[-1] == target or cache.get(current[-1].hexsha, {}).get(target.hexsha):
            for i in range(len(current) - 1):
                for j in range(i, len(current)):
                    c: dict = cache.get(current[i].hexsha, {})
                    cache[current[i].hexsha] = c
                    c[current[j].hexsha] = True

            return True

        for parent in current[-1].parents:
            if parent.hexsha not in visited:
                visited.add(parent.hexsha)
                queue.append(list(current))
                queue[-1].append(parent)

    for sha in visited:
        c: dict = cache.get(sha, {})
        cache[sha] = c
        c[target.hexsha] = False

    return False

def fetch(remote: git.Remote, src_branch: str) -> Commit:
    print(dim_text(f'Fetching {colored(src_branch, BLUE)} '), end='')

    reporter = ProgressReporter(freq=1.0)
    reporter.start()
    try:
        remote.fetch(f'refs/heads/{src_branch}')
        reporter.complete()

        return remote.refs[src_branch].commit
    except Exception as err:
        reporter.complete(success=False)
        exit(reason=err)

def find_remote_repo(repo: git.Repo, remote_name: str) -> str:
    """Attempts to locate a remote GitHub repository using a git remote URL"""
    remote = repo.remote(remote_name)
    url = list(remote.urls)[0]
    
    if url.find('github') == -1:
        exit(reason='Unable to find a remote GitHub repository')

    parts = url.split('/')

    return '/'.join(parts[-2:])

def run_hooks(hooks: list[str], config: ReleaseConfig):
    if len(hooks) == 0:
        return True

    env = os.environ
    env['RELEASE_VERSION'] = str(config.version)

    for cmd in hooks:
        print(dim_text(f'Running \'{cmd}\' '), end='', flush=True)
        reporter = ProgressReporter(freq=1.0)
        reporter.start()
        try:
            # Shell is intentionally used here for simplicity. 
            # Security is not a huge concern since this is just an automation script.
            command = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, shell=True, env=env, text=True)
            command.check_returncode()
            reporter.complete()
        except subprocess.CalledProcessError as err:
            reporter.complete(success=False)
            exit(2, tips=dim_text(err.stdout))

def login_github(remote: str) -> Repository:
    while True:
        username = input('Enter username [Skip if using token]: ')

        if username == '':
            token = prompt('Enter token: ', is_password=True)
            gh = Github(token)
        else:
            password = prompt('Enter password: ', is_password=True)
            gh = Github(username, password)
        
        token = None
        password = None

        try: # See if we have access
            return gh.get_repo(remote)
        except Exception as err:
            print(colored(f'Login failed: {err}', 'red'))

def make_pr(version: str, head: str, remote: str):
    target = login_github(remote)

    try:
        pr = target.create_pull(title=f'Merge release candidate for v{version}', body=PR_BODY, head=head, base='release/stable')
    except Exception as err:
        exit(reason='PR creation failed.', tips=err)

    print()
    print(bright_text('PR created successfully!', 'green'))
    print(bright_text(pr.html_url, BLUE))
    print('You can merge now or apply patches to the candidate branch as needed.')
    print(f'Reminder: {bright_text("Do not squash or rebase!", "yellow")}')

def prompt_release_notes() -> str:
    print(f'Type in release notes {colored(f"({EOF_COMMAND} on newline to stop)", BLUE)}')

    notes: list[str] = list()

    while True:
        try: 
            notes.append(input())
        except:
            break

    return '\n'.join(notes)

def yes_no_prompt(question: str, default: str = 'yes') -> bool:
    inputs = '(Y/n)' if default == 'yes' else '(y/N)'

    while True:
        ask = input(f'{question} {inputs}? ').lower()
        ask = default if ask == '' else ask

        if 'yes'.startswith(ask):
            return True
        elif 'no'.startswith(ask):
            return False
        else:
            continue

def select_version(previous_version: Version) -> Version:
    """Prompts for a version increment mode and increments the previous version.

    Modes are prompted as-needed depending on how many 'slots' the version has. If no mode is entered,
    then 'Patch' is selected (or the smallest mode if 'Patch' does not exist).
    """
    modes = ['Major', 'Minor', 'Patch', 'Build']

    while len(modes) > previous_version.slots:
        modes.pop()

    while len(modes) < previous_version.slots:
        modes.append(f'Digit{len(modes) + 1}')

    mode_output = '\n'.join([f' {[i + 1]} {dim_text(mode)} -> {previous_version.bump_highlight(i)}' for i, mode in enumerate(modes)])
    print(f'{bright_text("Current version:")} {colored(str(previous_version), BLUE)}\nVersion increment mode:')
    print(mode_output + '\n')

    while True:
        selection = input(f'Select a mode or enter a version [{dim_text("Patch")}]: ')

        option_position = min(2, len(modes)) if selection == '' else None

        if selection.isnumeric():
            selection = int(selection) - 1

            if selection >= 0 and selection < len(modes):
                option_position = selection
        elif selection.isalnum():
            for i, mode in enumerate(modes):
                if mode.lower().startswith(selection.lower()):
                    option_position = i
                    break

        if option_position is None:
            try:
                target_version = Version(selection)
            except:
                print(colored('Input is not parseable to a version.', 'red'))
                continue
        else:
            target_version = previous_version.bump(option_position)

        if target_version <= previous_version:
            print(colored('The next version must be greater than the previous version.', 'red'))
            continue

        if target_version.slots != previous_version.slots:
            print('The entered version scheme is different from the previous version. This may cause issues.')
            useVersion = yes_no_prompt('Wouled you like to continue', 'no')

            if not useVersion:
                continue

        break
    
    print(f'Next version -> {bright_text(str(target_version), "green")}\n')

    return target_version

def list_commits(target: Commit, source: Commit) -> 'list[Commit]':
    """Lists commits between source and target, excluding both"""
    target_parents = set(target.iter_parents())
    commits: list[Commit] = []

    for parent in source.iter_parents():
        if parent != target and parent not in target_parents and is_reachable(target, parent):
            commits.append(parent)

    return commits    

def select_commit(repo: git.Repo, config: ReleaseConfig) -> Commit:
    """Prompts for a commit that is between the head of the development branch and the last release commit"""
    commits = list_commits(config.commit, repo.head.commit)
    commits.append(repo.head.commit)
    commits.sort(key=lambda commit: commit.committed_datetime, reverse=True)

    display_text = []

    for commit in commits:
        display_text.append([('class:hash', commit.hexsha[:20]), ('', ' '), ('class:message', commit.summary)])

    style = Style.from_dict({
        'hash': 'italic',
        'message': f'fg:ansibright{BLUE}',
    })

    print('Select a target commit:\n')

    select_commit_app = Application(
        layout=Layout(SelectionDialog([(c, display_text[i]) for i, c in enumerate(commits)])), 
        full_screen=False, 
        style=style
    )

    return select_commit_app.run()

def select_notes() -> str:
    """Prompts for GitHub release notes, using either a file or manually entering notes. Markdown is expected."""
    use_notes = yes_no_prompt('Add additional release notes', 'no')

    if use_notes:
        use_file = yes_no_prompt('Use a file', 'no')

        if use_file:
            notes_file = input('Enter a file: ')

            with open(notes_file) as file:
                return file.read()
        else:
            return prompt_release_notes()

    return ''

def push_candidate(repo: git.Repo, hooks: list[str], config: ReleaseConfig, remote_name: str) -> str:
    """Runs the given hooks and commits all changes to the candidate branch specified by the release config."""
    version = str(config.version)
    candidate_branch = f'release/candidate/v{version}'

    repo.head.reference = config.commit
    index = repo.index
    index.reset(working_tree=True)

    for file in repo.untracked_files:
        os.remove(file)

    run_hooks(hooks, config)

    # index may be dirty after running hooks if the scripts use git at all
    print(dim_text('Rebuilding index '), end='')
    reporter = ProgressReporter(freq=1.0)
    reporter.start()
    index = repo.index
    
    index_diff: git.DiffIndex = index.diff(None)
    diff: Diff
    for diff in index_diff:
        if diff.deleted_file:
        	index.remove(diff.a_path)
        else:
            index.add(diff.b_path)

    notes = parse_changes(repo.working_tree_dir, version, config.notes)
    notesPath = os.path.join(os.path.dirname(config.path), 'notes.md')
    with open(notesPath, 'w+') as file:
        file.write(notes)

    index.add(config.write())
    index.add(notesPath)
    index.commit(f'Set release candidate for v{str(version)}')
    reporter.complete()
    
    # Detached head push TODO: specify to where it is being pushed
    print(dim_text(f'Pushing {colored(candidate_branch, BLUE)} '), end='')

    reporter = ProgressReporter(freq=1.0)
    reporter.start()

    try:
        repo.remote(remote_name).push(f'HEAD:refs/heads/{candidate_branch}')
        reporter.complete()
    except Exception as err:
        reporter.complete(success=False)
        exit(reason=f'Failed to push candidate: {err}')

    return candidate_branch  

def normalize_dictionary(dictionary: dict) -> dict:
    """Makes all keys in a dictionary lowercase, applied recursively."""
    new_dictionary = dict()

    if not isinstance(dictionary, dict):
        return dictionary

    for _, (key, value) in enumerate(dictionary.items()):
        if isinstance(value, dict):
            new_dictionary[key.lower()] = normalize_dictionary(value)
        elif isinstance(value, list):
            new_dictionary[key.lower()] = map(normalize_dictionary, value)
        else:
            new_dictionary[key.lower()] = value

    return new_dictionary

def parse_changes(root: str, version: str, body: str = '') -> str:
    """Looks for a generated changelog JSON file in the .changes directory, parsing it into a notes.md file."""
    changelog = os.path.join(root, '.changes', f'{version}.json')

    try:
        with open(changelog) as file:
            data = normalize_dictionary(json.load(file))

        title = f'## {version} ({data["date"]})'
        changelog = '\n'.join(map(lambda entry: f'- **{entry["type"]}** - {entry["description"]}', data['entries']))

        return f'{title}\n{body}\n### Changelog\n{changelog}'
    except Exception as err:
        exit(reason='Failed to read changelog from ".changes" directory', tips=err)

def cleanup(original_branch: str = None):
    """Stops all running threads and checksout the original git branch if applicable."""
    thread: ProgressReporter
    for thread in ProgressReporter.active_threads.copy().values():
        thread.cancel()

    print(colorama.Style.RESET_ALL, end='')
    print(flush=True)

    if not original_branch is None:
        back: git.Head = repo.branches[original_branch]
        back.checkout(force=True)

def exit(code: int = None, reason: str = None, tips: str = None):
    """General clean-up code for when the script terminates."""
    cleanup(original_branch)

    if not reason is None:
        code = 1 if code is None else code
        print(colored(reason, 'red' if code != 0 else 'white'))
    else:
        code = 0

    if not tips is None:
        print(tips)

    sys.exit(code)

def exception_hook(error, original_branch):
    print(error)
    cleanup(original_branch)

def load_config(repo: git.Repo, config_path: str) -> ReleaseConfig:
    """Loads and validate a release config from a past release. 
    
    The config can be located in two places: the path specified by 'config_path' or in the same directory
    as the release script. Changes made to the config will always be written back to where it was found.
    """
    abs_path = os.path.join(repo.working_tree_dir, config_path)

    if not os.path.isfile(abs_path):
        # last ditch effort, look for it in the same directory as the script
        abs_path = os.path.join(repo.working_tree_dir, os.path.dirname(os.path.relpath(__file__, repo.working_tree_dir)), os.path.basename(config_path))

        if not os.path.isfile(abs_path):
            exit(reason=f'Unable to locate "{config_path}"', tips=dim_text('Make sure the path is relative to the repository\'s root.'))

    try:
        config = ReleaseConfig(abs_path, repo)
    except:
        exit(reason='Failed to read release configuration.')

    validation = config.pre_validate(release_commit)

    if not validation is None:
        exit(reason=validation)

    return config

if __name__ == '__main__':
    WORKING_DIR = os.path.dirname(os.path.relpath(__file__))
    DEFAULT_DEVELOPMENT_BRANCH = 'master'
    DEFAULT_RELEASE_BRANCH = 'release/stable'
    DEFAULT_REMOTE = 'origin'

    parser = argparse.ArgumentParser(description='Creates release candidate branches from a target commit.')
    parser.add_argument('--hooks', metavar='cmd', type=str, required=False, nargs='+', help='Shell commands to be executed prior to pushing the release candidate', default=[])
    parser.add_argument('--config', metavar='path', type=str, required=False, help='Relative path to config file from the root of the repository', default=os.path.join(WORKING_DIR, CONFIG_NAME))
    parser.add_argument('--source', metavar='branch', type=str, required=False, help=f'Development branch to source commits from (default: {DEFAULT_DEVELOPMENT_BRANCH})', default=DEFAULT_DEVELOPMENT_BRANCH)
    parser.add_argument('--target', metavar='branch', type=str, required=False, help=f'Release branch to make a PR against (default: {DEFAULT_RELEASE_BRANCH})', default=DEFAULT_RELEASE_BRANCH)
    parser.add_argument('--remote', metavar='ref', type=str, required=False, help=f'Remote reference to fetch from and push to (default: {DEFAULT_REMOTE})', default=DEFAULT_REMOTE)

    args = parser.parse_args()
    pre_commit_hooks = args.hooks
    config_path: str = args.config
    development_branch: str = args.source
    release_branch: str = args.target
    remote_name: str = args.remote

    try:
        repo = git.Repo(search_parent_directories=True)
    except:
        exit(reason='Failed to find a git repository.')

    if repo.is_dirty(untracked_files=True):
        exit(reason='Working branch has unsaved changes.', tips=colored('Commit or stash any changes before running this script.', 'yellow'))

    if repo.head.is_detached:
        # Ideally we would check for 'new' commits in this state, but if someone is starting this script
        # in a detached state they probably know what they're doing
        print(dim_text('Warning: starting in detached HEAD', 'yellow'))
    else:
        original_branch = str(repo.head.ref)
        repo.head.reference = repo.head.commit

    signal.signal(signal.SIGINT, lambda sig, frame: exit())
    sys.excepthook = lambda exctype, value, traceback: exception_hook(value, original_branch)
    
    remote = repo.remote(remote_name)
    development_commit = fetch(remote, development_branch)
    release_commit = fetch(remote, release_branch)

    print()

    repo.head.reference = development_commit
    repo.index.reset(working_tree=True)

    # clean (potentially add warning, though we're already in a detached state so all changes would be committed)
    for file in repo.untracked_files:
        os.remove(file)

    config = load_config(repo, config_path)

    config.version = select_version(config.version)
    config.commit = select_commit(repo, config)
    config.notes = select_notes()

    print()
    print(f'{bright_text("----- Release parameters -----")}')
    print(str(config))
    verify = yes_no_prompt('Does this look accurate')

    if not verify:
        exit(reason='Aborting release.')
    
    candidate = push_candidate(repo, pre_commit_hooks, config, remote_name)

    print('Creating PR...')
    make_pr(str(config.version), candidate, find_remote_repo(repo, remote_name))

    exit()