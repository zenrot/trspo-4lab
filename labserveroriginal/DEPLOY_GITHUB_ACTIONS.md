# Развертывание через GitHub Actions

Каждый push в ветку `main` запускает GitHub Actions и разворачивает полный стек LabServer на сервере `alexey@188.243.90.29`.

Подключение к серверу выполняется по SSH с паролем. SSH-ключ для deploy не нужен.

## Что поднимается на сервере

После развертывания будут запущены отдельные Docker-контейнеры:

- `labserver` - серверная часть LabServer
- `plagiarism-server` - отдельный экземпляр серверной части для проверки плагиата
- `webserver` - nginx для админки, кабинета студента и GitLab HTTP
- `db` - PostgreSQL
- `gitlab` - локальный GitLab
- `mailpit` - локальный SMTP-сервер и веб-интерфейс для просмотра отправленных писем

Публичные адреса:

- админ-панель: `http://zenrot.root.sx:18080`
- кабинет студента: `http://zenrot.root.sx:18081`
- GitLab через HTTP: `http://zenrot.root.sx:18082`
- GitLab через SSH: `ssh://git@zenrot.root.sx:18022`
- просмотр отправленных писем: `http://zenrot.root.sx:18083`
- вспомогательный API проверки плагиата: `http://zenrot.root.sx:15289`

PostgreSQL доступен только с самого сервера на `127.0.0.1:15432`.

Эти порты выбраны потому, что они были свободны на сервере `alexey@188.243.90.29` и не конфликтовали с уже работающим Matrix-сервером.

## Секрет GitHub Actions

Нужно добавить только один обязательный secret:

- `DEPLOY_PASSWORD`: пароль пользователя `alexey` на сервере `188.243.90.29`

Добавить его можно здесь:

`Settings` -> `Secrets and variables` -> `Actions` -> `New repository secret`.

Остальные секреты создаются автоматически на сервере при первом deploy:

- пароль PostgreSQL
- ключ подписи JWT
- GitLab API token для LabServer

Они сохраняются в файле:

```bash
/home/alexey/trspo-3lab/shared/deploy.env
```

Этот файл не коммитится в git и остается на сервере между развертываниями.

## Почта

Для проверки писем используется отдельный контейнер `mailpit`.

LabServer отправляет письма через SMTP-контейнер `mailpit`, а посмотреть отправленные письма можно в браузере:

```text
http://zenrot.root.sx:18083
```

Это полноценная проверка того, что приложение формирует и отправляет письма: письмо проходит через SMTP и появляется в почтовом веб-интерфейсе. При этом Mailpit не отправляет письма наружу на Gmail, Yandex, Mail.ru и другие реальные ящики. Для учебного стенда это удобнее и надежнее, потому что не нужны SPF, DKIM, MX-записи, репутация домена и внешний SMTP-провайдер.

Если позже понадобится настоящая отправка во внешние почтовые ящики, нужно будет заменить Mailpit на реальный SMTP-сервер или внешний SMTP-провайдер и отдельно настроить DNS почты.

## Требования к серверу

На сервере должны быть установлены Docker и Docker Compose:

```bash
docker --version
docker compose version
```

Пользователь `alexey` должен запускать Docker без `sudo`.

Проверка:

```bash
ssh alexey@188.243.90.29
docker ps
```

Если `docker ps` требует `sudo`, нужно добавить пользователя `alexey` в группу `docker`.

## Как запустить развертывание

1. Добавить GitHub secret `DEPLOY_PASSWORD`.
2. Закоммитить файлы проекта, включая `.github/workflows/deploy.yml` и каталог `labserveroriginal`.
3. Выполнить push в `main`.
4. Открыть вкладку `Actions` в GitHub.
5. Дождаться успешного выполнения workflow `Deploy LabServer`.

Также развертывание можно запустить вручную через `Actions` -> `Deploy LabServer` -> `Run workflow`.

## Что делает workflow

Workflow выполняет такие шаги:

1. Устанавливает `sshpass` на GitHub Actions runner.
2. Подключается к серверу по паролю из `DEPLOY_PASSWORD`.
3. Упаковывает репозиторий в архив.
4. Загружает архив на сервер.
5. Распаковывает новый релиз в `/home/alexey/trspo-3lab/releases/<commit-sha>`.
6. Обновляет ссылку `/home/alexey/trspo-3lab/current`.
7. Запускает серверный скрипт `labserveroriginal/scripts/deploy-production.sh`.
8. Скрипт создает недостающие секреты, поднимает PostgreSQL, GitLab, Mailpit, собирает фронтенд и запускает весь стек.

## Где лежат данные

По умолчанию используются такие пути:

- постоянные данные: `/home/alexey/trspo-3lab/data`
- автосгенерированные секреты: `/home/alexey/trspo-3lab/shared/deploy.env`
- релизы: `/home/alexey/trspo-3lab/releases`
- текущая версия: `/home/alexey/trspo-3lab/current`

