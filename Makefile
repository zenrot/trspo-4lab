SHELL := /bin/sh

ROOT_DIR := $(CURDIR)
APP_DIR := $(ROOT_DIR)/labserveroriginal
ADMIN_DIR := $(APP_DIR)/web/admin
DASHBOARD_DIR := $(APP_DIR)/web/dashboard
COMPOSE := docker compose -f $(APP_DIR)/compose.yml
DOTNET ?= $(shell if [ -x "$$HOME/.dotnet/dotnet" ]; then echo "$$HOME/.dotnet/dotnet"; else echo dotnet; fi)
NPM ?= npm
OLLAMA_LOG ?= /tmp/trspo-ollama.log

.PHONY: help deps build-front build-admin build-dashboard test verify docker-check ollama-start llm-local compose-config up up-full up-container-ollama up-all start smoke ps urls logs logs-server logs-plagiarism logs-web down stop restart pull-llm pull-llm-container clean-dist clean-node-modules clean-learning

help:
	@printf '%s\n' 'LabServer local automation'
	@printf '%s\n' ''
	@printf '%s\n' 'Main targets:'
	@printf '%s\n' '  make up              Build frontends and start local stack with host Ollama'
	@printf '%s\n' '  make up-full         Start local stack plus GitLab container'
	@printf '%s\n' '  make up-container-ollama'
	@printf '%s\n' '                       Start local stack with Ollama in Docker'
	@printf '%s\n' '  make up-all          Start GitLab and Ollama containers too'
	@printf '%s\n' '  make down            Stop and remove containers'
	@printf '%s\n' '  make restart         Restart local stack'
	@printf '%s\n' '  make verify          Run backend tests, frontend builds, compose validation'
	@printf '%s\n' '  make smoke           Check UIs and run one plagiarism request on prog1.c'
	@printf '%s\n' '  make urls            Print local URLs'
	@printf '%s\n' ''
	@printf '%s\n' 'Useful targets:'
	@printf '%s\n' '  make deps            Install frontend dependencies with npm ci'
	@printf '%s\n' '  make build-front     Build admin and dashboard frontends'
	@printf '%s\n' '  make test            Run .NET tests'
	@printf '%s\n' '  make ps              Show compose services'
	@printf '%s\n' '  make logs            Follow all compose logs'
	@printf '%s\n' '  make logs-server     Follow LabServer logs'
	@printf '%s\n' '  make logs-plagiarism Follow plagiarism-server logs'
	@printf '%s\n' '  make pull-llm        Pull gemma2:2b into local host Ollama'
	@printf '%s\n' '  make pull-llm-container'
	@printf '%s\n' '                       Pull gemma2:2b into the Ollama compose volume'

deps:
	cd $(ADMIN_DIR) && { [ -x node_modules/.bin/ng ] || $(NPM) ci; }
	cd $(DASHBOARD_DIR) && { [ -x node_modules/.bin/ng ] || $(NPM) ci; }

build-admin:
	cd $(ADMIN_DIR) && $(NPM) run build

build-dashboard:
	cd $(DASHBOARD_DIR) && $(NPM) run build

build-front: deps build-admin build-dashboard

test:
	$(DOTNET) test $(APP_DIR)/LabServer.sln

verify: test build-front compose-config

docker-check:
	@docker info >/dev/null

ollama-start:
	@curl -fsS http://localhost:11434/api/tags >/dev/null 2>&1 || { \
		command -v ollama >/dev/null || { \
			printf '%s\n' 'Ollama CLI was not found. Install Ollama or use: make up-container-ollama'; \
			exit 1; \
		}; \
		printf '%s\n' 'Starting local Ollama on http://localhost:11434 ...'; \
		nohup ollama serve > $(OLLAMA_LOG) 2>&1 & \
		sleep 5; \
	}

llm-local: ollama-start
	@curl -fsS http://localhost:11434/api/tags >/dev/null || { \
		printf '%s\n' 'Local Ollama is not running on http://localhost:11434.'; \
		printf '%s\n' 'Start Ollama first, or use: make up-container-ollama'; \
		printf '%s%s\n' 'Ollama log: ' '$(OLLAMA_LOG)'; \
		exit 1; \
	}
	@curl -fsS http://localhost:11434/api/tags | grep -q '"name":"gemma2:2b' || $(MAKE) pull-llm

compose-config:
	$(COMPOSE) config --quiet

up: docker-check llm-local build-front compose-config
	$(COMPOSE) up -d --build db plagiarism-server labserver webserver
	@$(MAKE) urls

up-full: docker-check llm-local build-front compose-config
	COMPOSE_PROFILES=gitlab DISABLE_BACKGROUND_SERVICES=false $(COMPOSE) up -d --build db gitlab plagiarism-server labserver webserver
	@$(MAKE) urls

