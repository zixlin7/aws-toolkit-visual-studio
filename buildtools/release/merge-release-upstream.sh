#!/bin/bash

git clone $REPO_URL $REPO_DIR; cd $REPO_DIR
git merge $COMMIT_HASH --no-ff -m "Merge v${NEXT_VERSION} from $RELEASE_BRANCH"

if [ $? -ne 0 ]; then
    CONFLICT=true; git merge --abort; git checkout $COMMIT_HASH
fi

if "$IS_PROD"; then
    echo "[Release] Pipeline is in production. Pushing commit to GitHub."

    if ! [ -z $CONFLICT ]; then
        echo "[Release] Merge conlict detected. Opening a PR."
        BRANCH_NAME="release/merge-conflict/${NEXT_VERSION}"
        git checkout -b $BRANCH_NAME
        git push --follow-tags -u origin $BRANCH_NAME
        gh pr create --base $DEVELOPMENT_BRANCH --title "Merge $RELEASE_BRANCH changes into $DEVELOPMENT_BRANCH (${NEXT_VERSION})" --body "$MERGE_PR_BODY"
    else
        git push origin
    fi
fi
