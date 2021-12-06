#!/bin/bash

if ! "$IS_PROD";
then
    exit 0;
fi

git clone $REPO_URL $REPO_DIR; cd $REPO_DIR
git merge $COMMIT_HASH --no-ff -m "Merge v${NEXT_VERSION} from $RELEASE_BRANCH"

if [ $? -ne 0 ]; then
    CONFLICT=true; git merge --abort; git checkout $COMMIT_HASH
fi

echo "[Release] Pipeline is in production. Pushing commit to GitHub."

create_merge_pr() {
    git checkout -b $1
    git push --follow-tags -u origin $1
    gh pr create --base $DEVELOPMENT_BRANCH --title "Merge $RELEASE_BRANCH changes into $DEVELOPMENT_BRANCH (${NEXT_VERSION})" --body "$2"
}

if ! [ -z $CONFLICT ]; then
    echo "[Release] Merge conlict detected. Opening a PR."

    BRANCH_NAME="release/merge-conflict/${NEXT_VERSION}"

    PR_BODY="Automatic merge failed due to conflicts.
Manual resolution is required.
# Merges _must_ be done by merge commit."

    create_merge_pr $BRANCH_NAME "$PR_BODY"

    exit 0;
fi

git push origin

if [ $? -ne 0 ]; then
    echo "[Release] Failed to push to branch. Opening a PR."

    BRANCH_NAME="release/manual-merge/${NEXT_VERSION}"

    PR_BODY="Automatic merge failed to push directly.
Manual resolution is required.
# Merge _must_ be done by merge commit."

    create_merge_pr $BRANCH_NAME "$PR_BODY"
fi