## Как проверить контейнеры

На сервере выполнить:

```bash
ssh alexey@188.243.90.29
cd /home/alexey/trspo-3lab/current/labserveroriginal
docker compose -p trspo-lab -f compose.deploy.yml ps
```

Ожидаемый результат: контейнеры `labserver`, `plagiarism-server`, `webserver`, `db`, `gitlab` и `mailpit` должны быть в состоянии `Up`.

Логи серверной части:

```bash
docker compose -p trspo-lab -f compose.deploy.yml logs --tail=100 labserver
```

Логи nginx:

```bash
docker compose -p trspo-lab -f compose.deploy.yml logs --tail=100 webserver
```

Логи GitLab:

```bash
docker compose -p trspo-lab -f compose.deploy.yml logs --tail=100 gitlab
```

Логи почтового контейнера:

```bash
docker compose -p trspo-lab -f compose.deploy.yml logs --tail=100 mailpit
```

## Как проверить веб-интерфейс

Открыть в браузере:

- `http://zenrot.root.sx:18080` - админ-панель
- `http://zenrot.root.sx:18081` - кабинет студента
- `http://zenrot.root.sx:18082` - GitLab
- `http://zenrot.root.sx:18083` - просмотр отправленных писем

Проверка админ-панели:

1. Открыть `http://zenrot.root.sx:18080/register`.
2. Зарегистрировать первого пользователя.
3. Первый пользователь должен автоматически получить роль администратора.
4. Войти в админ-панель.
5. Проверить, что доступны страницы пользователей, групп, курсов, лабораторных и тестов.

## Как проверить GitLab

1. Открыть `http://zenrot.root.sx:18082`.
2. Дождаться полной инициализации GitLab. Первый запуск может занимать несколько минут.
3. Проверить, что GitLab открывается в браузере.
4. Проверить SSH-порт GitLab:

```bash
ssh -T -p 18022 git@zenrot.root.sx
```

GitLab может ответить отказом авторизации, если SSH-ключ не добавлен в GitLab. Это нормально: важно, что соединение идет именно на порт `18022`, а не на системный SSH-порт `22`.

LabServer получает GitLab API token автоматически. Скрипт deploy создает token внутри GitLab через `gitlab-rails runner` и записывает тот же token в `Server/appsettings.docker.json`.

## Как проверить отправку писем

Открыть веб-интерфейс Mailpit:

```text
http://zenrot.root.sx:18083
```

Полная проверка письма через приложение:

1. Открыть админ-панель: `http://zenrot.root.sx:18080`.
2. Войти под администратором.
3. Создать группу.
4. Добавить студента с любым email-адресом, например `student@example.com`.
5. Синхронизировать группу или студента с GitLab.
6. Нажать действие отправки учетных данных студенту.
7. Открыть `http://zenrot.root.sx:18083`.
8. Найти письмо в Mailpit.

В письме должны быть:

- ссылка на кабинет студента вида `http://zenrot.root.sx:18081/mylabs/<token>`
- имя пользователя GitLab
- начальный пароль GitLab

После получения письма проверить полный путь студента:

1. Открыть ссылку `http://zenrot.root.sx:18081/mylabs/<token>` из письма.
2. Убедиться, что кабинет студента открывается.
3. Перейти по ссылке GitLab из кабинета.
4. Войти в GitLab под присланными логином и паролем.
5. Проверить, что студент видит свои проекты или задания.

Если письмо не появилось в Mailpit:

1. Проверить логи серверной части:

```bash
docker compose -p trspo-lab -f compose.deploy.yml logs --tail=200 labserver
```

2. Найти ошибку вида `an error occured while sending email`.
3. Проверить, что контейнер `mailpit` запущен:

```bash
docker compose -p trspo-lab -f compose.deploy.yml ps mailpit
```

4. Проверить рабочий конфиг приложения:

```bash
grep -A8 '"SMTP"' Server/appsettings.docker.json
```

Ожидаемые SMTP-настройки:

- `domain`: `mailpit`
- `port`: `1025`
- `ssl`: `false`

## Как проверить развертывание после нового коммита

1. Сделать небольшой коммит.
2. Выполнить push в `main`.
3. Дождаться зеленого статуса workflow `Deploy LabServer`.
4. На сервере проверить, что `/home/alexey/trspo-3lab/current` указывает на новый релиз:

```bash
readlink /home/alexey/trspo-3lab/current
```

5. Проверить контейнеры:

```bash
cd /home/alexey/trspo-3lab/current/labserveroriginal
docker compose -p trspo-lab -f compose.deploy.yml ps
```

6. Открыть админ-панель, кабинет студента, GitLab и Mailpit в браузере.
