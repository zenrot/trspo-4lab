FROM ollama/ollama:latest

ARG OLLAMA_MODEL=gemma2:2b
ENV OLLAMA_MODELS=/usr/share/ollama/.ollama/models

RUN ollama serve >/tmp/ollama-build.log 2>&1 & \
    pid="$!"; \
    for i in $(seq 1 60); do \
      if ollama list >/dev/null 2>&1; then \
        break; \
      fi; \
      sleep 1; \
    done; \
    ollama pull "$OLLAMA_MODEL"; \
    kill "$pid"; \
    wait "$pid" || true
