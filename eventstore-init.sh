#!/bin/sh

docker run --name kurrentdb-node -it -p 2113:2113 \
    docker.kurrent.io/kurrent-latest/kurrentdb:25.0.1 --insecure --run-projections=All\
    --start-standard-projections=true \
    --enable-atom-pub-over-http
