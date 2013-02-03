#!/bin/bash
find Assets/Code/ \( -name '*.cs' ! -path '*Libraries*' \)| tee >(xargs cat| wc -l)
