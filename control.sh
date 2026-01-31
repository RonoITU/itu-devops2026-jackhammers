#!/bin/bash

# Ensure we have a command argument
if [ $# -eq 0 ]; then
    echo "Usage: ./control.sh <command>"
    exit 1
fi

if [ "$1" = "init" ]; then
    if [ -f "/tmp/minitwit.db" ]; then
        echo "Database already exists."
        exit 1
    fi
    echo "Putting a database to /tmp/minitwit.db..."
    python3 -c "from minitwit import init_db;init_db()"

elif [ "$1" = "startprod" ]; then
    echo "Starting minitwit with production webserver..."
    # Using python3 -m gunicorn is often safer to ensure it runs on the correct python env
    nohup python3 -m gunicorn --workers 4 --timeout 120 --bind 0.0.0.0:5000 minitwit:app > /tmp/out.log 2>&1 &

elif [ "$1" = "start" ]; then
    echo "Starting minitwit..."
    # Changed `which python` to explicit python3 and used $()
    nohup "$(which python3)" minitwit.py > /tmp/out.log 2>&1 &

elif [ "$1" = "stop" ]; then
    echo "Stopping minitwit..."
    pkill -f minitwit

elif [ "$1" = "inspectdb" ]; then
    ./flag_tool -i | less

elif [ "$1" = "flag" ]; then
    # Shift to remove the first argument ("flag") so $@ contains only the rest
    shift
    ./flag_tool "$@"

else
    echo "I do not know this command..."
fi
