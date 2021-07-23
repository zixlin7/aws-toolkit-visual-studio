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


# TODO: figure out better way to handle global state
original = None

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

PR_BODY = '### This PR must be merged by merge commit'
CONFIG_NAME = 'config.json'

# Is target reachable from source (is target a parent of source?)
# Cache is constructed from parent chains
cache = {}
def is_reachable(target: Commit, source: Commit) -> bool:   
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
    
class Version:
    slots: int

    def __init__(self, version: str, parent: 'Version' = None):
        is_number: Callable[[str], bool] = lambda part: part.isnumeric()
        conv_number: Callable[[str], int] = lambda s: int(s)

        self._version = list(map(conv_number, filter(is_number, version.split('.'))))

        if (len(self._version) != len(version.split('.'))):
            raise Exception('Version can only contain numbers')

        if (len(self._version) == 0):
            raise Exception('Empty version')

        self.slots = len(self._version)

    def __eq__ (self, other: 'Version') -> bool:
        return not other is None and self._version == other._version

    def __gt__(self, other: 'Version') -> bool:
        if other is None:
            return False

        return other._version < self._version and not other._version == self._version

    def __lt__(self, other: 'Version') -> bool:
        if other is None:
            return True

        for i in range(max(len(self._version), len(other._version))):
            v1 = self._version[i] if i < len(self._version) else 0
            v2 = other._version[i] if i < len(other._version) else 0

            if (v1 > v2):
                return False

        return True

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

    def __init__(self, path: str, repo: git.Repo):
        with open(path) as file:
            config = json.load(file)

        self._repo = repo
        self._path = path
        self.version = Version(config['version'])
        self.notes = config['notes']

        try:
            self.commit = repo.rev_parse(config['commit'])

            if self.commit.type != 'commit':
                raise Exception('not a commit')
        except:
            # make this better
            exit(reason='Bad commit in config.')

    def __str__(self) -> str:
        out = ''
        out += f'{dim_text("Version:")} {bright_text(str(self.version), "blue")}\n'
        out += f'{dim_text("Commit:")} {bright_text(self.commit.hexsha, "blue")}\n'
        out += f'{dim_text("Notes:")}\n{bright_text(self.notes, "blue")}\n' if self.notes != '' else ''

        return out

    def _jsonify(self) -> dict:
        return {
            'version': str(self.version),
            'commit': str(self.commit),
            'notes': self.notes,
        }

    def write(self, path: Optional[str] = None) -> str:
        path = self._path if path is None else path

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
    selected_style: str = "fg:ansired"
    checked_style: str = ""
    multiple_selection: bool = False
    show_scrollbar: bool = True
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

        # keep building up keypresses -> if no match then dump buffer TODO
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

            if not IS_WINDOWS:
                os.system('stty echo')

            self.cleanupLock.release()

    def cancel(self):
        self.__terminate(dim_text('CANCELLED', 'yellow'))

    def complete(self, success: bool = True):
        message = dim_text('DONE', 'green') if success else dim_text('FAILED', 'red')
        self.__terminate(message)

    def run(self):
        ProgressReporter.active_threads[self.native_id] = self
        if not IS_WINDOWS:
            os.system('stty -echo')

        while not self._done:
            self.update()
            time.sleep(0.025)


def fetch(remote: git.Remote, src: str) -> Commit:
    print(dim_text(f'Fetching {colored(src, "blue")} '), end='')

    reporter = ProgressReporter(freq=1.0)
    reporter.start()
    try:
        remote.fetch(f'refs/heads/{src}')
        reporter.complete()

        return remote.refs[src].commit
    except Exception as err:
        reporter.complete(success=False)
        exit(reason=err)

def find_remote_repo(repo: git.Repo) -> str:
    remote = get_remote(repo)
    url = list(remote.urls)[0]
    
    # this code sucks
    if url.find('github') == -1:
        print('not github')
        sys.exit(1)

    parts = url.split('/')

    return parts[-1]

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