up-container-ollama: docker-check build-front compose-config
	COMPOSE_PROFILES=ollama-container OLLAMA_BASE_URL=http://ollama:11434 $(COMPOSE) up -d ollama
	COMPOSE_PROFILES=ollama-container OLLAMA_BASE_URL=http://ollama:11434 $(COMPOSE) run --rm ollama-init
	COMPOSE_PROFILES=ollama-container OLLAMA_BASE_URL=http://ollama:11434 $(COMPOSE) up -d --build db plagiarism-server labserver webserver
	@$(MAKE) urls

up-all: docker-check build-front compose-config
	COMPOSE_PROFILES=gitlab,ollama-container OLLAMA_BASE_URL=http://ollama:11434 DISABLE_BACKGROUND_SERVICES=false $(COMPOSE) up -d ollama gitlab
	COMPOSE_PROFILES=gitlab,ollama-container OLLAMA_BASE_URL=http://ollama:11434 DISABLE_BACKGROUND_SERVICES=false $(COMPOSE) run --rm ollama-init
	COMPOSE_PROFILES=gitlab,ollama-container OLLAMA_BASE_URL=http://ollama:11434 DISABLE_BACKGROUND_SERVICES=false $(COMPOSE) up -d --build db gitlab plagiarism-server labserver webserver
	@$(MAKE) urls

start: up

smoke:
	@curl -fsS --max-time 10 http://localhost:8080 >/dev/null
	@curl -fsS --max-time 10 http://localhost:8081 >/dev/null
	@curl -fsS --max-time 10 http://localhost:11434/api/tags | grep -q '"name":"gemma2:2b'
	@tmp_zip=/tmp/trspo-make-smoke.zip; \
	commit_hash=make-smoke-$$(date +%s); \
	rm -f "$$tmp_zip"; \
	zip -jq "$$tmp_zip" $(ROOT_DIR)/prog1.c; \
	archive=$$(base64 < "$$tmp_zip" | tr -d '\n'); \
	curl -fsS --max-time 180 http://localhost:5289/test/plagiarism/schedule \
		-H 'Content-Type: application/json' \
		-d "{\"mergeRequest\":{\"title\":\"make smoke\",\"source_project_id\":999002,\"sha\":\"$$commit_hash\"},\"repoArchiveBase64\":\"$$archive\",\"changedFilePaths\":[\"prog1.c\"]}" >/dev/null; \
	result=$$(curl -fsS --max-time 20 http://localhost:5289/test/plagiarism/getresult \
		-H 'Content-Type: application/json' \
		-d "{\"sourceProjectId\":999002,\"commitHash\":\"$$commit_hash\"}"); \
	printf '%s\n' "$$result"; \
	printf '%s\n' "$$result" | grep -q '"testCompleted":true'; \
	printf '%s\n' "$$result" | grep -q 'Similarity='

ps:
	$(COMPOSE) ps

urls:
	@printf '%s\n' ''
	@printf '%s\n' 'Local URLs:'
	@printf '%s\n' '  Admin UI:             http://localhost:8080'
	@printf '%s\n' '  Student dashboard:    http://localhost:8081'
	@printf '%s\n' '  GitLab web:           http://localhost:8082  (only with make up-full/up-all)'
	@printf '%s\n' '  Plagiarism API:       http://localhost:5289/test/plagiarism'
	@printf '%s\n' '  Host Ollama API:      http://localhost:11434'
	@printf '%s\n' '  Docker Ollama API:    http://localhost:11435 (only with make up-container-ollama/up-all)'
	@printf '%s\n' ''

logs:
	$(COMPOSE) logs -f

logs-server:
	$(COMPOSE) logs -f labserver

logs-plagiarism:
	$(COMPOSE) logs -f plagiarism-server

logs-web:
	$(COMPOSE) logs -f webserver

down:
	$(COMPOSE) down

stop: down

restart: down up

pull-llm:
	ollama pull gemma2:2b

pull-llm-container: docker-check
	COMPOSE_PROFILES=ollama-container OLLAMA_BASE_URL=http://ollama:11434 $(COMPOSE) up -d ollama
	COMPOSE_PROFILES=ollama-container OLLAMA_BASE_URL=http://ollama:11434 $(COMPOSE) run --rm ollama-init

clean-dist:
	rm -rf $(ADMIN_DIR)/dist $(DASHBOARD_DIR)/dist

clean-node-modules:
	rm -rf $(ADMIN_DIR)/node_modules $(DASHBOARD_DIR)/node_modules

clean-learning:
	$(COMPOSE) down
	docker volume rm labserveroriginal_plagiarism-learning || true
