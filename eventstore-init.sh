#!/bin/sh

docker run --name esdb-node -it -p 2113:2113 -p 1113:1113 \
    eventstore/eventstore:22.10.2-alpha-arm64v8 --insecure --run-projections=All\
    --start-standard-projections=true \
    --enable-external-tcp --enable-atom-pub-over-http