def make_pr(version: str, head: str, remote: str):
    username = input('Enter username [Skip if using token]: ')

    if username == '':
        token = prompt('Enter token: ', is_password=True)
        gh = Github(token)
    else:
        password = prompt('Enter password: ', is_password=True)
        gh = Github(username, password)
    
    token = None
    password = None

    try: # We just use the first user we find for now
        target = gh.get_user().get_repo(remote)
        pr = target.create_pull(title=f'Merge release candidate for v{version}', body=PR_BODY, head=head, base='release/stable')
    except Exception as err:
        # Ask for retry instead of failing? Does not matter too much
        # TODO: may be possible that a duplicate candidate exists (so this failure is not always accurate)
        exit(reason='Login failed.', tips=err)

    print(colored('PR created successfully!', 'green'))
    print(f'Link: {pr.html_url}')
    print('You can merge now or apply patches to the candidate branch as needed.')
    print(f'Reminder: {bright_text("Do not squash or rebase!", "yellow")}')

def prompt_release_notes() -> str:
    print(f'Type in release notes {colored("(Ctrl-D on newline to stop)", "blue")}')

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

def select_version(config: ReleaseConfig) -> Version:
    modes = ['Major', 'Minor', 'Patch', 'Build']

    while len(modes) > config.version.slots:
        modes.pop()

    while len(modes) < config.version.slots:
        modes.append(f'Digit{len(modes) + 1}')

    mode_output = '\n'.join([f' {[i + 1]} {dim_text(mode)} -> {config.version.bump_highlight(i)}' for i, mode in enumerate(modes)])
    print(f'{bright_text("Current version:")} {colored(str(config.version), "blue")}\nVersion increment mode:')
    print(mode_output + '\n')

    while True:
        selection = input(f'Select a mode or enter a version [{dim_text("Patch")}]: ')

        option_position = 2 if selection == '' else None

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
            target_version = config.version.bump(option_position)

        if target_version < config.version:
            print(colored('The next version must be greater than the previous version.', 'red'))
            continue

        if target_version.slots != config.version.slots:
            print('The entered version scheme is different from the previous version. This may cause issues.')
            useVersion = yes_no_prompt('Wouled you like to continue', 'no')

            if not useVersion:
                continue

        break
    
    print(f'Next version -> {bright_text(str(target_version), "green")}\n')

    return target_version

def list_commits(target: Commit, source: Commit) -> 'list[Commit]':
    target_parents = set(target.iter_parents())
    commits: list[Commit] = []

    for parent in source.iter_parents():
        if parent != target and parent not in target_parents and is_reachable(target, parent):
            commits.append(parent)

    return commits    

def select_commit(repo: git.Repo, config: ReleaseConfig) -> Commit:
    commits = list_commits(config.commit, repo.head.commit)
    commits.append(repo.head.commit)
    commits.sort(key=lambda commit: commit.committed_datetime, reverse=True)

    display_text = []

    for commit in commits:
        display_text.append([('class:hash', commit.hexsha[:20]), ('', ' '), ('class:message', commit.summary)])

    style = Style.from_dict({
        'hash': 'italic',
        'message': 'fg:ansiblue',
    })

    print('Select a target commit:\n')

    select_commit_app = Application(
        layout=Layout(SelectionDialog([(c, display_text[i]) for i, c in enumerate(commits)])), 
        full_screen=False, 
        style=style
    )

    return select_commit_app.run()

def select_notes() -> str:
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

def push_candidate(repo: git.Repo, hooks: list[str], config: ReleaseConfig) -> str:
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
    with open(os.path.join(repo.working_tree_dir, 'buildtools', 'release', 'notes.md'), 'w+') as file:
        file.write(notes)

    index.add(config.write())
    index.add(os.path.join(repo.working_tree_dir, 'buildtools', 'release', 'notes.md'))
    index.commit(f'Set release candidate for v{str(version)}')
    reporter.complete()
    
    # Detached head push TODO: specify to where it is being pushed
    print(dim_text(f'Pushing {colored(candidate_branch, "blue")} '), end='')

    reporter = ProgressReporter(freq=1.0)
    reporter.start()

    try:
        get_remote(repo).push(f'HEAD:refs/heads/{candidate_branch}')
        reporter.complete()
    except Exception as err:
        reporter.complete(success=False)
        exit(reason=err) # TODO: log stacktrace?

    return candidate_branch  

