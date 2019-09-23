#!/bin/bash
cleanup ()
{
kill -s SIGTERM $!
exit 0
}

trap cleanup SIGINT
trap cleanup SIGQUIT
trap cleanup SIGTSTP

echo "Starting IBM DB2..."
/var/db2_setup/lib/setup_db2_instance.sh &

sleep 300

echo "Executing init scripts"
ls -al /docker-entrypoint-initdb.d
su - db2inst1 -c 'for f in /docker-entrypoint-initdb.d/*; do case "$f" in *.sh) echo "$0: running $f"; . "$f" ;; *.sql) echo "$0: running $f" && until db2 -vtf "$f"; do >&2 echo "DB2 is unavailable - sleeping"; sleep 2; done & ;; *) echo "$0: ignoring $f" ;; esac done'
echo "Startup finished"

fg

for (( ; ; ))
do
   sleep 1
done
