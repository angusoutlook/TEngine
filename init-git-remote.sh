#!/usr/bin/env bash

set -e

DEFAULT_FETCH_URL="https://github.com/Alex-Rachel/TEngine.git"
DEFAULT_PUSH_URL="https://github.com/angusoutlook/TEngine.git"

ERROR=0

if [ "$#" -eq 0 ]; then
  FETCH_URL="$DEFAULT_FETCH_URL"
  PUSH_URL="$DEFAULT_PUSH_URL"
elif [ "$#" -eq 2 ]; then
  FETCH_URL="$1"
  PUSH_URL="$2"
else
  echo "Error: you must provide both fetch-urlA and push-urlB, or no arguments to use defaults."
  ERROR=1
fi

if [ "$ERROR" -eq 0 ]; then
  echo "Setting origin fetch URL to: $FETCH_URL"
  if ! git remote set-url origin "$FETCH_URL"; then
    echo "Failed to set fetch URL"
    ERROR=1
  fi

  echo "Setting origin push URL to: $PUSH_URL"
  if ! git remote set-url --push origin "$PUSH_URL"; then
    echo "Failed to set push URL"
    ERROR=1
  fi

  if [ "$ERROR" -eq 0 ]; then
    echo "Current remote configuration:"
    git remote -v
  fi
fi

echo
echo "Usage: $0 <fetch-urlA> <push-urlB>"
echo "Sample: $0 https://github.com/user/repo-a.git https://github.com/user/repo-b.git"

if [ "$#" -eq 0 ]; then
  echo "Note: no arguments provided, using defaults:"
  echo "  fetch-urlA = $DEFAULT_FETCH_URL"
  echo "  push-urlB  = $DEFAULT_PUSH_URL"
fi

exit "$ERROR"


