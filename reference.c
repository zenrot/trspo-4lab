#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Перечисление всех возможных состояний нашего автомата
typedef enum {
    STATE_NORMAL,
    STATE_STRING,
    STATE_CHAR,
    STATE_SLASH,
    STATE_SINGLE_LINE_COMMENT,
    STATE_MULTI_LINE_COMMENT,
    STATE_MULTI_LINE_COMMENT_STAR
} State;

void process_file(const char* input_filename) {
    // Формируем имя выходного файла с расширением .wc
    char output_filename[64];
    strcpy(output_filename, input_filename);
    char* ext = strrchr(output_filename, '.');
    if (ext != NULL) {
        strcpy(ext, ".wc");
    } else {
        strcat(output_filename, ".wc");
    }

    FILE* fin = fopen(input_filename, "r");
    if (!fin) {
        printf("Ошибка: не удалось открыть исходный файл %s\n", input_filename);
        return;
    }

    FILE* fout = fopen(output_filename, "w");
    if (!fout) {
        printf("Ошибка: не удалось создать файл %s\n", output_filename);
        fclose(fin);
        return;
    }

    State state = STATE_NORMAL;
    int c;
    int escaped = 0; // Флаг для отслеживания символа '\'

    while ((c = fgetc(fin)) != EOF) {
        switch (state) {
            case STATE_NORMAL:
                if (c == '/') {
                    state = STATE_SLASH;
                } else if (c == '"') {
                    fputc(c, fout);
                    state = STATE_STRING;
                    escaped = 0;
                } else if (c == '\'') {
                    fputc(c, fout);
                    state = STATE_CHAR;
                    escaped = 0;
                } else {
                    fputc(c, fout);
                }
                break;

            case STATE_SLASH:
                if (c == '/') {
                    state = STATE_SINGLE_LINE_COMMENT;
                    escaped = 0;
                } else if (c == '*') {
                    state = STATE_MULTI_LINE_COMMENT;
                } else {
                    // Оказалось, что это был просто знак деления
                    fputc('/', fout);
                    if (c == '"') {
                        fputc(c, fout);
                        state = STATE_STRING;
                        escaped = 0;
                    } else if (c == '\'') {
                        fputc(c, fout);
                        state = STATE_CHAR;
                        escaped = 0;
                    } else {
                        fputc(c, fout);
                        state = STATE_NORMAL;
                    }
                }
                break;

            case STATE_SINGLE_LINE_COMMENT:
                if (c == '\\') {
                    escaped = 1;
                } else if (c == '\n') {
                    if (escaped) {
                        // Если был '\', комментарий продолжается на новой строке
                        escaped = 0; 
                    } else {
                        // Конец однострочного комментария
                        fputc('\n', fout);
                        state = STATE_NORMAL;
                    }
                } else if (c != '\r') {
                    escaped = 0; // Сбрасываем экранирование, если это был не перенос строки
                }
                break;

            case STATE_MULTI_LINE_COMMENT:
                if (c == '*') {
                    state = STATE_MULTI_LINE_COMMENT_STAR;
                }
                break;

            case STATE_MULTI_LINE_COMMENT_STAR:
                if (c == '/') {
                    state = STATE_NORMAL; // Конец многострочного комментария
                } else if (c != '*') {
                    state = STATE_MULTI_LINE_COMMENT; // Ложная тревога
                }
                break;

            case STATE_STRING:
                fputc(c, fout);
                if (escaped) {
                    escaped = 0;
                } else {
                    if (c == '\\') {
                        escaped = 1;
                    } else if (c == '"') {
                        state = STATE_NORMAL; // Вышли из строки
                    }
                }
                break;

            case STATE_CHAR:
                fputc(c, fout);
                if (escaped) {
                    escaped = 0;
                } else {
                    if (c == '\\') {
                        escaped = 1;
                    } else if (c == '\'') {
                        state = STATE_NORMAL; // Вышли из символа
                    }
                }
                break;
        }
    }

    // Заглушка на случай, если файл оборвался сразу после слеша
    if (state == STATE_SLASH) {
        fputc('/', fout);
    }

    fclose(fin);
    fclose(fout);
    printf("Файл успешно обработан: %s -> %s\n", input_filename, output_filename);
}

int main() {
    FILE* project = fopen("project.txt", "r");
    if (!project) {
        printf("Ошибка: не удалось найти файл project.txt\n");
        return 1;
    }

    int N;
    if (fscanf(project, "%d", &N) != 1) {
        printf("Ошибка: неверный формат числа в project.txt\n");
        fclose(project);
        return 1;
    }

    // Проглатываем остатки первой строки после числа N
    int ch;
    while ((ch = fgetc(project)) != '\n' && ch != EOF);

    char filename[64];
    for (int i = 0; i < N; ++i) {
        if (fgets(filename, sizeof(filename), project)) {
            // Очищаем имя файла от символов переноса строки
            filename[strcspn(filename, "\r\n")] = '\0';
            if (strlen(filename) > 0) {
                process_file(filename);
            }
        }
    }

    fclose(project);
    return 0;
}