# Parses changelog to add to the notes
def parse_changes(root: str, version: str, body: str = '') -> str:
    changelog = os.path.join(root, '.changes', f'{version}.json')

    try:
        with open(changelog) as file:
            data = json.load(file)

        # TODO: make this not uppercase...
        title = f'## {version} ({data["Date"]})'
        changelog = '\n'.join(map(lambda entry: f'- **{entry["Type"]}** \u2013 {entry["Description"]}', data['Entries']))

        return f'{title}\n{body}\n### Changelog\n{changelog}'
    except Exception as err:
        exit(reason='Failed to read changelog', tips=err)

# TODO: this is currently hard-coded to 'origin'
def get_remote(repo: git.Repo) -> git.Remote:
    return repo.remote()

def cleanup(original: str = None):
    thread: ProgressReporter
    for thread in ProgressReporter.active_threads.copy().values():
        thread.cancel()

    print(colorama.Style.RESET_ALL, end='')
    print(flush=True)

    if not original is None:
        back: git.Head = repo.branches[original]
        back.checkout(force=True)

def exit(code: int = None, reason: str = None, tips: str = None):
    cleanup(original)

    if not reason is None:
        code = 1 if code is None else code
        print(colored(reason, 'red' if code != 0 else 'white'))
    else:
        code = 0

    if not tips is None:
        print(tips)

    sys.exit(code)

def exception_hook(error, original):
    print(error)
    cleanup(original)

if __name__ == '__main__':
    WORKING_DIR = os.path.dirname(os.path.relpath(__file__))
    DEFAULT_DEVELOPMENT_BRANCH = 'master'
    DEFAULT_RELEASE_BRANCH = 'release/stable'
    DEFAULT_REMOTE = 'origin'

    parser = argparse.ArgumentParser(description='Creates release candidate branches from a target commit.')
    parser.add_argument('--hooks', metavar='cmd', type=str, required=False, nargs='+', help='Shell commands to be executed prior to pushing the release candidate', default=[])
    parser.add_argument('--config', metavar='path', type=str, required=False, help='Relative path to config file from the root of the repository', default=os.path.join(WORKING_DIR, CONFIG_NAME))
    parser.add_argument('--source', metavar='branch', type=str, required=False, help='Development branch to source commits from (default: master)', default=DEFAULT_DEVELOPMENT_BRANCH)
    parser.add_argument('--target', metavar='branch', type=str, required=False, help='Release branch to make a PR against (default: release/stable)', default=DEFAULT_RELEASE_BRANCH)
    parser.add_argument('--remote', metavar='ref', type=str, required=False, help='Remote reference to fetch from and push to (default: origin)', default=DEFAULT_REMOTE)
    parser.add_argument('--debug', metavar='', type=bool, required=False, help='Outputs debugging information', default=False)

    args = parser.parse_args()
    pre_commit_hooks = args.hooks
    config_path: str = args.config
    development_branch: str = args.source
    release_branch: str = args.target

    try:
        repo = git.Repo(search_parent_directories=True)
    except:
        exit(reason='Failed to find a git repository.')

    if repo.is_dirty(untracked_files=True):
        exit(reason='Working branch has unsaved changes.', tips=colored('Commit or stash any changes before running this script.', 'yellow'))

    if repo.head.is_detached:
        print(dim_text('Warning: starting in detached HEAD', 'yellow'))
        # TODO: check for commits on top of detached HEAD then abort
    else:
        original = str(repo.head.ref)
        repo.head.reference = repo.head.commit

    signal.signal(signal.SIGINT, lambda sig, frame: exit())
    sys.excepthook = lambda exctype, value, traceback: exception_hook(value, original)

    remote = get_remote(repo)
    development_commit = fetch(remote, development_branch)
    release_commit = fetch(remote, release_branch)

    print()

    repo.head.reference = development_commit
    repo.index.reset(working_tree=True)

    # clean (potentially add warning, though we're already in a detached state so all changes would be committed)
    for file in repo.untracked_files:
        os.remove(file)

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

    config.version = select_version(config)
    config.commit = select_commit(repo, config)
    config.notes = select_notes()

    print()
    print(f'{bright_text("----- Release parameters -----")}')
    print(str(config))
    verify = yes_no_prompt('Does this look accurate')

    if not verify:
        exit(reason='Aborting release.')
    
    candidate = push_candidate(repo, pre_commit_hooks, config)

    print('Creating PR...')
    make_pr(str(config.version), candidate, find_remote_repo(repo))

    exit()


