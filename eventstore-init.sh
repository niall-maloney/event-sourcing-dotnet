#!/bin/sh

docker run --name kurrentdb-node -it -p 2113:2113 \
    docker.kurrent.io/kurrent-preview/kurrentdb:25.0.1-experimental-arm64-8.0-jammy \
    --insecure --run-projections=All \
    --start-standard-projections=true \
    --enable-atom-pub-over-http
